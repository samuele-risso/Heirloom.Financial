using System.Text.Json.Serialization;
using Heirloom.Financial.Positions;

namespace Heirloom.Financial.Results;

/// <summary>
/// Il riepilogo completo del portafoglio, pronto da restituire dall'API: tutti gli importi sono
/// arrotondati a 2 decimali, i pesi a 4, le percentuali (come frazioni) a 4.
/// </summary>
/// <param name="Currency">Il codice ISO della valuta del riepilogo.</param>
/// <param name="ValuationDate">Il giorno della valutazione.</param>
/// <param name="CostBasisMethod">Il metodo usato per il costo di carico.</param>
/// <param name="TotalValue">Il valore di tutte le posizioni, compresi i pezzi a costo sconosciuto.</param>
/// <param name="KnownCostValue">Il valore dei soli pezzi a costo noto: la base del guadagno.</param>
/// <param name="UnknownCostValue">Il valore dei pezzi a costo sconosciuto.</param>
/// <param name="TotalCostBasis">Il costo di carico delle posizioni aperte.</param>
/// <param name="UnrealizedPnL">Il guadagno sulla carta.</param>
/// <param name="UnrealizedPercent">Il guadagno sulla carta come frazione del costo; assente se il costo è zero.</param>
/// <param name="RealizedPnL">Il guadagno già incassato con le vendite.</param>
/// <param name="UnknownCostProceeds">L'incasso di vendite di pezzi a costo sconosciuto, senza guadagno calcolabile.</param>
/// <param name="TotalPnL">Il guadagno complessivo: realizzato più sulla carta.</param>
/// <param name="EstimatedTax">Le tasse stimate se si vendesse tutto oggi; null senza regola fiscale nelle opzioni.</param>
/// <param name="ValueAfterTax">Il valore totale meno le tasse stimate; null senza regola fiscale.</param>
/// <param name="UnrealizedPnLAfterTax">Il guadagno sulla carta dei lotti a costo noto, meno le loro tasse; null senza regola fiscale.</param>
/// <param name="GoldFineWeightGrams">L'oro puro posseduto, in grammi.</param>
/// <param name="SilverFineWeightGrams">L'argento puro posseduto, in grammi.</param>
/// <param name="Positions">Le posizioni aperte, una per prodotto.</param>
/// <param name="Lots">I lotti aperti, uno per acquisto (vuoto con la media ponderata).</param>
/// <param name="ByMetal">Il riepilogo per metallo.</param>
/// <param name="CashFlow">Acquisti e vendite dall'inizio.</param>
/// <param name="AllocationByMetal">La ripartizione del valore tra oro e argento.</param>
/// <param name="AllocationByForm">La ripartizione del valore tra monete e lingotti.</param>
/// <param name="Performance">I rendimenti; assenti se non c'è storico o mancano i prezzi per ricostruirlo.</param>
/// <param name="Risk">Le misure di rischio; assenti se lo storico è troppo corto.</param>
/// <param name="Notes">Avvisi per chi legge (es. perché mancano i rendimenti).</param>
public sealed record WalletSummary(
    string Currency,
    DateOnly ValuationDate,
    [property: JsonConverter(typeof(JsonStringEnumConverter<CostBasisMethod>))] CostBasisMethod CostBasisMethod,
    MoneyResult TotalValue,
    MoneyResult KnownCostValue,
    MoneyResult UnknownCostValue,
    MoneyResult TotalCostBasis,
    MoneyResult UnrealizedPnL,
    decimal? UnrealizedPercent,
    MoneyResult RealizedPnL,
    MoneyResult UnknownCostProceeds,
    MoneyResult TotalPnL,
    MoneyResult? EstimatedTax,
    MoneyResult? ValueAfterTax,
    MoneyResult? UnrealizedPnLAfterTax,
    decimal GoldFineWeightGrams,
    decimal SilverFineWeightGrams,
    IReadOnlyList<PositionValuation> Positions,
    IReadOnlyList<LotValuation> Lots,
    IReadOnlyList<MetalSummary> ByMetal,
    CashFlowSummary CashFlow,
    IReadOnlyList<AllocationShare> AllocationByMetal,
    IReadOnlyList<AllocationShare> AllocationByForm,
    PerformanceSummary? Performance,
    RiskSummary? Risk,
    IReadOnlyList<string> Notes)
{
    /// <summary>Le tasse stimate sulle vendite già fatte; null senza regola fiscale nelle opzioni.</summary>
    public MoneyResult? RealizedTax { get; init; }

    /// <summary>Le tasse stimate di ogni vendita, in ordine di data; vuoto senza regola fiscale.</summary>
    public IReadOnlyList<SaleTax> SaleTaxes { get; init; } = [];

    /// <summary>Vendite e tasse stimate raggruppate per anno, dal più recente; vuoto senza regola fiscale.</summary>
    public IReadOnlyList<TaxYear> TaxYears { get; init; } = [];
}