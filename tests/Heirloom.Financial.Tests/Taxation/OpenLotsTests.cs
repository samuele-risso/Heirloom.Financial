using Heirloom.Financial.Domain;
using Heirloom.Financial.Positions;
using static Heirloom.Financial.Tests.TestData.OneOunce;

namespace Heirloom.Financial.Tests.Positions;

/// <summary>
/// I lotti aperti: gli acquisti di cui restano dei pezzi, con il costo di quelli rimasti.
/// </summary>
public class OpenLotsTests
{
    private static readonly DateOnly Day = new(2024, 1, 10);

    private static readonly Lot BuyA =
        Lot.BuyForTotal(GoldCoin, 3m, Eur(6000m), Eur(0m), Day) with { Id = "A", HasDocumentedCost = true };

    private static readonly Lot BuyB =
        Lot.Buy(GoldCoin, 2m, Eur(2100m), Eur(0m), Day.AddDays(30)) with { Id = "B" };

    private static readonly Lot Inherited =
        Lot.BuyWithUnknownCost(GoldCoin, 1m, Day.AddDays(60), Currency.EUR) with { Id = "H" };

    private static CostBasisResult Calculate(CostBasisMethod method, params Lot[] lots) =>
        CostBasisCalculator.Calculate(lots, method, Currency.EUR);

    [Fact]
    public void Lotto_specifico_resta_il_costo_dei_pezzi_rimasti()
    {
        var sale = Lot.Sell(GoldCoin, 1m, Eur(2200m), Eur(0m), Day.AddDays(90)) with { MatchedLotId = "A" };

        var open = Calculate(CostBasisMethod.SpecificLot, BuyA, BuyB, sale).OpenLots;

        Assert.Equal(2, open.Count);
        Assert.Equal(BuyA, open[0].Purchase);
        Assert.Equal(2m, open[0].Quantity);
        Assert.Equal(Eur(4000m), open[0].Cost);
        Assert.True(open[0].HasDocumentedCost);
        Assert.Equal(BuyB, open[1].Purchase);
        Assert.Equal(Eur(4200m), open[1].Cost);
        Assert.False(open[1].HasDocumentedCost);
    }

    [Fact]
    public void Fifo_un_lotto_venduto_del_tutto_sparisce()
    {
        var sale = Lot.Sell(GoldCoin, 3m, Eur(6600m), Eur(0m), Day.AddDays(90));

        var open = Assert.Single(Calculate(CostBasisMethod.Fifo, BuyA, BuyB, sale).OpenLots);

        Assert.Equal("B", open.Purchase.Id);
        Assert.Equal(2m, open.Quantity);
    }

    [Fact]
    public void I_lotti_a_costo_sconosciuto_non_hanno_costo()
    {
        var open = Calculate(CostBasisMethod.Fifo, BuyA, Inherited).OpenLots;

        var inherited = Assert.Single(open, o => o.Purchase.Id == "H");
        Assert.Null(inherited.Cost);
        Assert.False(inherited.HasKnownCost);
        Assert.Equal(GoldCoin.FineWeight, inherited.FineWeight);
        Assert.Equal(GoldCoin.GrossWeight, inherited.GrossWeight);
    }

    [Fact]
    public void I_lotti_aperti_sono_in_ordine_di_data()
    {
        var open = Calculate(CostBasisMethod.Fifo, Inherited, BuyB, BuyA).OpenLots;

        Assert.Equal(new[] { "A", "B", "H" }, open.Select(o => o.Purchase.Id));
    }

    [Fact]
    public void Con_la_media_ponderata_non_ci_sono_lotti_aperti()
    {
        Assert.Empty(Calculate(CostBasisMethod.WeightedAverage, BuyA, BuyB).OpenLots);
    }

    [Fact]
    public void La_somma_dei_lotti_aperti_coincide_con_le_posizioni()
    {
        var sale = Lot.Sell(GoldCoin, 2m, Eur(4400m), Eur(0m), Day.AddDays(90));
        var result = Calculate(CostBasisMethod.Fifo, BuyA, BuyB, Inherited, sale);

        var position = Assert.Single(result.Positions);
        Assert.Equal(position.Quantity, result.OpenLots.Sum(o => o.Quantity));
        Assert.Equal(position.CostBasis, result.OpenLots.Where(o => o.Cost is not null).Aggregate(Eur(0m), (s, o) => s + o.Cost!.Value));
    }

    // ---------- Costo documentato sul lotto ----------

    [Fact]
    public void Il_costo_documentato_e_falso_per_default()
    {
        Assert.False(Lot.Buy(GoldCoin, 1m, Eur(2000m), Eur(0m), Day).HasDocumentedCost);
    }

    [Fact]
    public void Una_vendita_non_puo_avere_il_costo_documentato()
    {
        var sale = Lot.Sell(GoldCoin, 1m, Eur(2000m), Eur(0m), Day);

        var ex = Assert.Throws<ArgumentException>(() => sale with { HasDocumentedCost = true });
        Assert.Equal("HasDocumentedCost", ex.ParamName);
    }

    [Fact]
    public void Il_costo_documentato_sopravvive_alla_conversione_di_valuta()
    {
        var rates = new Heirloom.Financial.Exchange.FxRateSeries([new FxRate(Currency.EUR, Currency.USD, 1.1m, Day)]);

        var converted = Heirloom.Financial.Exchange.LotCurrencyConverter.Convert(BuyA, Currency.USD, rates);

        Assert.True(converted.HasDocumentedCost);
    }
}