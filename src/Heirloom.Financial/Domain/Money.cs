using System.Globalization;

namespace Heirloom.Financial.Domain;

/// <summary>
/// Un importo di denaro legato alla sua valuta.
/// </summary>
/// <remarks>
/// <para>
/// È un value object immutabile. La regola principale: <b>non si possono combinare
/// importi in valute diverse</b>. Sommare, sottrarre, confrontare o dividere euro e
/// dollari lancia <see cref="InvalidOperationException"/>: per farlo bisogna prima
/// convertire con un cambio esplicito (CurrencyConverter, HA-015).
/// </para>
/// <para>
/// Un importo può essere negativo (per esempio una perdita). Money non arrotonda mai:
/// l'arrotondamento si fa solo quando si costruiscono i risultati (Rounding, HA-014).
/// </para>
/// <para>
/// Attenzione: <c>default(Money)</c> ha una valuta non valida (0). Crea sempre gli
/// importi con il costruttore o con <see cref="Zero(Currency)"/>.
/// </para>
/// </remarks>
public readonly record struct Money
{
    /// <summary>
    /// L'importo. Può essere negativo.
    /// </summary>
    public decimal Amount { get; }

    /// <summary>
    /// La valuta dell'importo.
    /// </summary>
    public Currency Currency { get; }

    /// <summary>
    /// Crea un importo in una valuta.
    /// </summary>
    /// <param name="amount">L'importo; può essere negativo.</param>
    /// <param name="currency">La valuta; deve essere un valore definito di <see cref="Domain.Currency"/>.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="currency"/> non è una valuta definita.</exception>
    public Money(decimal amount, Currency currency)
    {
        if (!Enum.IsDefined(currency))
        {
            throw new ArgumentOutOfRangeException(nameof(currency), currency, "Valuta non supportata.");
        }

        Amount = amount;
        Currency = currency;
    }

    /// <summary>
    /// L'importo zero in una valuta. Utile come punto di partenza per le somme.
    /// </summary>
    /// <param name="currency">La valuta.</param>
    /// <returns>Zero nella valuta indicata.</returns>
    public static Money Zero(Currency currency) => new(0m, currency);

    /// <summary>
    /// Indica se l'importo è zero.
    /// </summary>
    public bool IsZero => Amount == 0m;

    /// <summary>
    /// Indica se l'importo è negativo (per esempio una perdita).
    /// </summary>
    public bool IsNegative => Amount < 0m;

    /// <summary>Somma due importi nella stessa valuta.</summary>
    /// <param name="left">Il primo importo.</param>
    /// <param name="right">Il secondo importo.</param>
    /// <returns>La somma, nella stessa valuta.</returns>
    /// <exception cref="InvalidOperationException">Le due valute sono diverse.</exception>
    public static Money operator +(Money left, Money right)
    {
        EnsureSameCurrency(left, right, "sommare");
        return new Money(left.Amount + right.Amount, left.Currency);
    }

    /// <summary>Sottrae un importo da un altro nella stessa valuta.</summary>
    /// <param name="left">L'importo da cui sottrarre.</param>
    /// <param name="right">L'importo da sottrarre.</param>
    /// <returns>La differenza, nella stessa valuta. Può essere negativa.</returns>
    /// <exception cref="InvalidOperationException">Le due valute sono diverse.</exception>
    public static Money operator -(Money left, Money right)
    {
        EnsureSameCurrency(left, right, "sottrarre");
        return new Money(left.Amount - right.Amount, left.Currency);
    }

    /// <summary>Cambia il segno di un importo.</summary>
    /// <param name="money">L'importo.</param>
    /// <returns>L'importo con segno opposto, nella stessa valuta.</returns>
    public static Money operator -(Money money) => new(-money.Amount, money.Currency);

    /// <summary>Moltiplica un importo per un numero (es. prezzo unitario per quantità).</summary>
    /// <param name="money">L'importo.</param>
    /// <param name="factor">Il fattore.</param>
    /// <returns>L'importo moltiplicato, nella stessa valuta.</returns>
    public static Money operator *(Money money, decimal factor) => new(money.Amount * factor, money.Currency);

    /// <summary>Moltiplica un numero per un importo.</summary>
    /// <param name="factor">Il fattore.</param>
    /// <param name="money">L'importo.</param>
    /// <returns>L'importo moltiplicato, nella stessa valuta.</returns>
    public static Money operator *(decimal factor, Money money) => money * factor;

    /// <summary>Divide un importo per un numero (es. costo totale diviso quantità).</summary>
    /// <param name="money">L'importo.</param>
    /// <param name="divisor">Il divisore.</param>
    /// <returns>L'importo diviso, nella stessa valuta.</returns>
    /// <exception cref="DivideByZeroException"><paramref name="divisor"/> è zero.</exception>
    public static Money operator /(Money money, decimal divisor) => new(money.Amount / divisor, money.Currency);

    /// <summary>
    /// Divide due importi nella stessa valuta e restituisce il rapporto
    /// (es. guadagno diviso costo = rendimento come frazione).
    /// </summary>
    /// <param name="left">Il dividendo.</param>
    /// <param name="right">Il divisore.</param>
    /// <returns>Il rapporto tra i due importi, senza valuta.</returns>
    /// <exception cref="InvalidOperationException">Le due valute sono diverse.</exception>
    /// <exception cref="DivideByZeroException"><paramref name="right"/> è zero.</exception>
    public static decimal operator /(Money left, Money right)
    {
        EnsureSameCurrency(left, right, "dividere");
        return left.Amount / right.Amount;
    }

    /// <summary>Indica se il primo importo è minore del secondo.</summary>
    /// <param name="left">Il primo importo.</param>
    /// <param name="right">Il secondo importo.</param>
    /// <returns><c>true</c> se <paramref name="left"/> è minore di <paramref name="right"/>.</returns>
    /// <exception cref="InvalidOperationException">Le due valute sono diverse.</exception>
    public static bool operator <(Money left, Money right)
    {
        EnsureSameCurrency(left, right, "confrontare");
        return left.Amount < right.Amount;
    }

    /// <summary>Indica se il primo importo è maggiore del secondo.</summary>
    /// <param name="left">Il primo importo.</param>
    /// <param name="right">Il secondo importo.</param>
    /// <returns><c>true</c> se <paramref name="left"/> è maggiore di <paramref name="right"/>.</returns>
    /// <exception cref="InvalidOperationException">Le due valute sono diverse.</exception>
    public static bool operator >(Money left, Money right)
    {
        EnsureSameCurrency(left, right, "confrontare");
        return left.Amount > right.Amount;
    }

    /// <summary>Indica se il primo importo è minore o uguale al secondo.</summary>
    /// <param name="left">Il primo importo.</param>
    /// <param name="right">Il secondo importo.</param>
    /// <returns><c>true</c> se <paramref name="left"/> è minore o uguale a <paramref name="right"/>.</returns>
    /// <exception cref="InvalidOperationException">Le due valute sono diverse.</exception>
    public static bool operator <=(Money left, Money right)
    {
        EnsureSameCurrency(left, right, "confrontare");
        return left.Amount <= right.Amount;
    }

    /// <summary>Indica se il primo importo è maggiore o uguale al secondo.</summary>
    /// <param name="left">Il primo importo.</param>
    /// <param name="right">Il secondo importo.</param>
    /// <returns><c>true</c> se <paramref name="left"/> è maggiore o uguale a <paramref name="right"/>.</returns>
    /// <exception cref="InvalidOperationException">Le due valute sono diverse.</exception>
    public static bool operator >=(Money left, Money right)
    {
        EnsureSameCurrency(left, right, "confrontare");
        return left.Amount >= right.Amount;
    }

    /// <summary>
    /// Restituisce l'importo in forma leggibile e indipendente dalla lingua del PC,
    /// con il punto come separatore decimale (es. "1234.56 EUR").
    /// </summary>
    /// <returns>L'importo seguito dal codice ISO della valuta.</returns>
    public override string ToString() =>
        $"{Amount.ToString(CultureInfo.InvariantCulture)} {Currency.ToIsoCode()}";

    /// <summary>
    /// Lancia un'eccezione se i due importi non sono nella stessa valuta.
    /// </summary>
    private static void EnsureSameCurrency(Money left, Money right, string operation)
    {
        if (left.Currency != right.Currency)
        {
            throw new InvalidOperationException(
                $"Impossibile {operation} importi in valute diverse: {left.Currency.ToIsoCode()} e {right.Currency.ToIsoCode()}. " +
                "Converti prima con un cambio esplicito.");
        }
    }
}