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
/// <param name="GoldFineWeightGrams">L'oro puro posseduto, in grammi.</param>
/// <param name="SilverFineWeightGrams">L'argento puro posseduto, in grammi.</param>
/// <param name="Positions">Le posizioni aperte.</param>
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
    decimal GoldFineWeightGrams,
    decimal SilverFineWeightGrams,
    IReadOnlyList<PositionValuation> Positions,
    IReadOnlyList<AllocationShare> AllocationByMetal,
    IReadOnlyList<AllocationShare> AllocationByForm,
    PerformanceSummary? Performance,
    RiskSummary? Risk,
    IReadOnlyList<string> Notes);