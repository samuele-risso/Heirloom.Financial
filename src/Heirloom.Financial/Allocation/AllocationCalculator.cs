using Heirloom.Financial.Domain;
using Heirloom.Financial.Internal;
using Heirloom.Financial.Positions;
using Heirloom.Financial.Validation;

namespace Heirloom.Financial.Allocation;

/// <summary>
/// Divide il portafoglio in fette per metallo, per forma o per entrambi: i dati dei grafici a ciambella.
/// </summary>
/// <remarks>
/// <para>
/// Le fette sono <b>sempre tutte presenti</b>, anche quelle vuote (con valore zero), e sempre nello stesso
/// ordine: oro prima dell'argento, monete prima dei lingotti. Così il frontend riceve ogni volta la stessa forma.
/// </para>
/// <para>
/// Le percentuali di <see cref="Calculate"/> non sono arrotondate. Per mostrarle si usa
/// <see cref="RoundPercentages"/>, che le arrotonda facendo in modo che la somma resti esattamente 100%.
/// </para>
/// </remarks>
public static class AllocationCalculator
{
    /// <summary>
    /// Calcola le fette a partire dal riepilogo dei guadagni, che contiene già il valore di ogni posizione.
    /// </summary>
    /// <param name="summary">Il riepilogo prodotto da <see cref="PnLCalculator"/>.</param>
    /// <param name="grouping">Come raggruppare.</param>
    /// <param name="basis">Se calcolare le percentuali sul valore o sul peso fino.</param>
    /// <returns>Le fette, sempre tutte, in ordine fisso.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="summary"/> è null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Raggruppamento o base non definiti.</exception>
    public static IReadOnlyList<AllocationSlice> Calculate(PnLSummary summary, AllocationGrouping grouping, AllocationBasis basis)
    {
        ArgumentNullException.ThrowIfNull(summary);
        DomainGuards.Defined(grouping);
        DomainGuards.Defined(basis);

        var slices = Keys(grouping)
            .Select(key =>
            {
                var members = summary.Positions
                    .Where(p => Matches(p.Position.Product, key))
                    .ToList();

                return new AllocationSlice(
                    key.Metal,
                    key.Form,
                    members.Aggregate(Money.Zero(summary.Currency), (sum, p) => sum + p.Value),
                    members.Aggregate(Weight.Zero, (sum, p) => sum + p.Position.FineWeight),
                    Percent: null);
            })
            .ToList();

        var total = slices.Sum(s => Measure(s, basis));

        if (total == 0m)
        {
            return slices;
        }

        return slices
            .Select(s => s with { Percent = Measure(s, basis) / total })
            .ToList();
    }

    /// <summary>
    /// Arrotonda le percentuali a 4 decimali (0,01%) mantenendo la somma esattamente uguale a 1 (100%).
    /// Lo scarto dovuto all'arrotondamento va alla fetta più grande.
    /// </summary>
    /// <param name="slices">Le fette con le percentuali non arrotondate.</param>
    /// <returns>Le stesse fette con le percentuali arrotondate; invariate se le percentuali sono assenti.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="slices"/> è null.</exception>
    public static IReadOnlyList<AllocationSlice> RoundPercentages(IReadOnlyList<AllocationSlice> slices)
    {
        ArgumentNullException.ThrowIfNull(slices);

        if (slices.Count == 0 || slices.Any(s => s.Percent is null))
        {
            return slices;
        }

        var rounded = slices
            .Select(s => s with { Percent = Rounding.Percentage(s.Percent!.Value) })
            .ToList();

        var difference = 1m - rounded.Sum(s => s.Percent!.Value);

        if (difference != 0m)
        {
            var largest = rounded.IndexOf(rounded.MaxBy(s => s.Percent!.Value)!);
            rounded[largest] = rounded[largest] with { Percent = rounded[largest].Percent + difference };
        }

        return rounded;
    }

    private static decimal Measure(AllocationSlice slice, AllocationBasis basis) =>
        basis == AllocationBasis.Value ? slice.Value.Amount : slice.FineWeight.Grams;

    private static bool Matches(MetalProduct product, (Metal? Metal, ProductForm? Form) key) =>
        (key.Metal is null || product.Metal == key.Metal) &&
        (key.Form is null || product.Form == key.Form);

    private static IEnumerable<(Metal? Metal, ProductForm? Form)> Keys(AllocationGrouping grouping)
    {
        var metals = Enum.GetValues<Metal>();
        var forms = Enum.GetValues<ProductForm>();

        return grouping switch
        {
            AllocationGrouping.Metal => metals.Select(m => ((Metal?)m, (ProductForm?)null)),
            AllocationGrouping.Form => forms.Select(f => ((Metal?)null, (ProductForm?)f)),
            _ => metals.SelectMany(m => forms.Select(f => ((Metal?)m, (ProductForm?)f))),
        };
    }
}