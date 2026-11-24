using System.Text.Json.Serialization;
using Heirloom.Financial.Domain;

namespace Heirloom.Financial.Results;

/// <summary>
/// Il riepilogo di ciò che si possiede, senza prezzi di mercato: pezzi, pesi, costo di carico,
/// anzianità e flusso di cassa. Già arrotondato e pronto per l'API.
/// </summary>
/// <remarks>
/// Serve dove non c'è (o non serve) una valutazione: per esempio il riepilogo delle sole monete o dei soli
/// lingotti di un wallet. Per valore, guadagni, tasse e rendimenti si usa <see cref="WalletAnalyzer"/>.
/// </remarks>
/// <param name="Currency">Il codice ISO della valuta degli importi.</param>
/// <param name="AsOf">Il giorno del riepilogo.</param>
/// <param name="OpenLotCount">Quanti lotti d'acquisto hanno ancora pezzi.</param>
/// <param name="ProductCount">Quanti prodotti diversi si possiedono.</param>
/// <param name="Quantity">I pezzi posseduti.</param>
/// <param name="UnknownCostQuantity">I pezzi posseduti senza costo noto (eredità, regali).</param>
/// <param name="GrossWeightGrams">Il peso lordo posseduto, in grammi.</param>
/// <param name="FineWeightGrams">Il peso di metallo fino posseduto, in grammi.</param>
/// <param name="CostBasis">Il costo di carico dei pezzi posseduti a costo noto.</param>
/// <param name="AverageHoldingDays">
/// Da quanti giorni, in media, si possiedono i pezzi: media dei giorni dei lotti aperti pesata sul peso lordo;
/// null se non si possiede nulla.
/// </param>
/// <param name="ByMetal">Lo stesso riepilogo per metallo, solo per i metalli posseduti o scambiati.</param>
/// <param name="CashFlow">Acquisti e vendite di tutte le operazioni, anche dei lotti chiusi.</param>
public sealed record HoldingsSummary(
    string Currency,
    DateOnly AsOf,
    int OpenLotCount,
    int ProductCount,
    decimal Quantity,
    decimal UnknownCostQuantity,
    decimal GrossWeightGrams,
    decimal FineWeightGrams,
    MoneyResult CostBasis,
    decimal? AverageHoldingDays,
    IReadOnlyList<HoldingsMetal> ByMetal,
    CashFlowSummary CashFlow)
{
    /// <summary>Il riepilogo di un metallo, o null se non è né posseduto né scambiato.</summary>
    /// <param name="metal">Il metallo.</param>
    /// <returns>Il riepilogo del metallo, o null.</returns>
    public HoldingsMetal? For(Metal metal) => ByMetal.FirstOrDefault(m => m.Metal == metal);
}

/// <summary>Il riepilogo di un metallo in <see cref="HoldingsSummary"/>.</summary>
/// <param name="Metal">Il metallo.</param>
/// <param name="OpenLotCount">Quanti lotti d'acquisto di questo metallo hanno ancora pezzi.</param>
/// <param name="Quantity">I pezzi posseduti.</param>
/// <param name="UnknownCostQuantity">I pezzi posseduti senza costo noto.</param>
/// <param name="GrossWeightGrams">Il peso lordo posseduto, in grammi.</param>
/// <param name="FineWeightGrams">Il peso fino posseduto, in grammi.</param>
/// <param name="CostBasis">Il costo di carico dei pezzi a costo noto.</param>
/// <param name="AverageCostPerFineGram">Il costo medio per grammo fino, solo sui pezzi a costo noto; null se non ce ne sono.</param>
/// <param name="CashFlow">Acquisti e vendite di questo metallo.</param>
public sealed record HoldingsMetal(
    [property: JsonConverter(typeof(JsonStringEnumConverter<Metal>))] Metal Metal,
    int OpenLotCount,
    decimal Quantity,
    decimal UnknownCostQuantity,
    decimal GrossWeightGrams,
    decimal FineWeightGrams,
    MoneyResult CostBasis,
    decimal? AverageCostPerFineGram,
    CashFlowSummary CashFlow);
