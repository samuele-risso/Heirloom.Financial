using Heirloom.Financial.Allocation;
using Heirloom.Financial.Domain;
using Heirloom.Financial.Exchange;
using Heirloom.Financial.Internal;
using Heirloom.Financial.Performance;
using Heirloom.Financial.Positions;
using Heirloom.Financial.Risk;
using Heirloom.Financial.Taxation;
using Heirloom.Financial.Valuation;
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

        if (options.TaxPolicy is not null && options.CostBasisMethod == CostBasisMethod.WeightedAverage)
        {
            throw new ArgumentException(
                "La stima delle tasse richiede FIFO o lotto specifico: con la media ponderata gli acquisti si fondono " +
                "e non si sa quali pezzi hanno la fattura.",
                nameof(options));
        }

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

        var spotByMetal = todaySpots.ToDictionary(s => s.Metal);
        var openLots = ValueOpenLots(costBasis.OpenLots, spotByMetal, pnl.TotalValue, options);
        var tax = Taxes(openLots, currency, options.TaxPolicy);

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
            tax is null ? null : MoneyResult.From(tax.Value.Total),
            tax is null ? null : MoneyResult.From(pnl.TotalValue - tax.Value.Total),
            tax is null ? null : MoneyResult.From(tax.Value.KnownCostPnLAfterTax),
            FineGrams(costBasis, Metal.Gold),
            FineGrams(costBasis, Metal.Silver),
            pnl.Positions.Select(PositionValuation.From).ToList(),
            openLots.Select(l => l.Result).ToList(),
            pnl.Positions.GroupBy(p => p.Position.Product.Metal).OrderBy(g => g.Key)
                .Select(g => SummarizeMetal(g.Key, g.ToList(), spotByMetal[g.Key])).ToList(),
            CashFlow(lotList, currency),
            AllocationShare.From(AllocationCalculator.Calculate(pnl, AllocationGrouping.Metal, AllocationBasis.Value)),
            AllocationShare.From(AllocationCalculator.Calculate(pnl, AllocationGrouping.Form, AllocationBasis.Value)),
            performance,
            risk,
            notes);
    }

    /// <summary>Un lotto aperto valutato: i valori esatti per i totali e il risultato arrotondato per l'API.</summary>
    private sealed record ValuedLot(OpenLot Lot, Money Value, Money? Tax, LotValuation Result);

    private static List<ValuedLot> ValueOpenLots(
        IReadOnlyList<OpenLot> openLots,
        IReadOnlyDictionary<Metal, SpotPrice> spots,
        Money totalValue,
        WalletAnalysisOptions options)
    {
        return openLots.Select(open =>
        {
            var value = options.Valuation.Value(open.Product, open.Quantity, spots[open.Product.Metal]);
            var pnl = open.Cost is { } cost ? value - cost : (Money?)null;
            var tax = options.TaxPolicy?.EstimateTax(value, open.Cost, open.HasDocumentedCost);

            var result = new LotValuation(
                open.Purchase.Id,
                open.Product.Name,
                open.Product.Metal,
                open.Product.Form,
                open.Purchase.Date,
                open.Quantity,
                Rounding.Grams(open.FineWeight).Grams,
                MoneyResult.From(value),
                totalValue.IsZero ? null : Rounding.Percentage(value / totalValue),
                MoneyResult.FromOptional(open.Cost),
                MoneyResult.FromOptional(pnl),
                open.Cost is { IsZero: false } c && pnl is { } gain ? Rounding.Percentage(gain / c) : null,
                open.HasDocumentedCost,
                MoneyResult.FromOptional(tax))
            {
                GrossWeightGrams = Rounding.Grams(open.GrossWeight).Grams,
                ValueAfterTax = tax is { } t ? MoneyResult.From(value - t) : null,
                UnrealizedPnLAfterTax = tax is { } t2 && pnl is { } lotGain ? MoneyResult.From(lotGain - t2) : null,
            };

            return new ValuedLot(open, value, tax, result);
        }).ToList();
    }

    private static (Money Total, Money KnownCostPnLAfterTax)? Taxes(IReadOnlyList<ValuedLot> lots, Currency currency, ITaxPolicy? policy)
    {
        if (policy is null)
        {
            return null;
        }

        var total = Money.Zero(currency);
        var knownAfterTax = Money.Zero(currency);

        foreach (var lot in lots)
        {
            total += lot.Tax!.Value;

            if (lot.Lot.Cost is { } cost)
            {
                knownAfterTax += lot.Value - lot.Tax.Value - cost;
            }
        }

        return (total, knownAfterTax);
    }

    private static MetalSummary SummarizeMetal(Metal metal, IReadOnlyList<PositionPnL> positions, SpotPrice spot)
    {
        var gross = positions.Aggregate(Weight.Zero, (sum, p) => sum + p.Position.Product.GrossWeight * p.Position.Quantity);
        var fine = positions.Aggregate(Weight.Zero, (sum, p) => sum + p.Position.FineWeight);
        var coinGross = positions
            .Where(p => p.Position.Product.Form == ProductForm.Coin)
            .Aggregate(Weight.Zero, (sum, p) => sum + p.Position.Product.GrossWeight * p.Position.Quantity);

        var value = positions.Aggregate(Money.Zero(spot.Currency), (sum, p) => sum + p.Value);
        var cost = positions.Aggregate(Money.Zero(spot.Currency), (sum, p) => sum + p.Position.CostBasis);

        // Il costo medio usa solo i pezzi di cui si conosce il costo, così un'eredità non lo abbassa.
        var knownFine = positions.Aggregate(Weight.Zero, (sum, p) => sum + p.Position.Product.FineWeight * p.Position.KnownCostQuantity);

        var coinShare = gross.Grams > 0m ? Rounding.Percentage(coinGross.Grams / gross.Grams) : 0m;

        return new MetalSummary(
            metal,
            Rounding.Grams(gross).Grams,
            Rounding.Grams(fine).Grams,
            Math.Round(fine.TroyOunces, Rounding.WeightDecimals, MidpointRounding.AwayFromZero),
            MoneyResult.From(value),
            MoneyResult.From(cost),
            knownFine.Grams > 0m ? Rounding.UnitPrice(cost.Amount / knownFine.Grams) : null,
            Rounding.UnitPrice(spot.PricePerGram.Amount),
            coinShare,
            gross.Grams > 0m ? 1m - coinShare : 0m)
        {
            SpotVsAverageCost = knownFine.Grams > 0m && cost.Amount > 0m
                ? Rounding.Percentage((spot.PricePerGram.Amount - cost.Amount / knownFine.Grams) / (cost.Amount / knownFine.Grams))
                : null,
        };
    }

    internal static CashFlowSummary CashFlow(IReadOnlyList<Lot> lots, Currency currency)
    {
        var buys = lots.Where(l => l.Side == LotSide.Buy).ToList();
        var sells = lots.Where(l => l.Side == LotSide.Sell).ToList();

        var purchases = buys.Aggregate(Money.Zero(currency), (sum, l) => sum + l.NetAmount);
        var sales = sells.Aggregate(Money.Zero(currency), (sum, l) => sum + l.NetAmount);

        return new CashFlowSummary(
            MoneyResult.From(purchases),
            buys.Count,
            MoneyResult.From(sales),
            sells.Count,
            MoneyResult.From(sales - purchases));
    }

    internal static List<Lot> ConvertLots(IEnumerable<Lot> lots, Currency currency, DateOnly date, FxRateSeries? rates)
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

        var history = options.IncludeHistory ? History(snapshots, lots, new SpotLookup(spots), options, rates) : [];

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

    private static List<HistoryPoint> History(
        IReadOnlyList<WalletSnapshot> snapshots,
        IReadOnlyList<Lot> lots,
        SpotLookup lookup,
        WalletAnalysisOptions options,
        FxRateSeries? rates)
    {
        var index = TimeWeightedReturn.Index(snapshots).ToDictionary(p => p.Date, p => p.Value);
        var ordered = lots.OrderBy(l => l.Date).ToList();
        var points = new List<HistoryPoint>(snapshots.Count);

        // Il costo di carico cambia solo nei giorni con un'operazione: si ricalcola solo allora.
        CostBasisResult? costBasis = null;
        var included = -1;
        var next = 0;

        foreach (var snapshot in snapshots)
        {
            while (next < ordered.Count && ordered[next].Date <= snapshot.Date)
            {
                next++;
            }

            if (next != included)
            {
                costBasis = CostBasisCalculator.Calculate(ordered.Take(next), options.CostBasisMethod, options.Currency);
                included = next;
            }

            var spots = costBasis!.Positions
                .Select(p => p.Product.Metal)
                .Distinct()
                .Select(metal => SpotIn(lookup.On(metal, snapshot.Date), options.Currency, snapshot.Date, rates))
                .ToList();
            var pnl = PnLCalculator.Calculate(costBasis, spots, options.Valuation);

            var metals = pnl.Positions
                .GroupBy(p => p.Position.Product.Metal)
                .OrderBy(g => g.Key)
                .Select(g => new MetalHistoryValue(
                    g.Key,
                    Rounding.Amount(g.Aggregate(Money.Zero(options.Currency), (sum, p) => sum + p.Value).Amount),
                    Rounding.Amount(g.Aggregate(Money.Zero(options.Currency), (sum, p) => sum + p.Position.CostBasis).Amount),
                    Rounding.UnitPrice(spots.First(s => s.Metal == g.Key).PricePerGram.Amount)))
                .ToList();

            points.Add(new HistoryPoint(
                snapshot.Date,
                Rounding.Amount(snapshot.Value.Amount),
                Rounding.Amount(snapshot.InvestedCapital.Amount),
                index.TryGetValue(snapshot.Date, out var value) ? Rounding.Percentage(value) : 1m,
                Rounding.Amount(pnl.TotalCostBasis.Amount),
                Rounding.Amount(pnl.UnrealizedPnL.Amount),
                PositionValuation.Percent(pnl.UnrealizedPercent),
                metals));
        }

        return points;
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