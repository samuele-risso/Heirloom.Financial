using Heirloom.Financial.Validation;

namespace Heirloom.Financial.Domain;

/// <summary>
/// Un tasso di cambio tra due valute in un giorno.
/// </summary>
/// <remarks>
/// <para>
/// <b>Convenzione: 1 <see cref="Base"/> = <see cref="Rate"/> <see cref="Quote"/>.</b>
/// Per esempio EUR/USD = 1,08 significa che 1 euro vale 1,08 dollari:
/// Base = EUR, Quote = USD, Rate = 1,08.
/// </para>
/// <para>
/// Quindi da Base a Quote si <b>moltiplica</b> per il cambio (100 € × 1,08 = 108 $),
/// da Quote a Base si <b>divide</b> (108 $ ÷ 1,08 = 100 €). Invertire questa regola è
/// l'errore più comune con i cambi: la conversione vera e propria vive in un unico punto,
/// CurrencyConverter (HA-015).
/// </para>
/// </remarks>
public sealed record FxRate
{
    /// <summary>
    /// La valuta di base: quella di cui si esprime il valore (1 Base = ...).
    /// </summary>
    public Currency Base { get; }

    /// <summary>
    /// La valuta quotata: quella in cui si esprime il valore della base.
    /// </summary>
    public Currency Quote { get; }

    /// <summary>
    /// Quante unità di <see cref="Quote"/> vale 1 unità di <see cref="Base"/>. Sempre maggiore di zero.
    /// </summary>
    public decimal Rate { get; }

    /// <summary>
    /// Il giorno a cui si riferisce il cambio.
    /// </summary>
    public DateOnly Date { get; }

    /// <summary>
    /// Crea un tasso di cambio.
    /// </summary>
    /// <param name="baseCurrency">La valuta di base (es. EUR).</param>
    /// <param name="quoteCurrency">La valuta quotata (es. USD); diversa dalla base.</param>
    /// <param name="rate">Quante unità di quotata vale 1 unità di base; maggiore di zero.</param>
    /// <param name="date">Il giorno del cambio.</param>
    /// <exception cref="ArgumentOutOfRangeException">Valuta non definita o cambio non positivo.</exception>
    /// <exception cref="ArgumentException">Base e quotata sono la stessa valuta.</exception>
    public FxRate(Currency baseCurrency, Currency quoteCurrency, decimal rate, DateOnly date)
    {
        DomainGuards.Defined(baseCurrency);
        DomainGuards.Defined(quoteCurrency);

        if (baseCurrency == quoteCurrency)
        {
            throw new ArgumentException(
                $"Un cambio deve coinvolgere due valute diverse, non {baseCurrency.ToIsoCode()}/{quoteCurrency.ToIsoCode()}.",
                nameof(quoteCurrency));
        }

        Base = baseCurrency;
        Quote = quoteCurrency;
        Rate = DomainGuards.Positive(rate);
        Date = date;
    }

    /// <summary>
    /// Il cambio nella direzione opposta: da EUR/USD 1,25 a USD/EUR 0,8, stessa data.
    /// </summary>
    /// <returns>Il cambio inverso.</returns>
    public FxRate Inverse() => new(Quote, Base, 1m / Rate, Date);

    /// <summary>
    /// Indica se il cambio riguarda la coppia di valute indicata, in qualsiasi direzione.
    /// </summary>
    /// <param name="first">Una delle due valute.</param>
    /// <param name="second">L'altra valuta.</param>
    /// <returns><c>true</c> se il cambio è tra <paramref name="first"/> e <paramref name="second"/>.</returns>
    public bool Involves(Currency first, Currency second) =>
        (Base == first && Quote == second) || (Base == second && Quote == first);

    /// <summary>
    /// Restituisce il cambio in forma leggibile (es. "EUR/USD 1.08 @ 2026-11-20").
    /// </summary>
    /// <returns>La coppia, il cambio e la data.</returns>
    public override string ToString() =>
        $"{Base.ToIsoCode()}/{Quote.ToIsoCode()} {Rate.ToString(System.Globalization.CultureInfo.InvariantCulture)} @ {Date:yyyy-MM-dd}";
}