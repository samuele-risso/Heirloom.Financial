using System.Text.Json.Serialization;
using Heirloom.Financial.Domain;

namespace Heirloom.Financial.Results;

/// <summary>
/// Il riepilogo di un metallo: pesi, valore, costo e prezzi al grammo.
/// </summary>
/// <param name="Metal">Il metallo.</param>
/// <param name="GrossWeightGrams">Il peso lordo posseduto, a 4 decimali.</param>
/// <param name="FineWeightGrams">Il peso fino posseduto, a 4 decimali.</param>
/// <param name="TroyOunces">Il peso fino in once troy, a 4 decimali.</param>
/// <param name="Value">Il valore attuale.</param>
/// <param name="CostBasis">Il costo di carico dei pezzi a costo noto.</param>
/// <param name="AverageCostPerFineGram">Il costo medio per grammo fino dei soli pezzi a costo noto, a 4 decimali; null se non ce ne sono.</param>
/// <param name="SpotPerFineGram">Lo spot per grammo di metallo puro, a 4 decimali.</param>
/// <param name="CoinWeightShare">La quota di peso lordo in monete, come frazione a 4 decimali.</param>
/// <param name="BarWeightShare">La quota di peso lordo in lingotti, come frazione a 4 decimali.</param>
public sealed record MetalSummary(
    [property: JsonConverter(typeof(JsonStringEnumConverter<Metal>))] Metal Metal,
    decimal GrossWeightGrams,
    decimal FineWeightGrams,
    decimal TroyOunces,
    MoneyResult Value,
    MoneyResult CostBasis,
    decimal? AverageCostPerFineGram,
    decimal SpotPerFineGram,
    decimal CoinWeightShare,
    decimal BarWeightShare)
{
    /// <summary>
    /// Di quanto lo spot di oggi è sopra (positivo) o sotto (negativo) il costo medio per grammo fino, come frazione
    /// del costo medio a 4 decimali; null se non ci sono pezzi a costo noto.
    /// </summary>
    public decimal? SpotVsAverageCost { get; init; }
}