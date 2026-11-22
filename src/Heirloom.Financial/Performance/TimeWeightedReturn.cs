using Heirloom.Financial.Domain;

namespace Heirloom.Financial.Performance;

/// <summary>
/// Un punto di una serie indicizzata: il valore di 1 € investito all'inizio, giorno per giorno.
/// </summary>
/// <param name="Date">Il giorno.</param>
/// <param name="Value">Quanto vale oggi 1 investito all'inizio (1,05 = +5%).</param>
public readonly record struct IndexPoint(DateOnly Date, decimal Value);

/// <summary>
/// Il rendimento ponderato per il tempo (TWR): misura l'andamento degli <b>asset</b>,
/// neutralizzando quando e quanto l'utente ha comprato o venduto.
/// </summary>
/// <remarks>
/// <para>
/// Serve per confrontare il portafoglio con il prezzo dell'oro: un grosso acquisto non deve sembrare un guadagno.
/// Per il rendimento <b>personale</b> dell'utente, che invece tiene conto di quando ha investito, si usa <see cref="Xirr"/>.
/// </para>
/// <para>
/// I flussi di cassa si ricavano dagli snapshot: la variazione del capitale investito tra due giorni.
/// Si considera che un flusso avvenga a fine giornata, quindi sia già compreso nel valore di quel giorno.
/// Il rendimento del giorno è: (valore − flusso) / valore del giorno prima − 1.
/// </para>
/// </remarks>
public static class TimeWeightedReturn
{
    /// <summary>
    /// Il TWR dell'intero periodo coperto dagli snapshot.
    /// </summary>
    /// <param name="snapshots">Gli snapshot giornalieri, in qualsiasi ordine, tutti nella stessa valuta.</param>
    /// <returns>Il rendimento come frazione; assente se gli snapshot sono meno di 2.</returns>
    /// <exception cref="ArgumentException">Snapshot in valute diverse o con date ripetute.</exception>
    public static decimal? Calculate(IEnumerable<WalletSnapshot> snapshots)
    {
        var index = Index(snapshots);
        return index.Count < 2 ? null : index[^1].Value - 1m;
    }

    /// <summary>
    /// La serie indicizzata del TWR: parte da 1 e mostra, giorno per giorno, quanto vale 1 investito all'inizio.
    /// È la serie giusta per i grafici di andamento e per il drawdown.
    /// </summary>
    /// <param name="snapshots">Gli snapshot giornalieri, in qualsiasi ordine, tutti nella stessa valuta.</param>
    /// <returns>Un punto per ogni snapshot; vuota se non ci sono snapshot.</returns>
    /// <exception cref="ArgumentException">Snapshot in valute diverse o con date ripetute.</exception>
    public static IReadOnlyList<IndexPoint> Index(IEnumerable<WalletSnapshot> snapshots)
    {
        var list = Sorted(snapshots);
        var points = new List<IndexPoint>(list.Count);

        if (list.Count == 0)
        {
            return points;
        }

        var index = 1m;
        points.Add(new IndexPoint(list[0].Date, index));

        for (var i = 1; i < list.Count; i++)
        {
            var previous = list[i - 1];
            var current = list[i];
            var flow = current.InvestedCapital - previous.InvestedCapital;

            // Se il giorno prima il portafoglio era vuoto, non c'era nulla che potesse rendere:
            // il periodo non conta (rendimento zero) e si riparte dal valore dopo il primo acquisto.
            if (!previous.Value.IsZero)
            {
                var periodReturn = (current.Value - flow) / previous.Value - 1m;
                index *= 1m + periodReturn;
            }

            points.Add(new IndexPoint(current.Date, index));
        }

        return points;
    }

    internal static List<WalletSnapshot> Sorted(IEnumerable<WalletSnapshot> snapshots)
    {
        ArgumentNullException.ThrowIfNull(snapshots);

        var list = snapshots.OrderBy(s => s.Date).ToList();

        if (list.Count > 0 && list.Any(s => s.Currency != list[0].Currency))
        {
            throw new ArgumentException("Tutti gli snapshot devono essere nella stessa valuta.", nameof(snapshots));
        }

        for (var i = 1; i < list.Count; i++)
        {
            if (list[i].Date == list[i - 1].Date)
            {
                throw new ArgumentException($"Snapshot ripetuto per il {list[i].Date:yyyy-MM-dd}.", nameof(snapshots));
            }
        }

        return list;
    }
}