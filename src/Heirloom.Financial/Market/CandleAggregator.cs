using Heirloom.Financial.Domain;

namespace Heirloom.Financial.Market;

/// <summary>
/// Unisce più candele consecutive in una sola (es. le 24 candele orarie di un giorno nella candela giornaliera).
/// </summary>
public static class CandleAggregator
{
    /// <summary>
    /// Unisce le candele: apertura della prima, chiusura dell'ultima, massimo dei massimi, minimo dei minimi,
    /// volumi e scambi sommati.
    /// </summary>
    /// <param name="candles">Le candele, in qualsiasi ordine, tutte nella stessa valuta.</param>
    /// <returns>La candela unica.</returns>
    /// <exception cref="ArgumentException">Non ci sono candele, o sono in valute diverse.</exception>
    public static Candle Aggregate(IEnumerable<Candle> candles)
    {
        ArgumentNullException.ThrowIfNull(candles);

        var list = candles.OrderBy(c => c.OpenTime).ToList();

        if (list.Count == 0)
        {
            throw new ArgumentException("Serve almeno una candela da unire.", nameof(candles));
        }

        var currency = list[0].Currency;
        if (list.Any(c => c.Currency != currency))
        {
            throw new ArgumentException("Le candele da unire devono essere tutte nella stessa valuta.", nameof(candles));
        }

        var high = list.Select(c => c.High).Aggregate((max, h) => h > max ? h : max);
        var low = list.Select(c => c.Low).Aggregate((min, l) => l < min ? l : min);

        return new Candle(
            list[0].OpenTime,
            list.Max(c => c.CloseTime),
            list[0].Open,
            high,
            low,
            list[^1].Close,
            list.Sum(c => c.Volume),
            list.Sum(c => c.Trades));
    }
}