using Heirloom.Financial.Domain;

namespace Heirloom.Financial.Positions;

/// <summary>
/// Una posizione aperta: quanti pezzi di un prodotto si possiedono e quanto sono costati.
/// </summary>
/// <param name="Product">Il prodotto.</param>
/// <param name="Quantity">I pezzi posseduti; sempre maggiore di zero.</param>
/// <param name="CostBasis">Il costo di carico dei pezzi posseduti, costi accessori inclusi.</param>
public sealed record Position(MetalProduct Product, decimal Quantity, Money CostBasis)
{
    /// <summary>
    /// Il prezzo medio di carico di un pezzo.
    /// </summary>
    public Money AverageCost => CostBasis / Quantity;

    /// <summary>
    /// Il metallo puro contenuto in tutti i pezzi posseduti.
    /// </summary>
    public Weight FineWeight => Product.FineWeight * Quantity;
}