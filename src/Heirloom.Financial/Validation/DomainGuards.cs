using System.Runtime.CompilerServices;
using Heirloom.Financial.Domain;

namespace Heirloom.Financial.Validation;

/// <summary>
/// I controlli sui dati in ingresso, riusati da tutta la libreria.
/// </summary>
/// <remarks>
/// <para>
/// Ogni metodo restituisce il valore ricevuto se è valido, oppure lancia un'eccezione che dice
/// quale parametro è sbagliato e perché. Così i calcolatori possono dare per scontato che i dati
/// siano sani, e gli errori si fermano al confine invece di finire in un grafico.
/// </para>
/// <para>
/// Il nome del parametro viene ricavato in automatico dall'espressione passata
/// (<see cref="CallerArgumentExpressionAttribute"/>): scrivendo
/// <c>DomainGuards.Positive(quantity)</c> l'eccezione avrà <c>ParamName == "quantity"</c>.
/// </para>
/// </remarks>
public static class DomainGuards
{
    /// <summary>
    /// Verifica che un valore di un enum sia tra quelli definiti (es. rifiuta <c>(Metal)0</c>).
    /// </summary>
    /// <typeparam name="TEnum">Il tipo dell'enum.</typeparam>
    /// <param name="value">Il valore da controllare.</param>
    /// <param name="paramName">Il nome del parametro; ricavato in automatico.</param>
    /// <returns>Il valore, se valido.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Il valore non è definito nell'enum.</exception>
    public static TEnum Defined<TEnum>(TEnum value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
        where TEnum : struct, Enum
    {
        if (!Enum.IsDefined(value))
        {
            throw new ArgumentOutOfRangeException(paramName, value, $"Valore non supportato per {typeof(TEnum).Name}.");
        }

        return value;
    }

    /// <summary>
    /// Verifica che un numero sia maggiore di zero.
    /// </summary>
    /// <param name="value">Il numero da controllare.</param>
    /// <param name="paramName">Il nome del parametro; ricavato in automatico.</param>
    /// <returns>Il numero, se valido.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Il numero è zero o negativo.</exception>
    public static decimal Positive(decimal value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        if (value <= 0m)
        {
            throw new ArgumentOutOfRangeException(paramName, value, "Il valore deve essere maggiore di zero.");
        }

        return value;
    }

    /// <summary>
    /// Verifica che un importo sia maggiore di zero.
    /// </summary>
    /// <param name="value">L'importo da controllare.</param>
    /// <param name="paramName">Il nome del parametro; ricavato in automatico.</param>
    /// <returns>L'importo, se valido.</returns>
    /// <exception cref="ArgumentOutOfRangeException">L'importo è zero o negativo.</exception>
    public static Money Positive(Money value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        if (value.Amount <= 0m)
        {
            throw new ArgumentOutOfRangeException(paramName, value, "L'importo deve essere maggiore di zero.");
        }

        return value;
    }

    /// <summary>
    /// Verifica che un importo non sia negativo (lo zero è ammesso).
    /// </summary>
    /// <param name="value">L'importo da controllare.</param>
    /// <param name="paramName">Il nome del parametro; ricavato in automatico.</param>
    /// <returns>L'importo, se valido.</returns>
    /// <exception cref="ArgumentOutOfRangeException">L'importo è negativo.</exception>
    public static Money NonNegative(Money value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        if (value.IsNegative)
        {
            throw new ArgumentOutOfRangeException(paramName, value, "L'importo non può essere negativo.");
        }

        return value;
    }

    /// <summary>
    /// Verifica che un peso sia maggiore di zero.
    /// </summary>
    /// <param name="value">Il peso da controllare.</param>
    /// <param name="paramName">Il nome del parametro; ricavato in automatico.</param>
    /// <returns>Il peso, se valido.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Il peso è zero.</exception>
    public static Weight Positive(Weight value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        if (value.Grams <= 0m)
        {
            throw new ArgumentOutOfRangeException(paramName, value, "Il peso deve essere maggiore di zero.");
        }

        return value;
    }

    /// <summary>
    /// Verifica che un titolo sia maggiore di 0 e al massimo 1 (es. 0,9999).
    /// </summary>
    /// <param name="value">Il titolo da controllare.</param>
    /// <param name="paramName">Il nome del parametro; ricavato in automatico.</param>
    /// <returns>Il titolo, se valido.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Il titolo è zero, negativo o maggiore di 1.</exception>
    public static decimal Fineness(decimal value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        if (value <= 0m || value > 1m)
        {
            throw new ArgumentOutOfRangeException(paramName, value, "Il titolo deve essere maggiore di 0 e al massimo 1 (es. 0,9999).");
        }

        return value;
    }

    /// <summary>
    /// Verifica che due importi siano nella stessa valuta.
    /// </summary>
    /// <param name="reference">L'importo di riferimento.</param>
    /// <param name="value">L'importo da controllare.</param>
    /// <param name="paramName">Il nome del parametro controllato; ricavato in automatico da <paramref name="value"/>.</param>
    /// <returns>L'importo controllato, se valido.</returns>
    /// <exception cref="ArgumentException">Le due valute sono diverse; il messaggio le nomina entrambe.</exception>
    public static Money SameCurrency(Money reference, Money value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        if (value.Currency != reference.Currency)
        {
            throw new ArgumentException(
                $"Gli importi devono essere nella stessa valuta: {reference.Currency.ToIsoCode()} e {value.Currency.ToIsoCode()}.",
                paramName);
        }

        return value;
    }

    /// <summary>
    /// Verifica che una data non sia nel futuro rispetto a oggi.
    /// </summary>
    /// <param name="value">La data da controllare.</param>
    /// <param name="today">La data di oggi. È un parametro, e non <c>DateTime.Today</c>,
    /// così i calcoli restano ripetibili e testabili.</param>
    /// <param name="paramName">Il nome del parametro; ricavato in automatico.</param>
    /// <returns>La data, se valida.</returns>
    /// <exception cref="ArgumentOutOfRangeException">La data è successiva a <paramref name="today"/>.</exception>
    public static DateOnly NotInFuture(DateOnly value, DateOnly today, [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        if (value > today)
        {
            throw new ArgumentOutOfRangeException(paramName, value, $"La data {value:yyyy-MM-dd} è nel futuro (oggi è {today:yyyy-MM-dd}).");
        }

        return value;
    }

    /// <summary>
    /// Verifica che, scorrendo i lotti in ordine di data, non si venda mai più di quanto si possiede
    /// di ciascun prodotto. A parità di data gli acquisti vengono contati prima delle vendite.
    /// </summary>
    /// <param name="lots">I lotti da controllare, in qualsiasi ordine.</param>
    /// <param name="paramName">Il nome del parametro; ricavato in automatico.</param>
    /// <returns>I lotti, se validi.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="lots"/> è null.</exception>
    /// <exception cref="ArgumentException">Una vendita supera la quantità posseduta; il messaggio nomina prodotto e data.</exception>
    public static IReadOnlyList<Lot> NoOversell(IEnumerable<Lot> lots, [CallerArgumentExpression(nameof(lots))] string? paramName = null)
    {
        ArgumentNullException.ThrowIfNull(lots, paramName);

        var list = lots.ToList();
        var held = new Dictionary<MetalProduct, decimal>();

        var ordered = list
            .OrderBy(l => l.Date)
            .ThenBy(l => l.Side == LotSide.Buy ? 0 : 1);

        foreach (var lot in ordered)
        {
            held.TryGetValue(lot.Product, out var quantity);

            if (lot.Side == LotSide.Buy)
            {
                held[lot.Product] = quantity + lot.Quantity;
                continue;
            }

            if (lot.Quantity > quantity)
            {
                throw new ArgumentException(
                    $"Vendita di {lot.Quantity} pezzi di '{lot.Product.Name}' il {lot.Date:yyyy-MM-dd}, " +
                    $"ma in quella data se ne possiedono solo {quantity}.",
                    paramName);
            }

            held[lot.Product] = quantity - lot.Quantity;
        }

        return list;
    }
}