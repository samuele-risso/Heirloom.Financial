using Heirloom.Financial.Domain;

namespace Heirloom.Financial.Positions;

/// <summary>
/// L'ordine in cui si elaborano i lotti, uguale in tutta la libreria.
/// </summary>
internal static class LotOrdering
{
    /// <summary>
    /// Ordina i lotti per data; a parità di data gli acquisti vengono prima delle vendite.
    /// A parità di data e verso si mantiene l'ordine ricevuto.
    /// </summary>
    /// <param name="lots">I lotti, in qualsiasi ordine.</param>
    /// <returns>I lotti in ordine cronologico.</returns>
    public static IEnumerable<Lot> Chronological(IEnumerable<Lot> lots) =>
        lots.OrderBy(l => l.Date).ThenBy(l => l.Side == LotSide.Buy ? 0 : 1);
}