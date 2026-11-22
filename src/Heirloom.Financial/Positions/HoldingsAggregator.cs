using Heirloom.Financial.Domain;
using Heirloom.Financial.Validation;

namespace Heirloom.Financial.Positions;

/// <summary>
/// Trasforma la storia delle operazioni nello stato attuale: cosa si possiede, e quanto metallo puro.
/// </summary>
public static class HoldingsAggregator
{
    /// <summary>
    /// Le posizioni nette per prodotto. I prodotti venduti del tutto non compaiono.
    /// </summary>
    /// <param name="lots">Le operazioni, in qualsiasi ordine.</param>
    /// <returns>Una voce per ogni prodotto ancora posseduto, ordinate per metallo, forma e nome.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="lots"/> è null.</exception>
    /// <exception cref="ArgumentException">In qualche data si vende più di quanto si possiede.</exception>
    public static IReadOnlyList<Holding> Aggregate(IEnumerable<Lot> lots)
    {
        var list = DomainGuards.NoOversell(lots);

        return list
            .GroupBy(l => l.Product)
            .Select(g => new Holding(g.Key, g.Sum(l => l.Side == LotSide.Buy ? l.Quantity : -l.Quantity)))
            .Where(h => h.Quantity > 0m)
            .OrderBy(h => h.Product.Metal)
            .ThenBy(h => h.Product.Form)
            .ThenBy(h => h.Product.Name, StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>
    /// Il metallo puro posseduto di un certo metallo, sommando tutti i prodotti.
    /// </summary>
    /// <param name="lots">Le operazioni, in qualsiasi ordine.</param>
    /// <param name="metal">Il metallo.</param>
    /// <returns>Il peso fino totale posseduto di quel metallo.</returns>
    public static Weight TotalFineWeight(IEnumerable<Lot> lots, Metal metal)
    {
        DomainGuards.Defined(metal);

        return Aggregate(lots)
            .Where(h => h.Product.Metal == metal)
            .Aggregate(Weight.Zero, (total, h) => total + h.FineWeight);
    }
}