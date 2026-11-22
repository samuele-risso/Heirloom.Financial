using Heirloom.Financial.Domain;

namespace Heirloom.Financial.Positions;

/// <summary>
/// Quanto si possiede oggi di un prodotto: la quantità netta (acquisti meno vendite)
/// e il metallo puro che contiene. Non dice nulla sui costi: per quelli c'è <see cref="Position"/>.
/// </summary>
/// <param name="Product">Il prodotto.</param>
/// <param name="Quantity">I pezzi posseduti; sempre maggiore di zero.</param>
public sealed record Holding(MetalProduct Product, decimal Quantity)
{
    /// <summary>
    /// Il metallo puro contenuto in tutti i pezzi posseduti.
    /// </summary>
    public Weight FineWeight => Product.FineWeight * Quantity;
}