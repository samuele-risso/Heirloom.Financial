namespace Heirloom.Financial.Constants;

/// <summary>
/// Costanti fisiche di conversione tra unità di misura del peso.
/// </summary>
/// <remarks>
/// Sono l'unico punto della libreria in cui compaiono questi numeri: nessun altro file
/// deve riscriverli, nemmeno approssimati (31,1 al posto di 31,1034768).
/// </remarks>
public static class UnitConstants
{
    /// <summary>
    /// Grammi in un'oncia troy: 31,1034768 g. È l'unità con cui sono quotati oro e argento.
    /// </summary>
    /// <remarks>
    /// Da non confondere con l'oncia "avoirdupois" (28,349523125 g), usata per altri beni:
    /// scambiarle sottostima il valore del metallo di circa il 10%.
    /// </remarks>
    public const decimal GramsPerTroyOunce = 31.1034768m;

    /// <summary>
    /// Grammi in un chilogrammo: 1000 g.
    /// </summary>
    public const decimal GramsPerKilogram = 1000m;
}