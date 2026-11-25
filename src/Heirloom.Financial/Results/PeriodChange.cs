using Heirloom.Financial.Internal;

namespace Heirloom.Financial.Results;

/// <summary>
/// Quanto sono cambiati valore e guadagno di un wallet da un giorno dello storico a oggi (es. "rispetto a un mese fa").
/// </summary>
/// <param name="From">Il giorno dello storico usato per il confronto.</param>
/// <param name="ValueChange">Valore di oggi − valore di quel giorno.</param>
/// <param name="ValueChangePercent">La stessa variazione come frazione del valore di quel giorno, a 4 decimali; null se era zero.</param>
/// <param name="PnLChange">Guadagno di oggi − guadagno di quel giorno.</param>
/// <param name="PnLChangePercent">
/// La stessa variazione come frazione del guadagno di quel giorno preso in valore assoluto, a 4 decimali; null se era zero.
/// </param>
public sealed record PeriodChange(
    DateOnly From,
    decimal ValueChange,
    decimal? ValueChangePercent,
    decimal PnLChange,
    decimal? PnLChangePercent)
{
    /// <summary>
    /// Confronta oggi con il giorno dello storico più vicino a <paramref name="since"/>, senza andare oltre:
    /// l'ultimo giorno fino a quella data, o il primo dello storico se comincia dopo.
    /// </summary>
    /// <param name="history">Lo storico (<see cref="PerformanceSummary.History"/>).</param>
    /// <param name="since">Il giorno con cui confrontare (es. oggi meno 30 giorni).</param>
    /// <param name="currentValue">Il valore di oggi.</param>
    /// <param name="currentPnL">Il guadagno di oggi.</param>
    /// <returns>La variazione, o null se lo storico è vuoto.</returns>
    /// <exception cref="ArgumentNullException">Lo storico è null.</exception>
    public static PeriodChange? Since(IReadOnlyList<HistoryPoint> history, DateOnly since, decimal currentValue, decimal currentPnL)
    {
        ArgumentNullException.ThrowIfNull(history);

        if (history.Count == 0)
        {
            return null;
        }

        var past = history.Where(p => p.Date <= since).MaxBy(p => p.Date) ?? history.MinBy(p => p.Date)!;

        var valueChange = currentValue - past.Value;
        var pnlChange = currentPnL - past.UnrealizedPnL;

        return new PeriodChange(
            past.Date,
            Rounding.Amount(valueChange),
            past.Value == 0m ? null : Rounding.Percentage(valueChange / past.Value),
            Rounding.Amount(pnlChange),
            past.UnrealizedPnL == 0m ? null : Rounding.Percentage(pnlChange / Math.Abs(past.UnrealizedPnL)));
    }
}