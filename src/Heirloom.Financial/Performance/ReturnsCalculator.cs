namespace Heirloom.Financial.Performance;

/// <summary>
/// Le funzioni di base sui rendimenti, riusate dagli altri calcolatori di performance.
/// </summary>
/// <remarks>
/// <para>
/// I rendimenti sono sempre <b>frazioni</b>: 0,05 = +5%, −0,10 = −10%.
/// </para>
/// <para>
/// Regola fondamentale: <b>i rendimenti si compongono, non si sommano</b>.
/// +10% seguito da −10% non fa 0%, ma (1,10 × 0,90) − 1 = −1%.
/// </para>
/// <para>
/// I rendimenti semplici restano in <c>decimal</c>; i logaritmi e le potenze con esponente
/// non intero (CAGR) usano <c>double</c>, perché <c>decimal</c> non li supporta.
/// </para>
/// </remarks>
public static class ReturnsCalculator
{
    /// <summary>
    /// Il rendimento semplice tra due valori: fine / inizio − 1.
    /// </summary>
    /// <param name="start">Il valore iniziale; maggiore di zero.</param>
    /// <param name="end">Il valore finale; non negativo.</param>
    /// <returns>Il rendimento come frazione.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Il valore iniziale non è positivo o quello finale è negativo.</exception>
    public static decimal SimpleReturn(decimal start, decimal end)
    {
        ValidateValues(start, end);
        return end / start - 1m;
    }

    /// <summary>
    /// Il rendimento logaritmico tra due valori: ln(fine / inizio).
    /// A differenza dei semplici, i rendimenti logaritmici si possono sommare nel tempo.
    /// </summary>
    /// <param name="start">Il valore iniziale; maggiore di zero.</param>
    /// <param name="end">Il valore finale; maggiore di zero.</param>
    /// <returns>Il rendimento logaritmico.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Uno dei valori non è positivo.</exception>
    public static double LogReturn(decimal start, decimal end)
    {
        ValidateValues(start, end);

        if (end == 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(end), end, "Il logaritmo di zero non esiste: il valore finale deve essere positivo.");
        }

        return Math.Log((double)(end / start));
    }

    /// <summary>
    /// I rendimenti semplici di ogni periodo di una serie di valori (es. valori giornalieri).
    /// </summary>
    /// <param name="values">I valori in ordine di tempo; tutti positivi tranne eventualmente l'ultimo.</param>
    /// <returns>Un rendimento per ogni coppia di valori consecutivi; vuoto se i valori sono meno di 2.</returns>
    public static IReadOnlyList<decimal> PeriodReturns(IEnumerable<decimal> values)
    {
        ArgumentNullException.ThrowIfNull(values);

        var list = values.ToList();
        var returns = new List<decimal>(Math.Max(0, list.Count - 1));

        for (var i = 1; i < list.Count; i++)
        {
            returns.Add(SimpleReturn(list[i - 1], list[i]));
        }

        return returns;
    }

    /// <summary>
    /// Il rendimento cumulato di più periodi, composto: (1 + r1) × (1 + r2) × … − 1.
    /// </summary>
    /// <param name="periodReturns">I rendimenti di ogni periodo, come frazioni.</param>
    /// <returns>Il rendimento cumulato; zero se non ci sono periodi.</returns>
    public static decimal Cumulative(IEnumerable<decimal> periodReturns)
    {
        ArgumentNullException.ThrowIfNull(periodReturns);
        return periodReturns.Aggregate(1m, (growth, r) => growth * (1m + r)) - 1m;
    }

    /// <summary>
    /// Il rendimento cumulato di una serie di valori: ultimo / primo − 1.
    /// </summary>
    /// <param name="values">I valori in ordine di tempo.</param>
    /// <returns>Il rendimento cumulato; assente se i valori sono meno di 2.</returns>
    public static decimal? CumulativeFromValues(IEnumerable<decimal> values)
    {
        ArgumentNullException.ThrowIfNull(values);

        var list = values.ToList();
        return list.Count < 2 ? null : SimpleReturn(list[0], list[^1]);
    }

    /// <summary>
    /// Il rendimento annualizzato (CAGR): il rendimento annuo costante che avrebbe dato lo stesso risultato.
    /// Per esempio +20% in 3 anni corrisponde a circa +6,27% all'anno.
    /// </summary>
    /// <param name="totalReturn">Il rendimento totale del periodo, come frazione; maggiore di −1.</param>
    /// <param name="years">La durata del periodo in anni; maggiore di zero.</param>
    /// <returns>Il rendimento annualizzato, come frazione.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Rendimento non superiore a −1 o durata non positiva.</exception>
    public static decimal Annualized(decimal totalReturn, double years)
    {
        if (totalReturn <= -1m)
        {
            throw new ArgumentOutOfRangeException(nameof(totalReturn), totalReturn, "Con una perdita del 100% il rendimento annualizzato non è definito.");
        }

        if (years <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(years), years, "La durata deve essere maggiore di zero.");
        }

        return (decimal)(Math.Pow((double)(1m + totalReturn), 1.0 / years) - 1.0);
    }

    /// <summary>
    /// Il rendimento annualizzato tra due date, contando 365,25 giorni per anno.
    /// </summary>
    /// <param name="totalReturn">Il rendimento totale del periodo, come frazione.</param>
    /// <param name="start">La data di inizio.</param>
    /// <param name="end">La data di fine; successiva all'inizio.</param>
    /// <returns>Il rendimento annualizzato, come frazione.</returns>
    public static decimal Annualized(decimal totalReturn, DateOnly start, DateOnly end)
        => Annualized(totalReturn, (end.DayNumber - start.DayNumber) / 365.25);

    private static void ValidateValues(decimal start, decimal end)
    {
        if (start <= 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(start), start, "Il valore iniziale deve essere maggiore di zero.");
        }

        if (end < 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(end), end, "Il valore finale non può essere negativo.");
        }
    }
}