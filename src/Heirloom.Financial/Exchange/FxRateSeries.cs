using Heirloom.Financial.Domain;

namespace Heirloom.Financial.Exchange;

/// <summary>
/// La serie storica dei cambi di una coppia di valute (es. EUR/USD), con la ricerca per data.
/// </summary>
/// <remarks>
/// <para>
/// <b>Regola per i giorni senza cambio</b> (weekend, festivi della BCE): si usa l'ultimo cambio
/// disponibile <b>prima</b> di quella data. Mai uno successivo: sarebbe un dato "dal futuro".
/// </para>
/// <para>
/// La ricerca è binaria, quindi resta veloce anche con molti anni di dati.
/// </para>
/// </remarks>
public sealed class FxRateSeries
{
    private readonly FxRate[] _rates;
    private readonly DateOnly[] _dates;

    /// <summary>
    /// La valuta di base della coppia.
    /// </summary>
    public Currency Base { get; }

    /// <summary>
    /// La valuta quotata della coppia.
    /// </summary>
    public Currency Quote { get; }

    /// <summary>
    /// Il numero di cambi nella serie.
    /// </summary>
    public int Count => _rates.Length;

    /// <summary>
    /// La data del primo cambio disponibile.
    /// </summary>
    public DateOnly FirstDate => _dates[0];

    /// <summary>
    /// La data dell'ultimo cambio disponibile.
    /// </summary>
    public DateOnly LastDate => _dates[^1];

    /// <summary>
    /// Crea la serie a partire da un elenco di cambi, in qualsiasi ordine.
    /// </summary>
    /// <param name="rates">I cambi; tutti della stessa coppia, almeno uno, nessuna data ripetuta.</param>
    /// <exception cref="ArgumentNullException"><paramref name="rates"/> è null.</exception>
    /// <exception cref="ArgumentException">Elenco vuoto, coppie diverse o date ripetute.</exception>
    public FxRateSeries(IEnumerable<FxRate> rates)
    {
        ArgumentNullException.ThrowIfNull(rates);

        _rates = rates.OrderBy(r => r.Date).ToArray();

        if (_rates.Length == 0)
        {
            throw new ArgumentException("La serie dei cambi deve contenere almeno un cambio.", nameof(rates));
        }

        Base = _rates[0].Base;
        Quote = _rates[0].Quote;

        for (var i = 0; i < _rates.Length; i++)
        {
            var rate = _rates[i];

            if (rate.Base != Base || rate.Quote != Quote)
            {
                throw new ArgumentException(
                    $"Tutti i cambi devono essere {Base.ToIsoCode()}/{Quote.ToIsoCode()}: trovato {rate}.",
                    nameof(rates));
            }

            if (i > 0 && rate.Date == _rates[i - 1].Date)
            {
                throw new ArgumentException($"Data ripetuta nella serie dei cambi: {rate.Date:yyyy-MM-dd}.", nameof(rates));
            }
        }

        _dates = _rates.Select(r => r.Date).ToArray();
    }

    /// <summary>
    /// Restituisce il cambio valido in una data: quello del giorno stesso, oppure l'ultimo precedente.
    /// </summary>
    /// <param name="date">La data richiesta.</param>
    /// <returns>Il cambio da usare per quella data.</returns>
    /// <exception cref="InvalidOperationException">La data è precedente al primo cambio della serie.</exception>
    public FxRate GetRate(DateOnly date)
    {
        var index = Array.BinarySearch(_dates, date);

        if (index >= 0)
        {
            return _rates[index];
        }

        // Nessun cambio esatto: BinarySearch restituisce il complemento della posizione
        // in cui la data andrebbe inserita. Il cambio da usare è quello subito prima.
        var previous = ~index - 1;

        if (previous < 0)
        {
            throw new InvalidOperationException(
                $"Nessun cambio {Base.ToIsoCode()}/{Quote.ToIsoCode()} disponibile per il {date:yyyy-MM-dd}: " +
                $"la serie inizia il {FirstDate:yyyy-MM-dd}.");
        }

        return _rates[previous];
    }

    /// <summary>
    /// Converte un importo nella valuta richiesta, con il cambio valido nella data indicata.
    /// </summary>
    /// <param name="amount">L'importo da convertire.</param>
    /// <param name="target">La valuta di arrivo.</param>
    /// <param name="date">La data di cui usare il cambio (es. la data di un acquisto).</param>
    /// <returns>L'importo convertito.</returns>
    /// <exception cref="InvalidOperationException">Nessun cambio disponibile per quella data.</exception>
    public Money Convert(Money amount, Currency target, DateOnly date)
    {
        if (amount.Currency == target)
        {
            return amount;
        }

        return CurrencyConverter.Convert(amount, target, GetRate(date));
    }
}