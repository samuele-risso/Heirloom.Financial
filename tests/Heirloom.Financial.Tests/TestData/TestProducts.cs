using Heirloom.Financial.Domain;

namespace Heirloom.Financial.Tests.TestData;

/// <summary>
/// Prodotti reali molto diffusi, da usare nei test al posto di numeri astratti.
/// Pesi lordi e titoli sono quelli nominali di conio; il peso fino risultante
/// può differire di qualche decimillesimo dal valore dichiarato dalla zecca.
/// </summary>
public static class TestProducts
{
    // ---------- Oro: monete ----------

    /// <summary>Krugerrand 1 oz: 33,93 g a 22 carati (0,9167) = 1 oz troy di oro fino.</summary>
    public static readonly MetalProduct Krugerrand1Oz =
        new("Krugerrand 1 oz", Metal.Gold, ProductForm.Coin, new Weight(33.93m), 0.9167m);

    /// <summary>Maple Leaf oro 1 oz: 31,11 g a 0,9999.</summary>
    public static readonly MetalProduct GoldMapleLeaf1Oz =
        new("Maple Leaf oro 1 oz", Metal.Gold, ProductForm.Coin, new Weight(31.11m), 0.9999m);

    /// <summary>Marengo (20 franchi): 6,45 g a 0,900.</summary>
    public static readonly MetalProduct Marengo =
        new("Marengo 20 franchi", Metal.Gold, ProductForm.Coin, new Weight(6.45m), 0.900m);

    /// <summary>Sterlina (Sovereign): 7,98 g a 22 carati (0,9167).</summary>
    public static readonly MetalProduct Sovereign =
        new("Sterlina", Metal.Gold, ProductForm.Coin, new Weight(7.98m), 0.9167m);

    // ---------- Oro: lingotti ----------

    /// <summary>Lingotto oro 100 g a 0,9999.</summary>
    public static readonly MetalProduct GoldBar100g =
        new("Lingotto oro 100 g", Metal.Gold, ProductForm.Bar, new Weight(100m), 0.9999m);

    /// <summary>Lingotto oro 1 kg a 0,9999.</summary>
    public static readonly MetalProduct GoldBar1kg =
        new("Lingotto oro 1 kg", Metal.Gold, ProductForm.Bar, new Weight(1000m), 0.9999m);

    // ---------- Argento ----------

    /// <summary>Maple Leaf argento 1 oz: 31,11 g a 0,9999.</summary>
    public static readonly MetalProduct SilverMapleLeaf1Oz =
        new("Maple Leaf argento 1 oz", Metal.Silver, ProductForm.Coin, new Weight(31.11m), 0.9999m);

    /// <summary>Lingotto argento 1 kg a 0,999.</summary>
    public static readonly MetalProduct SilverBar1kg =
        new("Lingotto argento 1 kg", Metal.Silver, ProductForm.Bar, new Weight(1000m), 0.999m);

    /// <summary>Tutti i prodotti di prova, per i test che devono valere su ognuno.</summary>
    public static IReadOnlyList<MetalProduct> All { get; } =
    [
        Krugerrand1Oz, GoldMapleLeaf1Oz, Marengo, Sovereign,
        GoldBar100g, GoldBar1kg,
        SilverMapleLeaf1Oz, SilverBar1kg,
    ];
}