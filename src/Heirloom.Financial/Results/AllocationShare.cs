using System.Text.Json.Serialization;
using Heirloom.Financial.Allocation;
using Heirloom.Financial.Domain;
using Heirloom.Financial.Internal;

namespace Heirloom.Financial.Results;

/// <summary>
/// Una fetta della ripartizione, pronta per un grafico a torta.
/// </summary>
/// <param name="Metal">Il metallo della fetta; assente se si raggruppa solo per forma.</param>
/// <param name="Form">La forma della fetta; assente se si raggruppa solo per metallo.</param>
/// <param name="Value">Il valore della fetta.</param>
/// <param name="FineWeightGrams">Il metallo puro della fetta in grammi, a 4 decimali.</param>
/// <param name="Percent">La quota sul totale, come frazione a 4 decimali (le fette sommano a 1); assente se il portafoglio è vuoto.</param>
public sealed record AllocationShare(
    [property: JsonConverter(typeof(JsonStringEnumConverter<Metal>))] Metal? Metal,
    [property: JsonConverter(typeof(JsonStringEnumConverter<ProductForm>))] ProductForm? Form,
    MoneyResult Value,
    decimal FineWeightGrams,
    decimal? Percent)
{
    /// <summary>Costruisce le fette arrotondate, con le percentuali che sommano a 1.</summary>
    /// <param name="slices">Le fette calcolate da <see cref="AllocationCalculator"/>.</param>
    /// <returns>Le fette pronte da mostrare, nello stesso ordine.</returns>
    public static IReadOnlyList<AllocationShare> From(IReadOnlyList<AllocationSlice> slices)
    {
        ArgumentNullException.ThrowIfNull(slices);

        return AllocationCalculator.RoundPercentages(slices)
            .Select(s => new AllocationShare(
                s.Metal,
                s.Form,
                MoneyResult.From(s.Value),
                Rounding.Grams(s.FineWeight).Grams,
                s.Percent))
            .ToList();
    }
}