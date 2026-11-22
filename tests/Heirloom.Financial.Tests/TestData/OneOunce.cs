using Heirloom.Financial.Domain;

namespace Heirloom.Financial.Tests.TestData;

/// <summary>
/// Un prodotto con esattamente un'oncia troy di oro fino, per test con numeri tondi:
/// con la valutazione spot, un pezzo vale esattamente il prezzo spot.
/// </summary>
public static class OneOunce
{
    /// <summary>Moneta d'oro con 1 oz troy di fino (peso lordo come un Krugerrand).</summary>
    public static readonly MetalProduct GoldCoin = MetalProduct.FromFineWeight(
        "Moneta oro 1 oz", Metal.Gold, ProductForm.Coin, new Weight(33.93m), Weight.FromTroyOunces(1m));

    /// <summary>Lingotto d'argento con 10 oz troy di fino.</summary>
    public static readonly MetalProduct SilverBar = MetalProduct.FromFineWeight(
        "Lingotto argento 10 oz", Metal.Silver, ProductForm.Bar, new Weight(311.1m), Weight.FromTroyOunces(10m));

    /// <summary>Un importo in euro.</summary>
    public static Money Eur(decimal amount) => new(amount, Currency.EUR);

    /// <summary>Uno spot dell'oro in euro.</summary>
    public static SpotPrice GoldSpot(decimal pricePerOunce, DateOnly date) => new(Metal.Gold, Eur(pricePerOunce), date);

    /// <summary>Uno spot dell'argento in euro.</summary>
    public static SpotPrice SilverSpot(decimal pricePerOunce, DateOnly date) => new(Metal.Silver, Eur(pricePerOunce), date);
}