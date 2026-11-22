namespace Heirloom.Financial.Domain;

/// <summary>
/// La fotografia del portafoglio in un giorno: quanto valeva, quanto era stato investito
/// fino a quel giorno e quanto metallo puro conteneva.
/// </summary>
/// <remarks>
/// Una serie di snapshot giornalieri è la materia prima di tutte le statistiche nel tempo:
/// rendimenti, volatilità, drawdown. Valore e costo investito devono essere nella stessa valuta.
/// </remarks>
public sealed record WalletSnapshot
{
    /// <summary>
    /// Il giorno della fotografia.
    /// </summary>
    public DateOnly Date { get; }

    /// <summary>
    /// Il valore del portafoglio quel giorno.
    /// </summary>
    public Money Value { get; }

    /// <summary>
    /// Il capitale investito netto fino a quel giorno: acquisti meno incassi delle vendite.
    /// Le sue variazioni tra un giorno e l'altro sono i flussi di cassa dell'utente.
    /// </summary>
    public Money InvestedCapital { get; }

    /// <summary>
    /// Il peso d'oro puro posseduto quel giorno.
    /// </summary>
    public Weight GoldFineWeight { get; }

    /// <summary>
    /// Il peso d'argento puro posseduto quel giorno.
    /// </summary>
    public Weight SilverFineWeight { get; }

    /// <summary>
    /// La valuta della fotografia.
    /// </summary>
    public Currency Currency => Value.Currency;

    /// <summary>
    /// Il guadagno o la perdita sulla carta: valore meno capitale investito.
    /// </summary>
    public Money UnrealizedPnL => Value - InvestedCapital;

    /// <summary>
    /// Crea una fotografia del portafoglio.
    /// </summary>
    /// <param name="date">Il giorno.</param>
    /// <param name="value">Il valore; non negativo.</param>
    /// <param name="investedCapital">Il capitale investito netto; nella stessa valuta del valore.</param>
    /// <param name="goldFineWeight">Il peso d'oro puro.</param>
    /// <param name="silverFineWeight">Il peso d'argento puro.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="value"/> è negativo.</exception>
    /// <exception cref="ArgumentException">Valore e capitale investito sono in valute diverse.</exception>
    public WalletSnapshot(DateOnly date, Money value, Money investedCapital, Weight goldFineWeight, Weight silverFineWeight)
    {
        if (value.IsNegative)
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, "Il valore del portafoglio non può essere negativo.");
        }

        if (investedCapital.Currency != value.Currency)
        {
            throw new ArgumentException(
                $"Valore e capitale investito devono essere nella stessa valuta: {value.Currency.ToIsoCode()} e {investedCapital.Currency.ToIsoCode()}.",
                nameof(investedCapital));
        }

        Date = date;
        Value = value;
        InvestedCapital = investedCapital;
        GoldFineWeight = goldFineWeight;
        SilverFineWeight = silverFineWeight;
    }
}