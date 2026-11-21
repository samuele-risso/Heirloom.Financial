using Heirloom.Financial.Domain;

namespace Heirloom.Financial.Tests.TestData;

/// <summary>
/// Storie di portafoglio pronte da riusare nei test.
/// I prezzi sono esempi plausibili scelti per avere conti semplici, non quotazioni storiche.
/// </summary>
public static class TestLots
{
    private static Money Eur(decimal amount) => new(amount, Currency.EUR);

    private static readonly Money NoFees = Money.Zero(Currency.EUR);

    /// <summary>
    /// Un solo acquisto: 1 Krugerrand a 1.900 € più 25 € di spedizione.
    /// Costo totale 1.925 €.
    /// </summary>
    public static IReadOnlyList<Lot> SingleBuy { get; } =
    [
        Lot.Buy(TestProducts.Krugerrand1Oz, 1m, Eur(1900m), Eur(25m), new DateOnly(2024, 3, 15)),
    ];

    /// <summary>
    /// Due acquisti dello stesso prodotto a prezzi diversi, senza costi accessori:
    /// 2 Krugerrand a 2.000 € e 2 a 2.400 €. Prezzo medio ponderato 2.200 €.
    /// </summary>
    public static IReadOnlyList<Lot> MultipleBuys { get; } =
    [
        Lot.Buy(TestProducts.Krugerrand1Oz, 2m, Eur(2000m), NoFees, new DateOnly(2024, 1, 10)),
        Lot.Buy(TestProducts.Krugerrand1Oz, 2m, Eur(2400m), NoFees, new DateOnly(2024, 9, 10)),
    ];

    /// <summary>
    /// Gli acquisti di <see cref="MultipleBuys"/> più la vendita di 1 Krugerrand
    /// a 2.600 € con 10 € di commissioni. Restano 3 pezzi.
    /// </summary>
    public static IReadOnlyList<Lot> WithPartialSale { get; } =
    [
        .. MultipleBuys,
        Lot.Sell(TestProducts.Krugerrand1Oz, 1m, Eur(2600m), Eur(10m), new DateOnly(2025, 3, 1)),
    ];

    /// <summary>
    /// Portafoglio misto oro/argento, monete e lingotti, dal 2022 al 2025,
    /// con una vendita parziale di argento.
    /// </summary>
    public static IReadOnlyList<Lot> MixedMultiYear { get; } =
    [
        Lot.Buy(TestProducts.GoldBar100g, 1m, Eur(5300m), Eur(30m), new DateOnly(2022, 5, 20)),
        Lot.Buy(TestProducts.SilverBar1kg, 1m, Eur(750m), Eur(165m), new DateOnly(2022, 11, 3)),
        Lot.Buy(TestProducts.SilverMapleLeaf1Oz, 10m, Eur(28m), Eur(61.60m), new DateOnly(2023, 6, 12)),
        Lot.Buy(TestProducts.Krugerrand1Oz, 1m, Eur(2200m), Eur(20m), new DateOnly(2024, 2, 8)),
        Lot.Buy(TestProducts.Marengo, 3m, Eur(480m), Eur(15m), new DateOnly(2024, 10, 1)),
        Lot.Sell(TestProducts.SilverMapleLeaf1Oz, 5m, Eur(35m), Eur(5m), new DateOnly(2025, 4, 15)),
    ];
}