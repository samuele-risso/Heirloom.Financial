namespace Heirloom.Financial.Results;

/// <summary>
/// I soldi entrati e usciti con acquisti e vendite, dall'inizio.
/// </summary>
/// <param name="TotalPurchases">Il totale speso negli acquisti, costi accessori inclusi (gli acquisti a costo sconosciuto contano zero).</param>
/// <param name="PurchaseCount">Il numero di acquisti, compresi quelli a costo sconosciuto.</param>
/// <param name="TotalSales">Il totale incassato dalle vendite, al netto dei costi accessori.</param>
/// <param name="SalesCount">Il numero di vendite.</param>
/// <param name="NetCashFlow">Incassato − speso: negativo finché si è investito più di quanto si è venduto.</param>
public sealed record CashFlowSummary(
    MoneyResult TotalPurchases,
    int PurchaseCount,
    MoneyResult TotalSales,
    int SalesCount,
    MoneyResult NetCashFlow);