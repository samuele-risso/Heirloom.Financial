using Heirloom.Financial.Domain;
using Heirloom.Financial.Positions;
using Heirloom.Financial.Tests.TestData;

namespace Heirloom.Financial.Tests.Positions;

public class HoldingsAggregatorTests
{
    private static Money Eur(decimal amount) => new(amount, Currency.EUR);

    [Fact]
    public void Tre_acquisti_e_una_vendita_danno_una_posizione_con_la_quantita_netta()
    {
        var lots = new[]
        {
            Lot.Buy(TestProducts.Marengo, 2m, Eur(480m), Eur(0m), new DateOnly(2024, 1, 10)),
            Lot.Buy(TestProducts.Marengo, 3m, Eur(500m), Eur(0m), new DateOnly(2024, 2, 10)),
            Lot.Buy(TestProducts.Marengo, 1m, Eur(510m), Eur(0m), new DateOnly(2024, 3, 10)),
            Lot.Sell(TestProducts.Marengo, 4m, Eur(550m), Eur(0m), new DateOnly(2024, 4, 10)),
        };

        var holding = Assert.Single(HoldingsAggregator.Aggregate(lots));

        Assert.Equal(TestProducts.Marengo, holding.Product);
        Assert.Equal(2m, holding.Quantity);
    }

    [Fact]
    public void L_ordine_dei_lotti_non_cambia_il_risultato()
    {
        var ordered = HoldingsAggregator.Aggregate(TestLots.MixedMultiYear);
        var shuffled = HoldingsAggregator.Aggregate(TestLots.MixedMultiYear.Reverse());

        Assert.Equal(ordered, shuffled);
    }

    [Fact]
    public void Un_prodotto_venduto_del_tutto_non_compare()
    {
        var lots = new[]
        {
            Lot.Buy(TestProducts.Marengo, 2m, Eur(480m), Eur(0m), new DateOnly(2024, 1, 10)),
            Lot.Sell(TestProducts.Marengo, 2m, Eur(550m), Eur(0m), new DateOnly(2024, 4, 10)),
        };

        Assert.Empty(HoldingsAggregator.Aggregate(lots));
    }

    [Fact]
    public void Nessun_lotto_nessuna_posizione()
    {
        Assert.Empty(HoldingsAggregator.Aggregate([]));
    }

    [Fact]
    public void Il_portafoglio_misto_ha_cinque_posizioni_ordinate_per_metallo_forma_e_nome()
    {
        var holdings = HoldingsAggregator.Aggregate(TestLots.MixedMultiYear);

        Assert.Equal(
            new[] { "Krugerrand 1 oz", "Marengo 20 franchi", "Lingotto oro 100 g", "Maple Leaf argento 1 oz", "Lingotto argento 1 kg" },
            holdings.Select(h => h.Product.Name));

        Assert.Equal(5m, holdings.Single(h => h.Product == TestProducts.SilverMapleLeaf1Oz).Quantity);
    }

    [Fact]
    public void Il_peso_fino_totale_per_metallo_somma_tutti_i_prodotti()
    {
        var gold = HoldingsAggregator.TotalFineWeight(TestLots.MixedMultiYear, Metal.Gold);

        var expected =
            TestProducts.GoldBar100g.FineWeight +
            TestProducts.Krugerrand1Oz.FineWeight +
            TestProducts.Marengo.FineWeight * 3m;

        Assert.Equal(expected, gold);
    }

    [Fact]
    public void Una_vendita_superiore_al_posseduto_viene_rifiutata()
    {
        var lots = new[]
        {
            Lot.Buy(TestProducts.Marengo, 1m, Eur(480m), Eur(0m), new DateOnly(2024, 1, 10)),
            Lot.Sell(TestProducts.Marengo, 2m, Eur(550m), Eur(0m), new DateOnly(2024, 4, 10)),
        };

        Assert.Throws<ArgumentException>(() => HoldingsAggregator.Aggregate(lots));
    }
}