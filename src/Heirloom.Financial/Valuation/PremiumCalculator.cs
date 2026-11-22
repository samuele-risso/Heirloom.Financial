using Heirloom.Financial.Domain;
using Heirloom.Financial.Validation;

namespace Heirloom.Financial.Valuation;

/// <summary>
/// Misura il premio: quanto si è pagato sopra il valore del metallo contenuto.
/// </summary>
/// <remarks>
/// Le monete hanno di solito premi più alti dei lingotti, e i pezzi piccoli più alti dei grandi.
/// Confrontare i premi aiuta l'utente a capire se ha comprato bene. Il premio può essere
/// negativo (acquisto sotto spot): non è un errore.
/// </remarks>
public static class PremiumCalculator
{
    /// <summary>
    /// Il premio in percentuale, come frazione: (prezzo − valore di fusione) / valore di fusione.
    /// Per esempio 2.100 pagati per 2.000 di metallo → 0,05 (5%).
    /// </summary>
    /// <param name="price">Il prezzo pagato.</param>
    /// <param name="meltValue">Il valore di fusione, nella stessa valuta.</param>
    /// <returns>Il premio come frazione; negativo se si è pagato meno del metallo.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="meltValue"/> non è maggiore di zero.</exception>
    /// <exception cref="ArgumentException">Prezzo e valore di fusione sono in valute diverse.</exception>
    public static decimal PremiumPercent(Money price, Money meltValue)
    {
        DomainGuards.Positive(meltValue);
        DomainGuards.SameCurrency(meltValue, price);

        return (price - meltValue) / meltValue;
    }

    /// <summary>
    /// Il premio per oncia troy di metallo puro: (prezzo − valore di fusione) / once fine.
    /// Utile per confrontare prodotti di peso diverso.
    /// </summary>
    /// <param name="unitPrice">Il prezzo di un pezzo.</param>
    /// <param name="product">Il prodotto.</param>
    /// <param name="spot">Il prezzo spot, nella stessa valuta del prezzo.</param>
    /// <returns>Il premio per oncia fine, nella valuta del prezzo.</returns>
    /// <exception cref="ArgumentException">Prezzo e spot in valute diverse, o spot di un altro metallo.</exception>
    public static Money PremiumPerTroyOunce(Money unitPrice, MetalProduct product, SpotPrice spot)
    {
        var melt = MeltValueCalculator.MeltValue(product, spot);
        DomainGuards.SameCurrency(melt, unitPrice);

        return (unitPrice - melt) / product.FineWeight.TroyOunces;
    }

    /// <summary>
    /// Il premio effettivo pagato su un acquisto, costi accessori inclusi:
    /// il costo di ogni pezzo è (prezzo × quantità + costi) / quantità.
    /// </summary>
    /// <param name="lot">L'acquisto.</param>
    /// <param name="spotAtPurchase">Lo spot del giorno dell'acquisto, nella valuta del lotto.</param>
    /// <returns>Il premio effettivo come frazione.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="lot"/> è null.</exception>
    /// <exception cref="ArgumentException">Il lotto è una vendita, o spot e lotto sono in valute diverse.</exception>
    public static decimal EffectivePremiumPercent(Lot lot, SpotPrice spotAtPurchase)
    {
        ArgumentNullException.ThrowIfNull(lot);

        if (lot.Side != LotSide.Buy)
        {
            throw new ArgumentException("Il premio effettivo si calcola solo sugli acquisti.", nameof(lot));
        }

        var unitCost = lot.NetAmount / lot.Quantity;
        var melt = MeltValueCalculator.MeltValue(lot.Product, spotAtPurchase);

        return PremiumPercent(unitCost, melt);
    }
}