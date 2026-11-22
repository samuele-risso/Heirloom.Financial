using Heirloom.Financial.Validation;

namespace Heirloom.Financial.Market;

/// <summary>
/// Il valore di un indicatore in una data; assente nei primi giorni, quando non ci sono ancora abbastanza dati.
/// </summary>
/// <param name="Date">Il giorno.</param>
/// <param name="Value">Il valore dell'indicatore; assente durante il periodo di riscaldamento.</param>
public readonly record struct IndicatorPoint(DateOnly Date, decimal? Value);

/// <summary>
/// Gli indicatori tecnici usati dai grafici delle candele di Heirloom.
/// </summary>
/// <remarks>
/// <para>
/// Per ora contiene solo le medie mobili, che sono semplici e verificabili a mano: implementarle qui evita
/// una dipendenza esterna. Se un giorno serviranno indicatori più complessi (RSI, Bollinger, MACD),
/// si potrà aggiungere la libreria Stock Indicators <b>solo in questo file</b>: il resto del codice
/// continuerà a usare questi metodi e non saprà nulla della libreria esterna.
/// </para>
/// <para>
/// Tutti i calcoli sono in <c>decimal</c> e usano i prezzi di chiusura.
/// </para>
/// </remarks>
public static class Indicators
{
    /// <summary>
    /// La media mobile semplice (SMA): la media delle ultime N chiusure.
    /// </summary>
    /// <param name="closes">Le chiusure, in qualsiasi ordine, senza date ripetute.</param>
    /// <param name="periods">Il numero di periodi della media; maggiore di zero.</param>
    /// <returns>Un punto per ogni chiusura; i primi N−1 senza valore.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="periods"/> non è positivo.</exception>
    public static IReadOnlyList<IndicatorPoint> Sma(IEnumerable<DatedValue> closes, int periods)
    {
        DomainGuards.Positive(periods);

        var ordered = Sorted(closes);
        var result = new List<IndicatorPoint>(ordered.Count);
        var windowSum = 0m;

        for (var i = 0; i < ordered.Count; i++)
        {
            windowSum += ordered[i].Value;

            if (i >= periods)
            {
                windowSum -= ordered[i - periods].Value;
            }

            result.Add(new IndicatorPoint(ordered[i].Date, i >= periods - 1 ? windowSum / periods : null));
        }

        return result;
    }

    /// <summary>
    /// La media mobile esponenziale (EMA): come la SMA, ma dà più peso alle chiusure recenti.
    /// Il primo valore è la SMA delle prime N chiusure; poi EMA = chiusura × k + EMA precedente × (1 − k),
    /// con k = 2 / (N + 1).
    /// </summary>
    /// <param name="closes">Le chiusure, in qualsiasi ordine, senza date ripetute.</param>
    /// <param name="periods">Il numero di periodi; maggiore di zero.</param>
    /// <returns>Un punto per ogni chiusura; i primi N−1 senza valore.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="periods"/> non è positivo.</exception>
    public static IReadOnlyList<IndicatorPoint> Ema(IEnumerable<DatedValue> closes, int periods)
    {
        DomainGuards.Positive(periods);

        var ordered = Sorted(closes);
        var result = new List<IndicatorPoint>(ordered.Count);
        var k = 2m / (periods + 1);
        decimal? ema = null;
        var seedSum = 0m;

        for (var i = 0; i < ordered.Count; i++)
        {
            var close = ordered[i].Value;

            if (i < periods - 1)
            {
                seedSum += close;
            }
            else if (i == periods - 1)
            {
                ema = (seedSum + close) / periods;
            }
            else
            {
                ema = close * k + ema!.Value * (1m - k);
            }

            result.Add(new IndicatorPoint(ordered[i].Date, ema));
        }

        return result;
    }

    private static List<DatedValue> Sorted(IEnumerable<DatedValue> closes)
    {
        ArgumentNullException.ThrowIfNull(closes);

        var list = closes.OrderBy(c => c.Date).ToList();

        for (var i = 1; i < list.Count; i++)
        {
            if (list[i].Date == list[i - 1].Date)
            {
                throw new ArgumentException($"Data ripetuta nella serie: {list[i].Date:yyyy-MM-dd}.", nameof(closes));
            }
        }

        return list;
    }
}