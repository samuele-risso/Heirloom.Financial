using Heirloom.Financial.Domain;

namespace Heirloom.Financial.Positions;

/// <summary>
/// L'esito di una vendita: quanto si è incassato, quanto costavano i pezzi venduti e il guadagno realizzato.
/// </summary>
/// <param name="Sale">Il lotto di vendita.</param>
/// <param name="CostOfSold">Il costo di carico dei pezzi venduti, secondo il metodo scelto.</param>
public sealed record SaleResult(Lot Sale, Money CostOfSold)
{
    /// <summary>
    /// L'incasso netto: lordo meno commissioni di vendita.
    /// </summary>
    public Money Proceeds => Sale.NetAmount;

    /// <summary>
    /// Il guadagno (o la perdita) realizzato: incasso netto meno costo dei pezzi venduti.
    /// </summary>
    public Money RealizedPnL => Proceeds - CostOfSold;
}