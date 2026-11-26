using Heirloom.Financial.Domain;

namespace Heirloom.Financial.Positions;

/// <summary>
/// L'esito di una vendita: quanto si è incassato, quanto costavano i pezzi venduti e il guadagno realizzato.
/// </summary>
/// <remarks>
/// Se una parte dei pezzi venduti aveva costo sconosciuto, l'incasso viene diviso in proporzione
/// ai pezzi: il guadagno si calcola solo sulla parte a costo noto.
/// </remarks>
/// <param name="Sale">Il lotto di vendita.</param>
/// <param name="CostOfSold">Il costo di carico dei pezzi venduti a costo noto, secondo il metodo scelto.</param>
/// <param name="UnknownCostQuantity">Quanti dei pezzi venduti avevano costo sconosciuto.</param>
public sealed record SaleResult(Lot Sale, Money CostOfSold, decimal UnknownCostQuantity = 0m)
{
    /// <summary>
    /// Quanti dei pezzi venduti venivano da acquisti con il costo documentato (fattura).
    /// Sempre zero con la media ponderata, dove gli acquisti si fondono.
    /// </summary>
    public decimal DocumentedCostQuantity { get; init; }

    /// <summary>
    /// Il costo dei pezzi venduti con il costo documentato: è già compreso in <see cref="CostOfSold"/>.
    /// </summary>
    public Money? DocumentedCostOfSold { get; init; }

    /// <summary>
    /// I pezzi venduti di cui si conosceva il costo.
    /// </summary>
    public decimal KnownCostQuantity => Sale.Quantity - UnknownCostQuantity;

    /// <summary>
    /// Indica se tutti i pezzi venduti avevano costo noto.
    /// </summary>
    public bool HasKnownCost => UnknownCostQuantity == 0m;

    /// <summary>
    /// L'incasso netto: lordo meno commissioni di vendita.
    /// </summary>
    public Money Proceeds => Sale.NetAmount;

    /// <summary>
    /// La parte dell'incasso che corrisponde ai pezzi a costo noto.
    /// </summary>
    public Money KnownCostProceeds => HasKnownCost ? Proceeds : Proceeds * KnownCostQuantity / Sale.Quantity;

    /// <summary>
    /// La parte dell'incasso che corrisponde ai pezzi a costo sconosciuto.
    /// </summary>
    public Money UnknownCostProceeds => Proceeds - KnownCostProceeds;

    /// <summary>
    /// Il guadagno (o la perdita) realizzato sui pezzi a costo noto: la loro parte di incasso meno il loro costo.
    /// </summary>
    public Money RealizedPnL => KnownCostProceeds - CostOfSold;
}