using Heirloom.Financial.Domain;

namespace Heirloom.Financial.Valuation;

/// <summary>
/// Valuta ogni prodotto al solo valore del metallo contenuto (valore di fusione).
/// </summary>
/// <remarks>
/// È la strategia più oggettiva e prudente. Sottostima le monete, che sul mercato
/// valgono un po' più del loro metallo.
/// </remarks>
public sealed class SpotValuation : IValuationStrategy
{
    /// <summary>
    /// L'unica istanza necessaria: la strategia non ha configurazione.
    /// </summary>
    public static SpotValuation Instance { get; } = new();

    /// <inheritdoc />
    public Money UnitValue(MetalProduct product, SpotPrice spot) => MeltValueCalculator.MeltValue(product, spot);
}