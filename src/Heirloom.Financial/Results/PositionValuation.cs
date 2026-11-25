using System.Text.Json.Serialization;
using Heirloom.Financial.Domain;
using Heirloom.Financial.Internal;
using Heirloom.Financial.Positions;

namespace Heirloom.Financial.Results;

/// <summary>
/// Una riga della tabella delle posizioni: quantità, metallo, valore, costo e guadagno di un prodotto.
/// </summary>
/// <param name="Product">Il nome del prodotto.</param>
/// <param name="Metal">Il metallo.</param>
/// <param name="Form">La forma: moneta o lingotto.</param>
/// <param name="Quantity">I pezzi posseduti, compresi quelli a costo sconosciuto.</param>
/// <param name="UnknownCostQuantity">Quanti pezzi hanno costo sconosciuto (eredità, regali).</param>
/// <param name="FineWeightGrams">Il metallo puro in grammi, a 4 decimali.</param>
/// <param name="Value">Il valore attuale di tutti i pezzi.</param>
/// <param name="CostBasis">Il costo di carico dei pezzi a costo noto.</param>
/// <param name="AverageCost">Il prezzo medio di un pezzo a costo noto; assente se non ce ne sono.</param>
/// <param name="UnrealizedPnL">Il guadagno sulla carta dei pezzi a costo noto.</param>
/// <param name="UnrealizedPercent">Lo stesso guadagno come frazione del costo, a 4 decimali; assente se il costo è zero.</param>
public sealed record PositionValuation(
    string Product,
    [property: JsonConverter(typeof(JsonStringEnumConverter<Metal>))] Metal Metal,
    [property: JsonConverter(typeof(JsonStringEnumConverter<ProductForm>))] ProductForm Form,
    decimal Quantity,
    decimal UnknownCostQuantity,
    decimal FineWeightGrams,
    MoneyResult Value,
    MoneyResult CostBasis,
    MoneyResult? AverageCost,
    MoneyResult UnrealizedPnL,
    decimal? UnrealizedPercent)
{
    /// <summary>Il peso lordo posseduto, in grammi.</summary>
    public decimal GrossWeightGrams { get; init; }

    /// <summary>Il valore di un pezzo al prezzo di oggi.</summary>
    public MoneyResult? UnitValue { get; init; }

    /// <summary>Costruisce la riga, arrotondando ogni valore.</summary>
    /// <param name="pnl">Il guadagno della posizione.</param>
    /// <returns>La riga pronta da mostrare.</returns>
    public static PositionValuation From(PositionPnL pnl)
    {
        ArgumentNullException.ThrowIfNull(pnl);
        var position = pnl.Position;

        return new PositionValuation(
            position.Product.Name,
            position.Product.Metal,
            position.Product.Form,
            position.Quantity,
            position.UnknownCostQuantity,
            Rounding.Grams(position.FineWeight).Grams,
            MoneyResult.From(pnl.Value),
            MoneyResult.From(position.CostBasis),
            MoneyResult.FromOptional(position.AverageCost),
            MoneyResult.From(pnl.UnrealizedPnL),
            Percent(pnl.UnrealizedPercent))
        {
            GrossWeightGrams = Rounding.Grams(position.Product.GrossWeight * position.Quantity).Grams,
            UnitValue = position.Quantity > 0m ? MoneyResult.From(pnl.Value / position.Quantity) : null,
        };
    }

    internal static decimal? Percent(decimal? fraction) => fraction is { } f ? Rounding.Percentage(f) : null;
}