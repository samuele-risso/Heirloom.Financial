using Heirloom.Financial.Domain;

namespace Heirloom.Financial.Internal;

/// <summary>
/// L'unica regola di arrotondamento della libreria.
/// </summary>
/// <remarks>
/// <para>
/// Si usa <b>solo</b> quando si costruiscono i risultati da restituire (Results, consegna 7),
/// mai nei calcoli intermedi: arrotondare a metà strada accumula errori.
/// </para>
/// <para>
/// Usa sempre l'arrotondamento commerciale (<see cref="MidpointRounding.AwayFromZero"/>):
/// 2,345 diventa 2,35. Il default di .NET è invece l'arrotondamento bancario
/// (<see cref="MidpointRounding.ToEven"/>), che darebbe 2,34.
/// </para>
/// <para>
/// È <c>internal</c>: chi usa la libreria non lo vede; i test sì, grazie a <c>InternalsVisibleTo</c>.
/// </para>
/// </remarks>
internal static class Rounding
{
    /// <summary>Decimali per gli importi di denaro.</summary>
    public const int AmountDecimals = 2;

    /// <summary>Decimali per i pesi in grammi.</summary>
    public const int WeightDecimals = 4;

    /// <summary>Decimali per le percentuali, espresse come frazione (0,1234 = 12,34%).</summary>
    public const int PercentageDecimals = 4;

    /// <summary>Arrotonda un importo a 2 decimali.</summary>
    /// <param name="amount">L'importo.</param>
    /// <returns>L'importo arrotondato.</returns>
    public static decimal Amount(decimal amount) => Math.Round(amount, AmountDecimals, MidpointRounding.AwayFromZero);

    /// <summary>Arrotonda un importo a 2 decimali, mantenendo la valuta.</summary>
    /// <param name="money">L'importo.</param>
    /// <returns>L'importo arrotondato, nella stessa valuta.</returns>
    public static Money Amount(Money money) => new(Amount(money.Amount), money.Currency);

    /// <summary>Arrotonda un peso a 4 decimali di grammo.</summary>
    /// <param name="weight">Il peso.</param>
    /// <returns>Il peso arrotondato.</returns>
    public static Weight Grams(Weight weight) =>
        new(Math.Round(weight.Grams, WeightDecimals, MidpointRounding.AwayFromZero));

    /// <summary>Arrotonda una percentuale (frazione) a 4 decimali: 0,123456 → 0,1235.</summary>
    /// <param name="fraction">La percentuale come frazione.</param>
    /// <returns>La percentuale arrotondata.</returns>
    public static decimal Percentage(decimal fraction) =>
        Math.Round(fraction, PercentageDecimals, MidpointRounding.AwayFromZero);
}