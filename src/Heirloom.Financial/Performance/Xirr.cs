using Heirloom.Financial.Domain;

namespace Heirloom.Financial.Performance;

/// <summary>
/// Un flusso di cassa dal punto di vista dell'utente: negativo quando paga, positivo quando incassa.
/// </summary>
/// <param name="Date">Il giorno del flusso.</param>
/// <param name="Amount">L'importo; negativo per un acquisto, positivo per una vendita o per il valore finale.</param>
public readonly record struct CashFlow(DateOnly Date, decimal Amount);

/// <summary>
/// L'esito del calcolo dell'XIRR.
/// </summary>
/// <param name="Rate">Il rendimento annuo, come frazione; assente se il calcolo non è riuscito.</param>
/// <param name="Iterations">Le iterazioni usate.</param>
/// <param name="FailureReason">Perché il calcolo non è riuscito; assente se è riuscito.</param>
public sealed record XirrResult(double? Rate, int Iterations, string? FailureReason)
{
    /// <summary>Indica se il calcolo è riuscito.</summary>
    public bool Success => Rate is not null;

    internal static XirrResult Failed(string reason, int iterations = 0) => new(null, iterations, reason);
}

/// <summary>
/// Il rendimento personale dell'utente (money-weighted): tiene conto di quando e quanto ha investito.
/// È la stessa funzione XIRR di Excel.
/// </summary>
/// <remarks>
/// <para>
/// Trova il tasso annuo r per cui la somma dei flussi, ognuno attualizzato alla sua data, fa zero:
/// Σ flusso / (1 + r)^(giorni dal primo flusso / 365) = 0.
/// </para>
/// <para>
/// È l'unico calcolo iterativo della libreria. Prova prima il metodo di Newton (veloce);
/// se non converge, passa alla bisezione (lenta ma sicura). Lavora in <c>double</c>
/// perché servono potenze con esponente non intero.
/// </para>
/// </remarks>
public static class Xirr
{
    /// <summary>Il numero massimo di iterazioni predefinito.</summary>
    public const int DefaultMaxIterations = 200;

    /// <summary>La tolleranza predefinita sul tasso.</summary>
    public const double DefaultTolerance = 1e-12;

    private const double DaysPerYear = 365.0;
    private const double LowestRate = -0.9999999999;

    /// <summary>
    /// Calcola l'XIRR di una serie di flussi.
    /// </summary>
    /// <param name="flows">I flussi, in qualsiasi ordine; servono almeno un flusso negativo e uno positivo.</param>
    /// <param name="maxIterations">Il numero massimo di iterazioni per ciascun metodo.</param>
    /// <param name="tolerance">La precisione richiesta sul tasso.</param>
    /// <returns>Il risultato: il tasso, oppure il motivo per cui non è calcolabile. Non lancia eccezioni per dati non calcolabili.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="flows"/> è null.</exception>
    public static XirrResult Calculate(
        IEnumerable<CashFlow> flows,
        int maxIterations = DefaultMaxIterations,
        double tolerance = DefaultTolerance)
    {
        ArgumentNullException.ThrowIfNull(flows);

        var list = flows.OrderBy(f => f.Date).ToList();

        if (list.Count < 2)
        {
            return XirrResult.Failed("Servono almeno due flussi.");
        }

        if (!list.Any(f => f.Amount < 0m) || !list.Any(f => f.Amount > 0m))
        {
            return XirrResult.Failed("Servono almeno un flusso negativo (investimento) e uno positivo (incasso o valore finale).");
        }

        var origin = list[0].Date;
        var amounts = list.Select(f => (double)f.Amount).ToArray();
        var years = list.Select(f => (f.Date.DayNumber - origin.DayNumber) / DaysPerYear).ToArray();

        var newton = Newton(amounts, years, maxIterations, tolerance);
        return newton.Success ? newton : Bisection(amounts, years, maxIterations, tolerance, newton.Iterations);
    }

