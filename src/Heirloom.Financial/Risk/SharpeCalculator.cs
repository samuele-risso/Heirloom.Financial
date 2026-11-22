namespace Heirloom.Financial.Risk;

/// <summary>
/// L'indice di Sharpe: quanto rendimento in più rispetto a un investimento senza rischio
/// si è ottenuto per ogni unità di volatilità.
/// </summary>
/// <remarks>
/// Per i metalli conta meno che per le azioni (l'oro non paga interessi), ma è utile per
/// confrontarli con altri investimenti. Il tasso privo di rischio è sempre un parametro:
/// la libreria non contiene tassi di mercato.
/// </remarks>
public static class SharpeCalculator
{
    /// <summary>
    /// Calcola l'indice di Sharpe: (rendimento annuo − tasso privo di rischio) / volatilità annua.
    /// </summary>
    /// <param name="annualizedReturn">Il rendimento annualizzato, come frazione (0,08 = 8%).</param>
    /// <param name="riskFreeRate">Il tasso privo di rischio annuo, come frazione (es. il rendimento dei titoli di Stato a breve).</param>
    /// <param name="annualizedVolatility">La volatilità annualizzata, come frazione; non negativa.</param>
    /// <returns>L'indice di Sharpe; assente se la volatilità è zero (il rapporto non è definito).</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="annualizedVolatility"/> è negativa.</exception>
    public static double? Calculate(double annualizedReturn, double riskFreeRate, double annualizedVolatility)
    {
        if (annualizedVolatility < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(annualizedVolatility), annualizedVolatility, "La volatilità non può essere negativa.");
        }

        if (annualizedVolatility == 0)
        {
            return null;
        }

        return (annualizedReturn - riskFreeRate) / annualizedVolatility;
    }
}