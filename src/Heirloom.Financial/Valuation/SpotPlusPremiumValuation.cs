using Heirloom.Financial.Domain;

namespace Heirloom.Financial.Valuation;

/// <summary>
/// Valuta ogni prodotto al valore del metallo più un premio stimato:
/// valore = valore di fusione × (1 + premio).
/// </summary>
/// <remarks>
/// È più vicina al prezzo a cui il prodotto si compra sul mercato, soprattutto per le monete.
/// Esempio di configurazione: monete +4%, lingotti +1%, Sterlina +7%.
/// </remarks>
public sealed class SpotPlusPremiumValuation : IValuationStrategy
{
    private readonly ValuationRates _premiums;

    /// <summary>
    /// Crea la strategia.
    /// </summary>
    /// <param name="premiumByForm">Il premio (come frazione, es. 0,04) per ogni forma.</param>
    /// <param name="premiumByProduct">Premi specifici per singoli prodotti; facoltativi, vincono sulla forma.</param>
    /// <exception cref="ArgumentOutOfRangeException">Un premio è negativo.</exception>
    public SpotPlusPremiumValuation(
        IReadOnlyDictionary<ProductForm, decimal> premiumByForm,
        IReadOnlyDictionary<MetalProduct, decimal>? premiumByProduct = null)
    {
        _premiums = new ValuationRates(premiumByForm, premiumByProduct);
    }

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">Nessun premio configurato per il prodotto o la sua forma.</exception>
    public Money UnitValue(MetalProduct product, SpotPrice spot)
        => MeltValueCalculator.MeltValue(product, spot) * (1m + _premiums.For(product));
}