    /// <summary>
    /// Ricava i flussi di cassa dalle operazioni e aggiunge il valore attuale come ultimo flusso positivo.
    /// </summary>
    /// <param name="lots">Le operazioni, tutte nella valuta di <paramref name="currentValue"/>.</param>
    /// <param name="currentValue">Il valore attuale del portafoglio.</param>
    /// <param name="valuationDate">La data del valore attuale.</param>
    /// <returns>I flussi, pronti per <see cref="Calculate"/>.</returns>
    /// <exception cref="ArgumentException">Un lotto è in una valuta diversa dal valore attuale.</exception>
    public static IReadOnlyList<CashFlow> CashFlowsFrom(IEnumerable<Lot> lots, Money currentValue, DateOnly valuationDate)
    {
        ArgumentNullException.ThrowIfNull(lots);

        var flows = new List<CashFlow>();

        foreach (var lot in lots)
        {
            if (lot.Currency != currentValue.Currency)
            {
                throw new ArgumentException(
                    $"Tutti i lotti devono essere in {currentValue.Currency.ToIsoCode()}: convertili prima con LotCurrencyConverter.",
                    nameof(lots));
            }

            flows.Add(new CashFlow(lot.Date, lot.CashFlow.Amount));
        }

        flows.Add(new CashFlow(valuationDate, currentValue.Amount));
        return flows;
    }

    private static XirrResult Newton(double[] amounts, double[] years, int maxIterations, double tolerance)
    {
        var rate = 0.1;

        for (var i = 1; i <= maxIterations; i++)
        {
            var (value, derivative) = NpvAndDerivative(rate, amounts, years);

            if (derivative == 0 || double.IsNaN(derivative) || double.IsInfinity(derivative))
            {
                return XirrResult.Failed("Newton: derivata non utilizzabile.", i);
            }

            var next = rate - value / derivative;

            if (double.IsNaN(next) || double.IsInfinity(next) || next <= -1.0)
            {
                return XirrResult.Failed("Newton: il tasso è uscito dall'intervallo valido.", i);
            }

            if (Math.Abs(next - rate) < tolerance)
            {
                return new XirrResult(next, i, null);
            }

            rate = next;
        }

        return XirrResult.Failed("Newton: nessuna convergenza.", maxIterations);
    }

    private static XirrResult Bisection(double[] amounts, double[] years, int maxIterations, double tolerance, int usedIterations)
    {
        var low = LowestRate;
        var high = 1.0;
        var lowValue = Npv(low, amounts, years);

        // Allarga l'intervallo verso l'alto finché la funzione cambia segno.
        while (Math.Sign(Npv(high, amounts, years)) == Math.Sign(lowValue))
        {
            high *= 2;

            if (high > 1e9)
            {
                return XirrResult.Failed("Non esiste un tasso che azzeri i flussi.", usedIterations);
            }
        }

        for (var i = 1; i <= maxIterations; i++)
        {
            var middle = (low + high) / 2;
            var middleValue = Npv(middle, amounts, years);

            if (Math.Abs(high - low) < tolerance || middleValue == 0)
            {
                return new XirrResult(middle, usedIterations + i, null);
            }

            if (Math.Sign(middleValue) == Math.Sign(lowValue))
            {
                low = middle;
                lowValue = middleValue;
            }
            else
            {
                high = middle;
            }
        }

        return new XirrResult((low + high) / 2, usedIterations + maxIterations, null);
    }

    private static double Npv(double rate, double[] amounts, double[] years)
    {
        var total = 0.0;

        for (var i = 0; i < amounts.Length; i++)
        {
            total += amounts[i] / Math.Pow(1 + rate, years[i]);
        }

        return total;
    }

    private static (double Value, double Derivative) NpvAndDerivative(double rate, double[] amounts, double[] years)
    {
        double value = 0, derivative = 0;

        for (var i = 0; i < amounts.Length; i++)
        {
            var discount = Math.Pow(1 + rate, years[i]);
            value += amounts[i] / discount;
            derivative -= years[i] * amounts[i] / (discount * (1 + rate));
        }

        return (value, derivative);
    }
}