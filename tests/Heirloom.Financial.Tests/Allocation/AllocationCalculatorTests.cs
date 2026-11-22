using Heirloom.Financial.Allocation;
using Heirloom.Financial.Domain;
using Heirloom.Financial.Positions;
using Heirloom.Financial.Tests.TestData;
using Heirloom.Financial.Valuation;

namespace Heirloom.Financial.Tests.Allocation;

public class AllocationCalculatorTests
{
    private static readonly DateOnly Today = new(2026, 11, 20);
    private static readonly SpotPrice Gold = new(Metal.Gold, new Money(2500m, Currency.EUR), Today);
    private static readonly SpotPrice Silver = new(Metal.Silver, new Money(30m, Currency.EUR), Today);

    private static PnLSummary Summary(IEnumerable<Lot> lots)
    {
        var basis = CostBasisCalculator.Calculate(lots, CostBasisMethod.WeightedAverage, Currency.EUR);
        return PnLCalculator.Calculate(basis, [Gold, Silver], SpotValuation.Instance);
    }

    [Fact]
    public void Un_portafoglio_di_sole_monete_d_oro_e_tutto_nella_fetta_oro_monete()
    {
        var slices = AllocationCalculator.Calculate(Summary(TestLots.MultipleBuys), AllocationGrouping.MetalAndForm, AllocationBasis.Value);

        Assert.Equal(4, slices.Count);
        Assert.Equal(1m, slices.Single(s => s is { Metal: Metal.Gold, Form: ProductForm.Coin }).Percent);
        Assert.All(slices.Where(s => s is not { Metal: Metal.Gold, Form: ProductForm.Coin }), s => Assert.Equal(0m, s.Percent));
    }

    [Fact]
    public void Le_fette_sono_sempre_tutte_e_in_ordine_fisso()
    {
        var slices = AllocationCalculator.Calculate(Summary(TestLots.SingleBuy), AllocationGrouping.MetalAndForm, AllocationBasis.Value);

        Assert.Equal(
            new (Metal?, ProductForm?)[]
            {
                (Metal.Gold, ProductForm.Coin), (Metal.Gold, ProductForm.Bar),
                (Metal.Silver, ProductForm.Coin), (Metal.Silver, ProductForm.Bar),
            },
            slices.Select(s => (s.Metal, s.Form)));
    }

    [Fact]
    public void Raggruppando_per_metallo_o_per_forma_ci_sono_due_fette()
    {
        var summary = Summary(TestLots.MixedMultiYear);

        var byMetal = AllocationCalculator.Calculate(summary, AllocationGrouping.Metal, AllocationBasis.Value);
        var byForm = AllocationCalculator.Calculate(summary, AllocationGrouping.Form, AllocationBasis.Value);

        Assert.Equal(new Metal?[] { Metal.Gold, Metal.Silver }, byMetal.Select(s => s.Metal));
        Assert.All(byMetal, s => Assert.Null(s.Form));
        Assert.Equal(new ProductForm?[] { ProductForm.Coin, ProductForm.Bar }, byForm.Select(s => s.Form));
        Assert.All(byForm, s => Assert.Null(s.Metal));
    }

    [Fact]
    public void Un_portafoglio_vuoto_ha_fette_a_zero_e_percentuali_assenti()
    {
        var slices = AllocationCalculator.Calculate(Summary([]), AllocationGrouping.MetalAndForm, AllocationBasis.Value);

        Assert.Equal(4, slices.Count);
        Assert.All(slices, s =>
        {
            Assert.True(s.Value.IsZero);
            Assert.Equal(Weight.Zero, s.FineWeight);
            Assert.Null(s.Percent);
        });
    }

    [Fact]
    public void Sul_valore_domina_l_oro_sul_peso_domina_l_argento()
    {
        // Nel portafoglio misto ci sono circa 1,15 kg d'argento puro e circa 150 g d'oro puro:
        // l'oro vale molto di più, ma l'argento pesa molto di più.
        var summary = Summary(TestLots.MixedMultiYear);

        var byValue = AllocationCalculator.Calculate(summary, AllocationGrouping.Metal, AllocationBasis.Value);
        var byWeight = AllocationCalculator.Calculate(summary, AllocationGrouping.Metal, AllocationBasis.FineWeight);

        Assert.True(byValue[0].Percent > 0.8m);    // oro
        Assert.True(byWeight[1].Percent > 0.8m);   // argento
    }

    [Fact]
    public void Valori_e_pesi_delle_fette_sommano_ai_totali_del_portafoglio()
    {
        var summary = Summary(TestLots.MixedMultiYear);

        var slices = AllocationCalculator.Calculate(summary, AllocationGrouping.MetalAndForm, AllocationBasis.Value);

        Assert.Equal(summary.TotalValue, slices.Aggregate(Money.Zero(Currency.EUR), (sum, s) => sum + s.Value));
        Assert.Equal(
            summary.Positions.Aggregate(Weight.Zero, (sum, p) => sum + p.Position.FineWeight),
            slices.Aggregate(Weight.Zero, (sum, s) => sum + s.FineWeight));
    }

    // ---------- Arrotondamento ----------

    [Fact]
    public void Tre_terzi_arrotondati_fanno_esattamente_100_per_cento()
    {
        var third = 1m / 3m;
        var slices = new[]
        {
            new AllocationSlice(Metal.Gold, ProductForm.Coin, new Money(1m, Currency.EUR), Weight.Zero, third),
            new AllocationSlice(Metal.Gold, ProductForm.Bar, new Money(1m, Currency.EUR), Weight.Zero, third),
            new AllocationSlice(Metal.Silver, ProductForm.Coin, new Money(1m, Currency.EUR), Weight.Zero, third),
            new AllocationSlice(Metal.Silver, ProductForm.Bar, new Money(0m, Currency.EUR), Weight.Zero, 0m),
        };

        var rounded = AllocationCalculator.RoundPercentages(slices);

        Assert.Equal(1m, rounded.Sum(s => s.Percent!.Value));
        Assert.Equal(0.3334m, rounded[0].Percent);   // la prima delle fette più grandi prende lo scarto
        Assert.Equal(0.3333m, rounded[1].Percent);
        Assert.Equal(0.3333m, rounded[2].Percent);
        Assert.Equal(0m, rounded[3].Percent);
    }

    [Fact]
    public void Arrotondare_un_portafoglio_vuoto_non_cambia_nulla()
    {
        var empty = AllocationCalculator.Calculate(Summary([]), AllocationGrouping.Metal, AllocationBasis.Value);

        Assert.Same(empty, AllocationCalculator.RoundPercentages(empty));
    }
}