using Heirloom.Financial.Domain;

namespace Heirloom.Financial.Positions;

/// <summary>
/// Un acquisto di cui si possiedono ancora dei pezzi, con il costo di quelli rimasti.
/// </summary>
/// <remarks>
/// Esiste con i metodi <see cref="CostBasisMethod.Fifo"/> e <see cref="CostBasisMethod.SpecificLot"/>,
/// che tengono separati gli acquisti. Con la media ponderata gli acquisti si fondono e i lotti aperti non esistono.
/// </remarks>
/// <param name="Purchase">Il lotto d'acquisto originale.</param>
/// <param name="Quantity">I pezzi ancora posseduti di quell'acquisto.</param>
/// <param name="Cost">Il costo dei pezzi rimasti (la loro parte del costo d'acquisto); null se sconosciuto.</param>
public sealed record OpenLot(Lot Purchase, decimal Quantity, Money? Cost)
{
    /// <summary>Il prodotto.</summary>
    public MetalProduct Product => Purchase.Product;

    /// <summary>Se il costo è noto.</summary>
    public bool HasKnownCost => Cost is not null;

    /// <summary>Se il costo è documentato (fattura).</summary>
    public bool HasDocumentedCost => Purchase.HasDocumentedCost;

    /// <summary>Il peso fino dei pezzi rimasti.</summary>
    public Weight FineWeight => Product.FineWeight * Quantity;

    /// <summary>Il peso lordo dei pezzi rimasti.</summary>
    public Weight GrossWeight => Product.GrossWeight * Quantity;
}