using Heirloom.Financial.Allocation;
using Heirloom.Financial.Domain;
using Heirloom.Financial.Exchange;
using Heirloom.Financial.Internal;
using Heirloom.Financial.Performance;
using Heirloom.Financial.Positions;
using Heirloom.Financial.Risk;
using Heirloom.Financial.Validation;

namespace Heirloom.Financial.Results;

/// <summary>
/// Il punto d'ingresso unico della libreria: dai lotti, dagli spot e dai cambi produce
/// il <see cref="WalletSummary"/> completo, già arrotondato e pronto per l'API.
/// </summary>
/// <remarks>
/// <para>Mette in fila i calcolatori della libreria, sempre nello stesso modo:</para>
/// <list type="number">
/// <item>converte i lotti nella valuta richiesta, ognuno con il cambio della sua data;</item>
/// <item>calcola costo di carico, guadagni e ripartizione con gli spot del giorno di valutazione;</item>
/// <item>ricostruisce lo storico giorno per giorno e ne ricava TWR, XIRR, volatilità, drawdown e Sharpe.</item>
/// </list>
/// <para>
/// Se mancano i prezzi per ricostruire lo storico, il riepilogo arriva lo stesso, senza rendimenti
/// e rischio, con il motivo in <see cref="WalletSummary.Notes"/>.
/// </para>
/// </remarks>
public static class WalletAnalyzer
{
    /// <summary>
    /// Analizza il portafoglio.
    /// </summary>
    /// <param name="lots">Le operazioni, in qualsiasi ordine e valuta.</param>
    /// <param name="spots">I prezzi spot storici (almeno fino alla data di valutazione), di oro e argento.</param>
    /// <param name="options">Le scelte di calcolo.</param>
    /// <param name="rates">I cambi; servono se lotti o spot sono in una valuta diversa da quella del riepilogo.</param>
    /// <returns>Il riepilogo completo.</returns>
    /// <exception cref="ArgumentNullException">Un argomento obbligatorio è null.</exception>
    /// <exception cref="ArgumentException">Lotti non validi (vendite oltre il posseduto, lotti nel futuro, lotti in altra valuta senza cambi).</exception>
    /// <exception cref="ArgumentOutOfRangeException">Un'opzione ha un valore non definito, o un lotto è successivo alla data di valutazione.</exception>
    /// <exception cref="InvalidOperationException">Manca lo spot di un metallo posseduto nel giorno di valutazione, o un cambio.</exception>
    public static WalletSummary Analyze(
        IEnumerable<Lot> lots,
        IEnumerable<SpotPrice> spots,
        WalletAnalysisOptions options,
        FxRateSeries? rates = null)
    {
        ArgumentNullException.ThrowIfNull(lots);
        ArgumentNullException.ThrowIfNull(spots);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(options.Valuation);
        DomainGuards.Defined(options.Currency);
        DomainGuards.Defined(options.CostBasisMethod);
        DomainGuards.Defined(options.AnnualizationBasis);

        var currency = options.Currency;
        var date = options.ValuationDate;
        var spotList = spots.ToList();
        var lookup = new SpotLookup(spotList);

        var lotList = ConvertLots(lots, currency, date, rates);

        // Posizioni e guadagni al giorno di valutazione.
        var costBasis = CostBasisCalculator.Calculate(lotList, options.CostBasisMethod, currency);
        var todaySpots = costBasis.Positions
            .Select(p => p.Product.Metal)
            .Distinct()
            .Select(metal => SpotIn(lookup.On(metal, date), currency, date, rates))
            .ToList();
        var pnl = PnLCalculator.Calculate(costBasis, todaySpots, options.Valuation);

        var notes = new List<string>();
        var (performance, risk) = BuildHistory(lotList, spotList, pnl, options, rates, notes);

        return new WalletSummary(
            currency.ToIsoCode(),
            date,
            options.CostBasisMethod,
            MoneyResult.From(pnl.TotalValue),
            MoneyResult.From(pnl.KnownCostValue),
            MoneyResult.From(pnl.UnknownCostValue),
            MoneyResult.From(pnl.TotalCostBasis),
            MoneyResult.From(pnl.UnrealizedPnL),
            PositionValuation.Percent(pnl.UnrealizedPercent),
            MoneyResult.From(pnl.RealizedPnL),
            MoneyResult.From(costBasis.UnknownCostProceeds),
            MoneyResult.From(pnl.TotalPnL),
            FineGrams(costBasis, Metal.Gold),
            FineGrams(costBasis, Metal.Silver),
            pnl.Positions.Select(PositionValuation.From).ToList(),
            AllocationShare.From(AllocationCalculator.Calculate(pnl, AllocationGrouping.Metal, AllocationBasis.Value)),
            AllocationShare.From(AllocationCalculator.Calculate(pnl, AllocationGrouping.Form, AllocationBasis.Value)),
            performance,
            risk,
            notes);
    }

