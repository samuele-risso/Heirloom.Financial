namespace Heirloom.Financial.Domain;

/// <summary>
/// Il prezzo di mercato (spot) di un metallo in un giorno, per oncia troy.
/// </summary>
/// <remarks>
/// Lo spot è il prezzo del metallo grezzo all'ingrosso, quotato per oncia troy di metallo
/// puro e di solito in dollari. Per le valutazioni giornaliere si usa il prezzo di chiusura
/// del giorno. La data è obbligatoria: un prezzo senza data non si può usare nei calcoli storici.
/// </remarks>
public sealed record SpotPrice
{
    /// <summary>
    /// Il metallo quotato.
    /// </summary>
    public Metal Metal { get; }

    /// <summary>
    /// Il prezzo di un'oncia troy di metallo puro, con la sua valuta.
    /// </summary>
    public Money PricePerTroyOunce { get; }

    /// <summary>
    /// Il giorno a cui si riferisce il prezzo.
    /// </summary>
    public DateOnly Date { get; }

    /// <summary>
    /// La valuta del prezzo.
    /// </summary>
    public Currency Currency => PricePerTroyOunce.Currency;

    /// <summary>
    /// Il prezzo di un grammo di metallo puro.
    /// </summary>
    public Money PricePerGram => PricePerTroyOunce / Weight.GramsPerTroyOunce;

    /// <summary>
    /// Crea un prezzo spot.
    /// </summary>
    /// <param name="metal">Il metallo; deve essere un valore definito di <see cref="Domain.Metal"/>.</param>
    /// <param name="pricePerTroyOunce">Il prezzo per oncia troy; deve essere maggiore di zero.</param>
    /// <param name="date">Il giorno del prezzo.</param>
    /// <exception cref="ArgumentOutOfRangeException">Metallo non definito o prezzo non positivo.</exception>
    public SpotPrice(Metal metal, Money pricePerTroyOunce, DateOnly date)
    {
        if (!Enum.IsDefined(metal))
        {
            throw new ArgumentOutOfRangeException(nameof(metal), metal, "Metallo non supportato.");
        }

        if (pricePerTroyOunce.IsNegative || pricePerTroyOunce.IsZero)
        {
            throw new ArgumentOutOfRangeException(nameof(pricePerTroyOunce), pricePerTroyOunce, "Il prezzo spot deve essere maggiore di zero.");
        }

        Metal = metal;
        PricePerTroyOunce = pricePerTroyOunce;
        Date = date;
    }
}