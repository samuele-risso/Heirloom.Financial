using Heirloom.Financial.Domain;
using Heirloom.Financial.Tests.TestData;
using Heirloom.Financial.Valuation;

namespace Heirloom.Financial.Tests.Valuation;

public class ValuationStrategiesTests
{
    private static readonly SpotPrice Gold = new(Metal.Gold, new Money(2000m, Currency.EUR), new DateOnly(2026, 11, 20));

    private static readonly Dictionary<ProductForm, decimal> Premiums = new()
    {
        [ProductForm.Coin] = 0.04m,
        [ProductForm.Bar] = 0.01m,
    };

    private static readonly Dictionary<ProductForm, decimal> Discounts = new()
    {
        [ProductForm.Coin] = 0.01m,
        [ProductForm.Bar] = 0.02m,
    };

    private static Money Melt(MetalProduct product) => MeltValueCalculator.MeltValue(product, Gold);

    // ---------- Spot ----------

    [Fact]
    public void SpotValuation_restituisce_il_valore_di_fusione()
    {
        IValuationStrategy strategy = SpotValuation.Instance;

        Assert.Equal(Melt(TestProducts.Krugerrand1Oz), strategy.UnitValue(TestProducts.Krugerrand1Oz, Gold));
    }

    [Fact]
    public void Value_moltiplica_il_valore_unitario_per_la_quantita()
    {
        IValuationStrategy strategy = SpotValuation.Instance;

        Assert.Equal(Melt(TestProducts.Marengo) * 3m, strategy.Value(TestProducts.Marengo, 3m, Gold));
    }

    // ---------- Spot più premio ----------

    [Fact]
    public void SpotPlusPremium_applica_il_premio_della_forma()
    {
        IValuationStrategy strategy = new SpotPlusPremiumValuation(Premiums);

        Assert.Equal(Melt(TestProducts.Krugerrand1Oz) * 1.04m, strategy.UnitValue(TestProducts.Krugerrand1Oz, Gold));
        Assert.Equal(Melt(TestProducts.GoldBar100g) * 1.01m, strategy.UnitValue(TestProducts.GoldBar100g, Gold));
    }

    [Fact]
    public void Il_premio_del_singolo_prodotto_vince_su_quello_della_forma()
    {
        var byProduct = new Dictionary<MetalProduct, decimal> { [TestProducts.Sovereign] = 0.07m };
        IValuationStrategy strategy = new SpotPlusPremiumValuation(Premiums, byProduct);

        Assert.Equal(Melt(TestProducts.Sovereign) * 1.07m, strategy.UnitValue(TestProducts.Sovereign, Gold));
        Assert.Equal(Melt(TestProducts.Marengo) * 1.04m, strategy.UnitValue(TestProducts.Marengo, Gold));
    }

    [Fact]
    public void Una_forma_non_configurata_da_un_errore_chiaro()
    {
        var onlyBars = new Dictionary<ProductForm, decimal> { [ProductForm.Bar] = 0.01m };
        IValuationStrategy strategy = new SpotPlusPremiumValuation(onlyBars);

        var ex = Assert.Throws<InvalidOperationException>(() => strategy.UnitValue(TestProducts.Krugerrand1Oz, Gold));

        Assert.Contains("Krugerrand", ex.Message);
    }

    [Fact]
    public void Un_premio_negativo_viene_rifiutato()
    {
        var wrong = new Dictionary<ProductForm, decimal> { [ProductForm.Coin] = -0.01m };

        Assert.Throws<ArgumentOutOfRangeException>(() => new SpotPlusPremiumValuation(wrong));
    }

    // ---------- Riacquisto ----------

    [Fact]
    public void Buyback_applica_lo_sconto_della_forma()
    {
        IValuationStrategy strategy = new BuybackValuation(Discounts);

        Assert.Equal(Melt(TestProducts.GoldBar100g) * 0.98m, strategy.UnitValue(TestProducts.GoldBar100g, Gold));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(1)]
    [InlineData(2)]
    public void Uno_sconto_fuori_intervallo_viene_rifiutato(int discount)
    {
        var wrong = new Dictionary<ProductForm, decimal> { [ProductForm.Bar] = discount };

        Assert.Throws<ArgumentOutOfRangeException>(() => new BuybackValuation(wrong));
    }

    // ---------- Confronto ----------

    [Fact]
    public void Riacquisto_spot_e_premio_sono_sempre_in_ordine_crescente()
    {
        IValuationStrategy buyback = new BuybackValuation(Discounts);
        IValuationStrategy spot = SpotValuation.Instance;
        IValuationStrategy premium = new SpotPlusPremiumValuation(Premiums);

        foreach (var product in TestProducts.All.Where(p => p.Metal == Metal.Gold))
        {
            var b = buyback.UnitValue(product, Gold);
            var s = spot.UnitValue(product, Gold);
            var p = premium.UnitValue(product, Gold);

            Assert.True(b <= s && s <= p, product.Name);
        }
    }
}