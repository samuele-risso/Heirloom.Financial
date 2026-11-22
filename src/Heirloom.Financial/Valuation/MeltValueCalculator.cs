using Heirloom.Financial.Domain;
using Heirloom.Financial.Exchange;

namespace Heirloom.Financial.Valuation;

/// <summary>
/// Calcola il valore di fusione: quanto vale il metallo puro contenuto, al prezzo spot.
/// </summary>
/// <remarks>
/// <para>
/// Valore di fusione = peso fino in once troy × prezzo spot per oncia. È il valore più oggettivo
/// e prudente: non tiene conto di premi di conio, rarità o prezzi di riacquisto.
/// </para>
/// <para>
/// Il risultato è nella valuta dello spot (di solito USD). Per averlo in un'altra valuta si usa
/// l'overload con il cambio, che passa da <see cref="CurrencyConverter"/>.
/// </para>
/// </remarks>
public static class MeltValueCalculator
{
    /// <summary>
    /// Il valore di fusione di un singolo pezzo.
    /// </summary>
    /// <param name="product">Il prodotto.</param>
    /// <param name="spot">Il prezzo spot dello stesso metallo del prodotto.</param>
    /// <returns>Il valore del metallo contenuto, nella valuta dello spot.</returns>
    /// <exception cref="ArgumentNullException">Un argomento è null.</exception>
    /// <exception cref="ArgumentException">Lo spot è di un metallo diverso da quello del prodotto.</exception>
    public static Money MeltValue(MetalProduct product, SpotPrice spot)
    {
        ArgumentNullException.ThrowIfNull(product);
        ArgumentNullException.ThrowIfNull(spot);

        if (spot.Metal != product.Metal)
        {
            throw new ArgumentException(
                $"Lo spot è di {spot.Metal}, ma '{product.Name}' è di {product.Metal}.", nameof(spot));
        }

        return spot.PricePerTroyOunce * product.FineWeight.TroyOunces;
    }

    /// <summary>
    /// Il valore di fusione di più pezzi dello stesso prodotto.
    /// </summary>
    /// <param name="product">Il prodotto.</param>
    /// <param name="quantity">Il numero di pezzi; non negativo.</param>
    /// <param name="spot">Il prezzo spot dello stesso metallo del prodotto.</param>
    /// <returns>Il valore del metallo contenuto, nella valuta dello spot.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="quantity"/> è negativa.</exception>
    public static Money MeltValue(MetalProduct product, decimal quantity, SpotPrice spot)
    {
        if (quantity < 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), quantity, "La quantità non può essere negativa.");
        }

        return MeltValue(product, spot) * quantity;
    }

    /// <summary>
    /// Il valore di fusione di un singolo pezzo, convertito in un'altra valuta.
    /// </summary>
    /// <param name="product">Il prodotto.</param>
    /// <param name="spot">Il prezzo spot dello stesso metallo del prodotto.</param>
    /// <param name="target">La valuta in cui si vuole il risultato.</param>
    /// <param name="rate">Il cambio da usare per la conversione.</param>
    /// <returns>Il valore del metallo contenuto, nella valuta richiesta.</returns>
    public static Money MeltValue(MetalProduct product, SpotPrice spot, Currency target, FxRate rate)
        => CurrencyConverter.Convert(MeltValue(product, spot), target, rate);

    /// <summary>
    /// Il valore di fusione del metallo ancora posseduto, dato l'elenco delle operazioni:
    /// gli acquisti aggiungono pezzi, le vendite li tolgono.
    /// </summary>
    /// <param name="lots">Le operazioni, anche di metalli diversi.</param>
    /// <param name="spots">Un prezzo spot per ogni metallo presente nei lotti, tutti nella stessa valuta.</param>
    /// <returns>Il valore totale, nella valuta degli spot. Zero se non si possiede nulla.</returns>
    /// <exception cref="ArgumentNullException">Un argomento è null.</exception>
    /// <exception cref="ArgumentException">Spot mancanti, ripetuti per lo stesso metallo o in valute diverse.</exception>
    public static Money MeltValue(IEnumerable<Lot> lots, IEnumerable<SpotPrice> spots)
    {
        ArgumentNullException.ThrowIfNull(lots);
        ArgumentNullException.ThrowIfNull(spots);

        var spotList = spots.ToList();

        if (spotList.Count == 0)
        {
            throw new ArgumentException("Serve almeno un prezzo spot.", nameof(spots));
        }

        var currency = spotList[0].Currency;

        if (spotList.Any(s => s.Currency != currency))
        {
            throw new ArgumentException("Tutti gli spot devono essere nella stessa valuta.", nameof(spots));
        }

        if (spotList.GroupBy(s => s.Metal).Any(g => g.Count() > 1))
        {
            throw new ArgumentException("C'è più di un prezzo spot per lo stesso metallo.", nameof(spots));
        }

        var byMetal = spotList.ToDictionary(s => s.Metal);
        var total = Money.Zero(currency);

        foreach (var lot in lots)
        {
            if (!byMetal.TryGetValue(lot.Product.Metal, out var spot))
            {
                throw new ArgumentException($"Manca il prezzo spot per {lot.Product.Metal}.", nameof(spots));
            }

            var value = MeltValue(lot.Product, lot.Quantity, spot);
            total = lot.Side == LotSide.Buy ? total + value : total - value;
        }

        return total;
    }
}