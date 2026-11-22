namespace Heirloom.Financial.Constants;

/// <summary>
/// I titoli (frazione di metallo puro) più diffusi per oro e argento.
/// </summary>
/// <remarks>
/// Sono comodità per test, seed del database e suggerimenti nei form, non un elenco chiuso:
/// un prodotto può avere qualsiasi titolo valido tra 0 (escluso) e 1 (incluso).
/// </remarks>
public static class CommonFineness
{
    /// <summary>Oro 999,9‰ (24 carati): lingotti, Maple Leaf, Philharmoniker.</summary>
    public const decimal Gold9999 = 0.9999m;

    /// <summary>Oro 916,7‰ (22 carati): Krugerrand, Sterlina (Sovereign), American Eagle.</summary>
    public const decimal Gold9167 = 0.9167m;

    /// <summary>Oro 900‰: Marengo (20 franchi), 20 lire, molte monete storiche dell'Unione Latina.</summary>
    public const decimal Gold900 = 0.900m;

    /// <summary>Argento 999,9‰: Maple Leaf argento, alcuni lingotti.</summary>
    public const decimal Silver9999 = 0.9999m;

    /// <summary>Argento 999‰: la maggior parte dei lingotti d'argento.</summary>
    public const decimal Silver999 = 0.999m;

    /// <summary>Argento 925‰ (sterling): monete commemorative e argenteria.</summary>
    public const decimal Silver925 = 0.925m;
}