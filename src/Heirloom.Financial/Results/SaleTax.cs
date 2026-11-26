namespace Heirloom.Financial.Results;

/// <summary>
/// Le tasse stimate su una vendita già fatta, con la regola in vigore il giorno della vendita.
/// </summary>
/// <param name="LotId">L'id della vendita, se il lotto ne ha uno.</param>
/// <param name="Product">Il nome del prodotto venduto.</param>
/// <param name="Date">Il giorno della vendita.</param>
/// <param name="Quantity">I pezzi venduti.</param>
/// <param name="DocumentedQuantity">Quanti di quei pezzi avevano il costo documentato (fattura).</param>
/// <param name="Proceeds">L'incasso netto.</param>
/// <param name="RealizedPnL">Il guadagno sui pezzi a costo noto.</param>
/// <param name="Tax">Le tasse stimate.</param>
public sealed record SaleTax(
    string? LotId,
    string Product,
    DateOnly Date,
    decimal Quantity,
    decimal DocumentedQuantity,
    MoneyResult Proceeds,
    MoneyResult RealizedPnL,
    MoneyResult Tax);

/// <summary>
/// Le vendite di un anno solare con le loro tasse stimate: è l'anno in cui vanno dichiarate.
/// </summary>
/// <param name="Year">L'anno.</param>
/// <param name="SalesCount">Il numero di vendite.</param>
/// <param name="Proceeds">L'incasso netto dell'anno.</param>
/// <param name="RealizedPnL">Il guadagno dell'anno sui pezzi a costo noto.</param>
/// <param name="Tax">Le tasse stimate dell'anno.</param>
public sealed record TaxYear(
    int Year,
    int SalesCount,
    MoneyResult Proceeds,
    MoneyResult RealizedPnL,
    MoneyResult Tax);