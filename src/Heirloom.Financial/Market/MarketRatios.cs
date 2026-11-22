using Heirloom.Financial.Domain;

namespace Heirloom.Financial.Market;

/// <summary>
/// Indicatori sul mercato, indipendenti dal portafoglio dell'utente.
/// </summary>
public static class MarketRatios
{
    /// <summary>
    /// Il rapporto oro/argento: quante once d'argento servono per comprare un'oncia d'oro.
    /// Molti investitori lo seguono per decidere quale dei due metalli comprare.
    /// </summary>
    /// <param name="gold">Lo spot dell'oro.</param>
    /// <param name="silver">Lo spot dell'argento, nella stessa valuta.</param>
    /// <returns>Il rapporto (es. 2.000 / 25 = 80).</returns>
    /// <exception cref="ArgumentNullException">Uno spot è null.</exception>
    /// <exception cref="ArgumentException">Spot dei metalli sbagliati o in valute diverse.</exception>
    public static decimal GoldSilverRatio(SpotPrice gold, SpotPrice silver)
    {
        ArgumentNullException.ThrowIfNull(gold);
        ArgumentNullException.ThrowIfNull(silver);

        if (gold.Metal != Metal.Gold)
        {
            throw new ArgumentException("Il primo spot deve essere dell'oro.", nameof(gold));
        }

        if (silver.Metal != Metal.Silver)
        {
            throw new ArgumentException("Il secondo spot deve essere dell'argento.", nameof(silver));
        }

        if (gold.Currency != silver.Currency)
        {
            throw new ArgumentException(
                $"I due spot devono essere nella stessa valuta: {gold.Currency.ToIsoCode()} e {silver.Currency.ToIsoCode()}.",
                nameof(silver));
        }

        return gold.PricePerTroyOunce / silver.PricePerTroyOunce;
    }

    /// <summary>
    /// La serie storica del rapporto oro/argento, con le due serie di prezzi allineate per data.
    /// </summary>
    /// <param name="gold">I prezzi dell'oro.</param>
    /// <param name="silver">I prezzi dell'argento, nella stessa valuta.</param>
    /// <returns>Il rapporto per ogni data in cui sono noti entrambi i prezzi.</returns>
    public static IReadOnlyList<DatedValue> GoldSilverRatioSeries(IEnumerable<DatedValue> gold, IEnumerable<DatedValue> silver)
        => PriceSeries.Align(gold, silver)
            .Where(v => v.Second != 0m)
            .Select(v => new DatedValue(v.Date, v.First / v.Second))
            .ToList();
}