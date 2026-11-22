using Heirloom.Financial.Domain;

namespace Heirloom.Financial.Valuation;

/// <summary>
/// Una percentuale configurata per forma del prodotto, con eventuali eccezioni per singolo prodotto.
/// Vince sempre la regola più specifica: prima il prodotto, poi la forma.
/// </summary>
/// <remarks>
/// Usata dalle strategie con premio e con sconto di riacquisto. La configurazione arriva da fuori
/// (per esempio dalle impostazioni di Heirloom): la libreria non contiene valori di mercato.
/// </remarks>
internal sealed class ValuationRates
{
    private readonly Dictionary<ProductForm, decimal> _byForm;
    private readonly Dictionary<MetalProduct, decimal> _byProduct;

    /// <summary>
    /// Crea la configurazione.
    /// </summary>
    /// <param name="byForm">La percentuale (come frazione, es. 0,04 = 4%) per ogni forma.</param>
    /// <param name="byProduct">Eccezioni per singoli prodotti; facoltative.</param>
    /// <param name="maximum">Il valore massimo ammesso (escluso), per esempio 1 per uno sconto.</param>
    /// <exception cref="ArgumentNullException"><paramref name="byForm"/> è null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Una percentuale è negativa o non inferiore al massimo.</exception>
    public ValuationRates(
        IReadOnlyDictionary<ProductForm, decimal> byForm,
        IReadOnlyDictionary<MetalProduct, decimal>? byProduct = null,
        decimal maximum = decimal.MaxValue)
    {
        ArgumentNullException.ThrowIfNull(byForm);

        _byForm = new Dictionary<ProductForm, decimal>(byForm);
        _byProduct = byProduct is null ? new Dictionary<MetalProduct, decimal>() : new Dictionary<MetalProduct, decimal>(byProduct);

        foreach (var rate in _byForm.Values.Concat(_byProduct.Values))
        {
            if (rate < 0m || rate >= maximum)
            {
                throw new ArgumentOutOfRangeException(nameof(byForm), rate,
                    $"Ogni percentuale deve essere almeno 0 e minore di {maximum}.");
            }
        }
    }

    /// <summary>
    /// La percentuale da applicare a un prodotto.
    /// </summary>
    /// <param name="product">Il prodotto.</param>
    /// <returns>La percentuale del prodotto se configurata, altrimenti quella della sua forma.</returns>
    /// <exception cref="InvalidOperationException">Né il prodotto né la sua forma sono configurati.</exception>
    public decimal For(MetalProduct product)
    {
        ArgumentNullException.ThrowIfNull(product);

        if (_byProduct.TryGetValue(product, out var specific))
        {
            return specific;
        }

        if (_byForm.TryGetValue(product.Form, out var general))
        {
            return general;
        }

        throw new InvalidOperationException(
            $"Nessuna percentuale configurata per '{product.Name}' né per la forma {product.Form}.");
    }
}