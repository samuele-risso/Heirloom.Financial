using Heirloom.Financial.Domain;

namespace Heirloom.Financial.Valuation;

/// <summary>
/// Un modo di dare un valore a un prodotto in metallo prezioso.
/// </summary>
/// <remarks>
/// <para>
/// Non esiste un unico modo giusto di valutare l'oro fisico, quindi la scelta è intercambiabile:
/// </para>
/// <list type="bullet">
/// <item><see cref="SpotValuation"/>: solo il valore del metallo (prudente).</item>
/// <item><see cref="SpotPlusPremiumValuation"/>: metallo più un premio stimato (vicino al prezzo di mercato).</item>
/// <item><see cref="BuybackValuation"/>: quanto pagherebbe un commerciante (quanto l'utente incasserebbe davvero).</item>
/// </list>
/// <para>
/// Il valore è sempre nella valuta dello spot: le conversioni si fanno a parte, con un cambio esplicito.
/// </para>
/// </remarks>
public interface IValuationStrategy
{
    /// <summary>
    /// Il valore di un singolo pezzo.
    /// </summary>
    /// <param name="product">Il prodotto da valutare.</param>
    /// <param name="spot">Il prezzo spot dello stesso metallo.</param>
    /// <returns>Il valore di un pezzo, nella valuta dello spot.</returns>
    Money UnitValue(MetalProduct product, SpotPrice spot);
}

/// <summary>
/// Metodi di comodo per tutte le strategie di valutazione.
/// </summary>
public static class ValuationStrategyExtensions
{
    /// <summary>
    /// Il valore di più pezzi dello stesso prodotto.
    /// </summary>
    /// <param name="strategy">La strategia.</param>
    /// <param name="product">Il prodotto.</param>
    /// <param name="quantity">Il numero di pezzi; non negativo.</param>
    /// <param name="spot">Il prezzo spot dello stesso metallo.</param>
    /// <returns>Il valore totale, nella valuta dello spot.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="quantity"/> è negativa.</exception>
    public static Money Value(this IValuationStrategy strategy, MetalProduct product, decimal quantity, SpotPrice spot)
    {
        ArgumentNullException.ThrowIfNull(strategy);

        if (quantity < 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), quantity, "La quantità non può essere negativa.");
        }

        return strategy.UnitValue(product, spot) * quantity;
    }
}