using Heirloom.Financial.Domain;
using Heirloom.Financial.Performance;

namespace Heirloom.Financial.Risk;

/// <summary>
/// Il drawdown di un giorno: quanto il valore è sotto il massimo raggiunto fino a quel giorno.
/// </summary>
/// <param name="Date">Il giorno.</param>
/// <param name="Drawdown">La distanza dal massimo precedente, come frazione: 0 sul massimo, −0,25 = 25% sotto.</param>
public readonly record struct DrawdownPoint(DateOnly Date, decimal Drawdown);

/// <summary>
/// Il peggior calo della serie: da un massimo al minimo successivo.
/// </summary>
/// <param name="Depth">La profondità del calo, come frazione negativa (−0,25 = −25%); zero se non ci sono cali.</param>
/// <param name="PeakDate">Il giorno del massimo da cui è iniziato il calo.</param>
/// <param name="TroughDate">Il giorno del minimo.</param>
/// <param name="RecoveryDate">Il primo giorno in cui il valore è tornato al massimo precedente; assente se non è ancora successo.</param>
public sealed record MaxDrawdown(decimal Depth, DateOnly PeakDate, DateOnly TroughDate, DateOnly? RecoveryDate);

/// <summary>
/// Calcola il drawdown: il dato di rischio più intuitivo per l'utente ("quanto ho perso dal punto più alto").
/// </summary>
/// <remarks>
/// Per il portafoglio si calcola sulla serie TWR e non sul valore assoluto: altrimenti un nuovo acquisto
/// durante un calo verrebbe scambiato per un recupero.
/// </remarks>
public static class DrawdownCalculator
{
    /// <summary>
    /// La serie del drawdown giorno per giorno, pronta per un grafico.
    /// </summary>
    /// <param name="series">La serie indicizzata (es. da <see cref="TimeWeightedReturn.Index"/>), in ordine di data.</param>
    /// <returns>Un punto per ogni punto della serie.</returns>
    public static IReadOnlyList<DrawdownPoint> Series(IEnumerable<IndexPoint> series)
    {
        ArgumentNullException.ThrowIfNull(series);

        var points = new List<DrawdownPoint>();
        var peak = 0m;

        foreach (var point in series.OrderBy(p => p.Date))
        {
            peak = Math.Max(peak, point.Value);
            points.Add(new DrawdownPoint(point.Date, peak == 0m ? 0m : point.Value / peak - 1m));
        }

        return points;
    }

    /// <summary>
    /// Il peggior calo della serie, con le date di massimo, minimo e recupero.
    /// </summary>
    /// <param name="series">La serie indicizzata, in qualsiasi ordine.</param>
    /// <returns>Il drawdown massimo; assente se la serie è vuota.</returns>
    public static MaxDrawdown? Max(IEnumerable<IndexPoint> series)
    {
        ArgumentNullException.ThrowIfNull(series);

        var ordered = series.OrderBy(p => p.Date).ToList();

        if (ordered.Count == 0)
        {
            return null;
        }

        var peak = ordered[0];
        var worst = new MaxDrawdown(0m, ordered[0].Date, ordered[0].Date, null);
        var worstPeakValue = ordered[0].Value;

        foreach (var point in ordered)
        {
            if (point.Value > peak.Value)
            {
                peak = point;
            }

            var drawdown = peak.Value == 0m ? 0m : point.Value / peak.Value - 1m;

            if (drawdown < worst.Depth)
            {
                worst = new MaxDrawdown(drawdown, peak.Date, point.Date, null);
                worstPeakValue = peak.Value;
            }
        }

        if (worst.Depth == 0m)
        {
            return worst;
        }

        // Il recupero è il primo giorno, dopo il minimo, in cui si torna al massimo da cui era iniziato il calo.
        var recovery = ordered.FindIndex(p => p.Date > worst.TroughDate && p.Value >= worstPeakValue);

        return recovery < 0 ? worst : worst with { RecoveryDate = ordered[recovery].Date };
    }

    /// <summary>
    /// Il peggior calo del portafoglio, calcolato sulla serie TWR degli snapshot.
    /// </summary>
    /// <param name="snapshots">Gli snapshot giornalieri.</param>
    /// <returns>Il drawdown massimo; assente se non ci sono snapshot.</returns>
    public static MaxDrawdown? Max(IEnumerable<WalletSnapshot> snapshots)
        => Max(TimeWeightedReturn.Index(snapshots));
}