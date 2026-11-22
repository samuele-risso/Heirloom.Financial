using Heirloom.Financial.Domain;

namespace Heirloom.Financial.Valuation;

/// <summary>
/// Valuta ogni prodotto al prezzo a cui un commerciante lo ricomprerebbe:
/// valore = valore di fusione × (1 − sconto).
/// </summary>
/// <remarks>
/// È il valore più onesto per l'utente: quello che incasserebbe vendendo oggi.
/// Esempio di configurazione: monete −1%, lingotti −2%.
/// </remarks>
public sealed class BuybackValuation : IValuationStrategy
{
    private readonly ValuationRates _discounts;

    /// <summary>
    /// Crea la strategia.
    /// </summary>
    /// <param name="discountByForm">Lo sconto (come frazione, es. 0,02) per ogni forma; tra 0 incluso e 1 escluso.</param>
    /// <param name="discountByProduct">Sconti specifici per singoli prodotti; facoltativi, vincono sulla forma.</param>
    /// <exception cref="ArgumentOutOfRangeException">Uno sconto è negativo o non inferiore a 1.</exception>
    public BuybackValuation(
        IReadOnlyDictionary<ProductForm, decimal> discountByForm,
        IReadOnlyDictionary<MetalProduct, decimal>? discountByProduct = null)
    {
        _discounts = new ValuationRates(discountByForm, discountByProduct, maximum: 1m);
    }

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">Nessuno sconto configurato per il prodotto o la sua forma.</exception>
    public Money UnitValue(MetalProduct product, SpotPrice spot)
        => MeltValueCalculator.MeltValue(product, spot) * (1m - _discounts.For(product));
}