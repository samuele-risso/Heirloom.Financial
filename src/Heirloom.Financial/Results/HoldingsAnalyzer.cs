using Heirloom.Financial.Domain;
using Heirloom.Financial.Exchange;
using Heirloom.Financial.Internal;
using Heirloom.Financial.Positions;
using Heirloom.Financial.Validation;

namespace Heirloom.Financial.Results;

/// <summary>
/// Produce il <see cref="HoldingsSummary"/>: cosa si possiede e quanto è costato, senza prezzi di mercato.
/// </summary>
/// <remarks>
/// I lotti aperti vengono dal <see cref="CostBasisCalculator"/> con il metodo richiesto, quindi pesi e costi
/// sono gli stessi di <see cref="WalletAnalyzer"/> con le stesse operazioni.
/// </remarks>
public static class HoldingsAnalyzer
{
    /// <summary>
    /// Riassume le operazioni.
    /// </summary>
    /// <param name="lots">Le operazioni, in qualsiasi ordine e valuta.</param>
    /// <param name="method">Il metodo del costo di carico; i lotti aperti esistono solo con FIFO e lotto specifico.</param>
    /// <param name="currency">La valuta degli importi.</param>
    /// <param name="asOf">Il giorno del riepilogo: nessuna operazione può essere successiva.</param>
    /// <param name="rates">I cambi; servono se ci sono operazioni in un'altra valuta.</param>
    /// <returns>Il riepilogo.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="lots"/> è null.</exception>
    /// <exception cref="ArgumentException">
    /// Lotti non validi (vendite oltre il posseduto, operazioni in altra valuta senza cambi), o metodo a media ponderata.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">Un'operazione è successiva ad <paramref name="asOf"/>, o un valore non è definito.</exception>
    public static HoldingsSummary Summarize(
        IEnumerable<Lot> lots,
        CostBasisMethod method,
        Currency currency,
        DateOnly asOf,
        FxRateSeries? rates = null)
    {
        ArgumentNullException.ThrowIfNull(lots);
        DomainGuards.Defined(method);
        DomainGuards.Defined(currency);

        if (method == CostBasisMethod.WeightedAverage)
        {
            throw new ArgumentException(
                "Il riepilogo dei lotti richiede FIFO o lotto specifico: con la media ponderata i lotti si fondono.",
                nameof(method));
        }

        var lotList = WalletAnalyzer.ConvertLots(lots, currency, asOf, rates);
        var openLots = CostBasisCalculator.Calculate(lotList, method, currency).OpenLots;

        var byMetal = lotList
            .Select(l => l.Product.Metal)
            .Distinct()
            .Order()
            .Select(metal => SummarizeMetal(
                metal,
                openLots.Where(l => l.Product.Metal == metal).ToList(),
                lotList.Where(l => l.Product.Metal == metal).ToList(),
                currency))
            .ToList();

        return new HoldingsSummary(
            currency.ToIsoCode(),
            asOf,
            openLots.Count,
            openLots.Select(l => l.Product).Distinct().Count(),
            openLots.Sum(l => l.Quantity),
            UnknownQuantity(openLots),
            Rounding.Grams(Gross(openLots)).Grams,
            Rounding.Grams(Fine(openLots)).Grams,
            MoneyResult.From(Cost(openLots, currency)),
            AverageHoldingDays(openLots, asOf),
            byMetal,
            WalletAnalyzer.CashFlow(lotList, currency));
    }

    private static HoldingsMetal SummarizeMetal(Metal metal, IReadOnlyList<OpenLot> open, IReadOnlyList<Lot> lots, Currency currency)
    {
        var cost = Cost(open, currency);
        var knownFine = open.Where(l => l.HasKnownCost).Aggregate(Weight.Zero, (sum, l) => sum + l.FineWeight);

        return new HoldingsMetal(
            metal,
            open.Count,
            open.Sum(l => l.Quantity),
            UnknownQuantity(open),
            Rounding.Grams(Gross(open)).Grams,
            Rounding.Grams(Fine(open)).Grams,
            MoneyResult.From(cost),
            knownFine.Grams > 0m ? Rounding.UnitPrice(cost.Amount / knownFine.Grams) : null,
            WalletAnalyzer.CashFlow(lots, currency));
    }

    private static decimal UnknownQuantity(IEnumerable<OpenLot> lots) => lots.Where(l => !l.HasKnownCost).Sum(l => l.Quantity);

    private static Weight Gross(IEnumerable<OpenLot> lots) => lots.Aggregate(Weight.Zero, (sum, l) => sum + l.GrossWeight);

    private static Weight Fine(IEnumerable<OpenLot> lots) => lots.Aggregate(Weight.Zero, (sum, l) => sum + l.FineWeight);

    private static Money Cost(IEnumerable<OpenLot> lots, Currency currency) =>
        lots.Aggregate(Money.Zero(currency), (sum, l) => l.Cost is { } c ? sum + c : sum);

    private static decimal? AverageHoldingDays(IReadOnlyList<OpenLot> lots, DateOnly asOf)
    {
        var weight = Gross(lots).Grams;
        if (weight <= 0m)
        {
            return null;
        }

        var weightedDays = lots.Sum(l => (asOf.DayNumber - l.Purchase.Date.DayNumber) * l.GrossWeight.Grams);
        return Math.Round(weightedDays / weight, 2, MidpointRounding.AwayFromZero);
    }
}
