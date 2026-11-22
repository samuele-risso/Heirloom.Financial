using System.Globalization;
using Heirloom.Financial.Domain;
using Heirloom.Financial.Tests.TestData;
using Heirloom.Financial.Valuation;

namespace Heirloom.Financial.Tests.Valuation;

public class PremiumCalculatorTests
{
    private static readonly DateOnly Day = new(2026, 11, 20);
    private static Money Eur(decimal amount) => new(amount, Currency.EUR);

    [Theory]
    [InlineData("2100", "2000", "0.05")]
    [InlineData("2000", "2000", "0")]
    [InlineData("1900", "2000", "-0.05")]
    public void Premio_percentuale(string price, string melt, string expected)
    {
        var premium = PremiumCalculator.PremiumPercent(Eur(Parse(price)), Eur(Parse(melt)));

        Assert.Equal(Parse(expected), premium);
    }

    [Fact]
    public void Un_valore_di_fusione_zero_viene_rifiutato_senza_dividere_per_zero()
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(
            () => PremiumCalculator.PremiumPercent(Eur(100m), Eur(0m)));

        Assert.Equal("meltValue", ex.ParamName);
    }

    [Fact]
    public void Prezzo_e_valore_in_valute_diverse_vengono_rifiutati()
    {
        Assert.Throws<ArgumentException>(
            () => PremiumCalculator.PremiumPercent(new Money(2100m, Currency.USD), Eur(2000m)));
    }

    [Fact]
    public void Premio_per_oncia_di_un_lingotto_da_100_g()
    {
        // 3,2148 oz fini a 2.000 €/oz = 6.429,51 € di metallo; pagato 6.600 € → circa 53 € per oncia.
        var spot = new SpotPrice(Metal.Gold, Eur(2000m), Day);

        var perOz = PremiumCalculator.PremiumPerTroyOunce(Eur(6600m), TestProducts.GoldBar100g, spot);

        Assert.Equal(Currency.EUR, perOz.Currency);
        Assert.Equal(53.03m, Math.Round(perOz.Amount, 2));
    }

    [Fact]
    public void Il_premio_effettivo_include_i_costi_accessori()
    {
        // 1 Krugerrand a 1.900 € + 25 € di spedizione = 1.925 €; spot 1.850 €/oz → premio circa 4,05%.
        var spot = new SpotPrice(Metal.Gold, Eur(1850m), new DateOnly(2024, 3, 15));

        var premium = PremiumCalculator.EffectivePremiumPercent(TestLots.SingleBuy[0], spot);

        Assert.Equal(0.0405m, Math.Round(premium, 4));
    }

    [Fact]
    public void Il_premio_effettivo_di_una_vendita_viene_rifiutato()
    {
        var sale = TestLots.WithPartialSale.Single(l => l.Side == LotSide.Sell);
        var spot = new SpotPrice(Metal.Gold, Eur(2500m), sale.Date);

        Assert.Throws<ArgumentException>(() => PremiumCalculator.EffectivePremiumPercent(sale, spot));
    }

    private static decimal Parse(string text) => decimal.Parse(text, CultureInfo.InvariantCulture);
}