    private static List<Lot> ConvertLots(IEnumerable<Lot> lots, Currency currency, DateOnly date, FxRateSeries? rates)
    {
        var list = lots.ToList();

        foreach (var lot in list)
        {
            DomainGuards.NotInFuture(lot.Date, date, nameof(lots));
        }

        if (list.FirstOrDefault(l => l.Currency != currency) is { } other)
        {
            if (rates is null)
            {
                throw new ArgumentException(
                    $"'{other.Product.Name}' del {other.Date:yyyy-MM-dd} è in {other.Currency.ToIsoCode()}: " +
                    $"servono i cambi verso {currency.ToIsoCode()}.",
                    nameof(lots));
            }

            return LotCurrencyConverter.Convert(list, currency, rates).ToList();
        }

        return list;
    }

    private static SpotPrice SpotIn(SpotPrice spot, Currency currency, DateOnly date, FxRateSeries? rates)
    {
        if (spot.Currency == currency)
        {
            return spot;
        }

        if (rates is null)
        {
            throw new InvalidOperationException(
                $"Lo spot di {spot.Metal} è in {spot.Currency.ToIsoCode()}: servono i cambi verso {currency.ToIsoCode()}.");
        }

        return new SpotPrice(spot.Metal, rates.Convert(spot.PricePerTroyOunce, currency, date), spot.Date);
    }

    private static decimal FineGrams(CostBasisResult costBasis, Metal metal) =>
        Rounding.Grams(costBasis.Positions
            .Where(p => p.Product.Metal == metal)
            .Aggregate(Weight.Zero, (sum, p) => sum + p.FineWeight)).Grams;

    private static (PerformanceSummary? Performance, RiskSummary? Risk) BuildHistory(
        IReadOnlyList<Lot> lots,
        IReadOnlyList<SpotPrice> spots,
        PnLSummary pnl,
        WalletAnalysisOptions options,
        FxRateSeries? rates,
        List<string> notes)
    {
        if (lots.Count == 0)
        {
            notes.Add("Nessuna operazione: rendimenti e rischio non disponibili.");
            return (null, null);
        }

        var from = options.HistoryFrom ?? lots.Min(l => l.Date);
        var to = options.ValuationDate;

        if (from > to)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options),
                from,
                $"L'inizio dello storico ({from:yyyy-MM-dd}) è successivo alla data di valutazione ({to:yyyy-MM-dd}).");
        }

        IReadOnlyList<WalletSnapshot> snapshots;
        IReadOnlyList<CashFlow> flows;

        try
        {
            snapshots = SnapshotBuilder.Build(lots, spots, options.Valuation, options.Currency, from, to, rates);
            flows = Xirr.CashFlowsFrom(lots, pnl.TotalValue, to, spots, options.Valuation, rates);
        }
        catch (InvalidOperationException ex)
        {
            notes.Add($"Rendimenti e rischio non disponibili: {ex.Message}");
            return (null, null);
        }

        var twr = TimeWeightedReturn.Calculate(snapshots);
        var longEnough = to.DayNumber - from.DayNumber >= 365;
        var annualized = twr is { } total && longEnough
            ? ReturnsCalculator.Annualized(total, from, to)
            : (decimal?)null;

        var xirr = Xirr.Calculate(flows);
        if (!xirr.Success)
        {
            notes.Add($"Rendimento personale (XIRR) non calcolabile: {xirr.FailureReason}");
        }

        var history = options.IncludeHistory ? History(snapshots) : [];

        var performance = new PerformanceSummary(
            from,
            to,
            options.Currency.ToIsoCode(),
            PositionValuation.Percent(twr),
            PositionValuation.Percent(annualized),
            xirr.Rate is { } rate && double.IsFinite(rate) && Math.Abs(rate) < 1e9 ? Rounding.Percentage((decimal)rate) : null,
            history);

        return (performance, Risk(snapshots, annualized, options));
    }

    private static List<HistoryPoint> History(IReadOnlyList<WalletSnapshot> snapshots)
    {
        var index = TimeWeightedReturn.Index(snapshots).ToDictionary(p => p.Date, p => p.Value);

        return snapshots
            .Select(s => new HistoryPoint(
                s.Date,
                Rounding.Amount(s.Value.Amount),
                Rounding.Amount(s.InvestedCapital.Amount),
                index.TryGetValue(s.Date, out var value) ? Rounding.Percentage(value) : 1m))
            .ToList();
    }

    private static RiskSummary? Risk(IReadOnlyList<WalletSnapshot> snapshots, decimal? annualizedReturn, WalletAnalysisOptions options)
    {
        var volatility = VolatilityCalculator.Annualized(snapshots, options.AnnualizationBasis);

        if (volatility is not { } vol)
        {
            return null;
        }

        var drawdown = DrawdownCalculator.Max(snapshots);
        var sharpe = annualizedReturn is { } r && options.RiskFreeRate is { } riskFree
            ? SharpeCalculator.Calculate((double)r, (double)riskFree, vol)
            : null;

        return new RiskSummary(
            options.AnnualizationBasis,
            Rounding.Percentage((decimal)vol),
            drawdown is null || drawdown.Depth == 0m
                ? null
                : new DrawdownResult(Rounding.Percentage(drawdown.Depth), drawdown.PeakDate, drawdown.TroughDate, drawdown.RecoveryDate),
            sharpe is { } s ? Rounding.Percentage((decimal)s) : null);
    }
}