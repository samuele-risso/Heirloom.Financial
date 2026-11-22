using Heirloom.Financial.Domain;
using Heirloom.Financial.Exchange;
using Heirloom.Financial.Validation;

namespace Heirloom.Financial.Market;

/// <summary>
/// Una candela di mercato: prezzo di apertura, massimo, minimo e chiusura in un intervallo di tempo
/// (un'ora, un giorno), per un'oncia troy di metallo puro.
/// </summary>
public sealed record Candle
{
    /// <summary>L'inizio dell'intervallo.</summary>
    public DateTime OpenTime { get; }

    /// <summary>La fine dell'intervallo.</summary>
    public DateTime CloseTime { get; }

    /// <summary>Il prezzo di apertura.</summary>
    public Money Open { get; }

    /// <summary>Il prezzo massimo.</summary>
    public Money High { get; }

    /// <summary>Il prezzo minimo.</summary>
    public Money Low { get; }

    /// <summary>Il prezzo di chiusura.</summary>
    public Money Close { get; }

    /// <summary>Il volume scambiato (nell'unità della fonte, es. once).</summary>
    public decimal Volume { get; }

    /// <summary>Il numero di scambi; zero se la fonte non lo fornisce.</summary>
    public decimal Trades { get; }

    /// <summary>La valuta dei prezzi.</summary>
    public Currency Currency => Open.Currency;

    /// <summary>Il giorno della candela (quello dell'apertura): è il giorno del cambio usato per convertirla.</summary>
    public DateOnly Day => DateOnly.FromDateTime(OpenTime);

    /// <summary>Crea una candela.</summary>
    /// <param name="openTime">L'inizio dell'intervallo.</param>
    /// <param name="closeTime">La fine dell'intervallo; non prima dell'inizio.</param>
    /// <param name="open">Il prezzo di apertura; non negativo.</param>
    /// <param name="high">Il massimo; non minore del minimo.</param>
    /// <param name="low">Il minimo; non negativo.</param>
    /// <param name="close">Il prezzo di chiusura; non negativo.</param>
    /// <param name="volume">Il volume; non negativo.</param>
    /// <param name="trades">Il numero di scambi; non negativo.</param>
    /// <exception cref="ArgumentOutOfRangeException">Un prezzo o il volume è negativo, il massimo è sotto il minimo, o la fine precede l'inizio.</exception>
    /// <exception cref="ArgumentException">I prezzi sono in valute diverse.</exception>
    public Candle(DateTime openTime, DateTime closeTime, Money open, Money high, Money low, Money close, decimal volume, decimal trades = 0m)
    {
        if (closeTime < openTime)
        {
            throw new ArgumentOutOfRangeException(nameof(closeTime), closeTime, "La fine della candela non può precedere l'inizio.");
        }

        DomainGuards.NonNegative(open);
        DomainGuards.NonNegative(low);
        DomainGuards.NonNegative(close);
        DomainGuards.SameCurrency(open, high);
        DomainGuards.SameCurrency(open, low);
        DomainGuards.SameCurrency(open, close);

        if (high < low)
        {
            throw new ArgumentOutOfRangeException(nameof(high), high, $"Il massimo ({high}) non può essere sotto il minimo ({low}).");
        }

        if (volume < 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(volume), volume, "Il volume non può essere negativo.");
        }

        if (trades < 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(trades), trades, "Il numero di scambi non può essere negativo.");
        }

        OpenTime = openTime;
        CloseTime = closeTime;
        Open = open;
        High = high;
        Low = low;
        Close = close;
        Volume = volume;
        Trades = trades;
    }

    /// <summary>
    /// La stessa candela in un'altra valuta: i quattro prezzi si convertono con il cambio del giorno della candela.
    /// </summary>
    /// <param name="target">La valuta di arrivo.</param>
    /// <param name="rates">La serie dei cambi.</param>
    /// <returns>La candela convertita (identica se è già nella valuta di arrivo).</returns>
    /// <exception cref="InvalidOperationException">Non c'è un cambio fino al giorno della candela.</exception>
    public Candle ConvertTo(Currency target, FxRateSeries rates)
    {
        ArgumentNullException.ThrowIfNull(rates);

        if (Currency == target)
        {
            return this;
        }

        Money Convert(Money price) => rates.Convert(price, target, Day);

        return new Candle(OpenTime, CloseTime, Convert(Open), Convert(High), Convert(Low), Convert(Close), Volume, Trades);
    }

    /// <summary>Lo spot del giorno della candela per un metallo: il prezzo di chiusura.</summary>
    /// <param name="metal">Il metallo della candela.</param>
    /// <returns>Lo spot.</returns>
    public SpotPrice ToSpot(Metal metal) => new(metal, Close, Day);
}