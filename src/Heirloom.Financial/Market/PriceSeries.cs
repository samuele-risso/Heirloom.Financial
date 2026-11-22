namespace Heirloom.Financial.Market;

/// <summary>
/// Strumenti per lavorare con serie di prezzi che hanno date diverse (giorni senza quotazione, festivi, buchi nei dati).
/// </summary>
/// <remarks>
/// Regola unica: un giorno senza valore prende <b>l'ultimo valore noto</b> (forward fill). Non si interpola mai:
/// inventare un prezzo a metà tra due giorni userebbe un dato che quel giorno non si conosceva ancora.
/// </remarks>
public static class PriceSeries
{
    /// <summary>
    /// Riempie i giorni mancanti tra due date con l'ultimo valore noto.
    /// </summary>
    /// <param name="series">La serie, in qualsiasi ordine, senza date ripetute.</param>
    /// <param name="from">Il primo giorno.</param>
    /// <param name="to">L'ultimo giorno.</param>
    /// <returns>Un valore per ogni giorno; i giorni prima del primo valore noto sono esclusi.</returns>
    /// <exception cref="ArgumentException">Date ripetute nella serie, o date invertite.</exception>
    public static IReadOnlyList<DatedValue> ForwardFill(IEnumerable<DatedValue> series, DateOnly from, DateOnly to)
    {
        if (to < from)
        {
            throw new ArgumentException($"La data finale {to:yyyy-MM-dd} precede quella iniziale {from:yyyy-MM-dd}.", nameof(to));
        }

        var ordered = Sorted(series);
        var result = new List<DatedValue>();
        var next = 0;
        DatedValue? last = null;

        for (var day = from; day <= to; day = day.AddDays(1))
        {
            while (next < ordered.Count && ordered[next].Date <= day)
            {
                last = ordered[next++];
            }

            if (last is { } known)
            {
                result.Add(new DatedValue(day, known.Value));
            }
        }

        return result;
    }

    /// <summary>
    /// Allinea due serie sulle stesse date: l'unione delle date di entrambe, a partire dal primo giorno
    /// in cui esistono tutte e due, con l'ultimo valore noto dove una delle due manca.
    /// </summary>
    /// <param name="first">La prima serie.</param>
    /// <param name="second">La seconda serie.</param>
    /// <returns>I valori allineati, in ordine di data; vuoto se una delle due serie è vuota.</returns>
    /// <exception cref="ArgumentException">Date ripetute in una delle serie.</exception>
    public static IReadOnlyList<AlignedValue> Align(IEnumerable<DatedValue> first, IEnumerable<DatedValue> second)
    {
        var a = Sorted(first);
        var b = Sorted(second);

        if (a.Count == 0 || b.Count == 0)
        {
            return [];
        }

        var start = a[0].Date > b[0].Date ? a[0].Date : b[0].Date;
        var dates = a.Select(x => x.Date).Union(b.Select(x => x.Date)).Where(d => d >= start).Order();

        var result = new List<AlignedValue>();
        int i = 0, j = 0;
        decimal lastA = 0m, lastB = 0m;

        foreach (var date in dates)
        {
            while (i < a.Count && a[i].Date <= date)
            {
                lastA = a[i++].Value;
            }

            while (j < b.Count && b[j].Date <= date)
            {
                lastB = b[j++].Value;
            }

            result.Add(new AlignedValue(date, lastA, lastB));
        }

        return result;
    }

    private static List<DatedValue> Sorted(IEnumerable<DatedValue> series)
    {
        ArgumentNullException.ThrowIfNull(series);

        var list = series.OrderBy(v => v.Date).ToList();

        for (var i = 1; i < list.Count; i++)
        {
            if (list[i].Date == list[i - 1].Date)
            {
                throw new ArgumentException($"Data ripetuta nella serie: {list[i].Date:yyyy-MM-dd}.", nameof(series));
            }
        }

        return list;
    }
}