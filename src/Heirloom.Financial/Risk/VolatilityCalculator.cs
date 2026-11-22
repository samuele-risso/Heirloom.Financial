using Heirloom.Financial.Domain;
using Heirloom.Financial.Performance;
using Heirloom.Financial.Validation;

namespace Heirloom.Financial.Risk;

/// <summary>
/// Quanti periodi ci sono in un anno, per annualizzare la volatilità.
/// </summary>
/// <remarks>
/// Va scelta in base ai dati: se la serie contiene solo i giorni di borsa si usa 252,
/// se contiene tutti i giorni (anche weekend, come gli snapshot giornalieri) si usa 365.
/// Mischiare le due cose falsa il risultato di circa il 20%.
/// </remarks>
public enum AnnualizationBasis
{
    /// <summary>252 giorni di borsa all'anno.</summary>
    TradingDays252 = 1,

    /// <summary>365 giorni di calendario all'anno.</summary>
    CalendarDays365 = 2,
}

/// <summary>
/// Misura quanto oscilla il valore: la deviazione standard campionaria dei rendimenti (non dei prezzi),
/// annualizzata moltiplicando per la radice dei periodi in un anno.
/// </summary>
/// <remarks>
/// Coincide con la funzione DEV.ST di Excel applicata ai rendimenti giornalieri, moltiplicata per √252 o √365.
/// Lavora in <c>double</c> perché serve la radice quadrata.
/// </remarks>
public static class VolatilityCalculator
{
    /// <summary>
    /// La deviazione standard campionaria dei rendimenti di periodo, senza annualizzare.
    /// </summary>
    /// <param name="values">I valori in ordine di tempo; tutti positivi.</param>
    /// <returns>La volatilità di periodo; assente se i valori sono meno di 3 (servono almeno 2 rendimenti).</returns>
    public static double? PeriodVolatility(IEnumerable<decimal> values)
    {
        var returns = ReturnsCalculator.PeriodReturns(values).Select(r => (double)r).ToList();

        if (returns.Count < 2)
        {
            return null;
        }

        var mean = returns.Average();
        var sumOfSquares = returns.Sum(r => (r - mean) * (r - mean));

        return Math.Sqrt(sumOfSquares / (returns.Count - 1));
    }

    /// <summary>
    /// La volatilità annualizzata di una serie di valori.
    /// </summary>
    /// <param name="values">I valori in ordine di tempo; tutti positivi.</param>
    /// <param name="basis">Quanti periodi ci sono in un anno. Obbligatorio: non c'è un valore predefinito.</param>
    /// <returns>La volatilità annualizzata (0,15 = 15%); assente se i valori sono meno di 3.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="basis"/> non è definito.</exception>
    public static double? Annualized(IEnumerable<decimal> values, AnnualizationBasis basis)
    {
        DomainGuards.Defined(basis);

        var periodVolatility = PeriodVolatility(values);
        return periodVolatility * Math.Sqrt(PeriodsPerYear(basis));
    }

    /// <summary>
    /// La volatilità annualizzata del portafoglio, calcolata sulla serie TWR: acquisti e vendite
    /// non vengono scambiati per oscillazioni del valore.
    /// </summary>
    /// <param name="snapshots">Gli snapshot giornalieri.</param>
    /// <param name="basis">Quanti periodi ci sono in un anno.</param>
    /// <returns>La volatilità annualizzata; assente se gli snapshot utili sono meno di 3.</returns>
    public static double? Annualized(IEnumerable<WalletSnapshot> snapshots, AnnualizationBasis basis)
        => Annualized(TimeWeightedReturn.Index(snapshots).Select(p => p.Value), basis);

    /// <summary>
    /// Il numero di periodi in un anno per la base scelta.
    /// </summary>
    /// <param name="basis">La base.</param>
    /// <returns>252 o 365.</returns>
    public static int PeriodsPerYear(AnnualizationBasis basis) => basis switch
    {
        AnnualizationBasis.TradingDays252 => 252,
        AnnualizationBasis.CalendarDays365 => 365,
        _ => throw new ArgumentOutOfRangeException(nameof(basis), basis, "Base di annualizzazione non supportata."),
    };
}