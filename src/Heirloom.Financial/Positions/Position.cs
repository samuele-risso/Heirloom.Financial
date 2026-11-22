using Heirloom.Financial.Domain;

namespace Heirloom.Financial.Positions;

/// <summary>
/// Una posizione aperta: quanti pezzi di un prodotto si possiedono e quanto sono costati.
/// </summary>
/// <param name="Product">Il prodotto.</param>
/// <param name="Quantity">I pezzi posseduti, compresi quelli a costo sconosciuto; sempre maggiore di zero.</param>
/// <param name="CostBasis">Il costo di carico dei soli pezzi a costo noto, costi accessori inclusi.</param>
/// <param name="UnknownCostQuantity">Quanti dei pezzi posseduti hanno costo sconosciuto (eredità, regali).</param>
public sealed record Position(MetalProduct Product, decimal Quantity, Money CostBasis, decimal UnknownCostQuantity = 0m)
{
    /// <summary>
    /// I pezzi di cui si conosce il costo.
    /// </summary>
    public decimal KnownCostQuantity => Quantity - UnknownCostQuantity;

    /// <summary>
    /// Indica se una parte della posizione ha costo sconosciuto.
    /// </summary>
    public bool HasUnknownCost => UnknownCostQuantity > 0m;

    /// <summary>
    /// Il prezzo medio di carico di un pezzo a costo noto; assente se nessun pezzo ha costo noto.
    /// </summary>
    public Money? AverageCost => KnownCostQuantity > 0m ? CostBasis / KnownCostQuantity : null;

    /// <summary>
    /// Il metallo puro contenuto in tutti i pezzi posseduti.
    /// </summary>
    public Weight FineWeight => Product.FineWeight * Quantity;
}