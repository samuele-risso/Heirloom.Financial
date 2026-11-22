using Heirloom.Financial.Domain;
using Heirloom.Financial.Positions;
using Heirloom.Financial.Tests.TestData;

namespace Heirloom.Financial.Tests.Positions;

public class SpecificLotTests
{
    private static Money Eur(decimal amount) => new(amount, Currency.EUR);

    private static readonly Lot BuyA =
        Lot.Buy(TestProducts.Sovereign, 2m, Eur(1000m), Eur(0m), new DateOnly(2024, 1, 10)) with { Id = "A" };

    private static readonly Lot BuyB =
        Lot.Buy(TestProducts.Sovereign, 2m, Eur(1200m), Eur(0m), new DateOnly(2024, 5, 10)) with { Id = "B" };

    private static Lot SellFrom(string lotId, decimal quantity, DateOnly? date = null, MetalProduct? product = null) =>
        Lot.Sell(product ?? TestProducts.Sovereign, quantity, Eur(1300m), Eur(0m), date ?? new DateOnly(2024, 9, 10))
            with
        { MatchedLotId = lotId };

    private static CostBasisResult Calculate(params Lot[] lots) =>
        CostBasisCalculator.Calculate(lots, CostBasisMethod.SpecificLot, Currency.EUR);

    [Fact]
    public void La_vendita_prende_il_costo_del_lotto_indicato()
    {
        var result = Calculate(BuyA, BuyB, SellFrom("B", 1m));

        var sale = Assert.Single(result.Sales);
        Assert.Equal(Eur(1200m), sale.CostOfSold);
        Assert.Equal(Eur(100m), sale.RealizedPnL);
        Assert.True(sale.HasKnownCost);
    }

    [Fact]
    public void Dopo_la_vendita_restano_i_pezzi_degli_altri_lotti()
    {
        var result = Calculate(BuyA, BuyB, SellFrom("B", 1m));

        var position = Assert.Single(result.Positions);
        Assert.Equal(3m, position.Quantity);
        Assert.Equal(Eur(3200m), position.CostBasis);
    }

    [Fact]
    public void Con_fifo_la_stessa_vendita_avrebbe_preso_il_lotto_piu_vecchio()
    {
        var lots = new[] { BuyA, BuyB, SellFrom("B", 1m) };

        var fifo = CostBasisCalculator.Calculate(lots, CostBasisMethod.Fifo, Currency.EUR);

        Assert.Equal(Eur(1000m), Assert.Single(fifo.Sales).CostOfSold);
    }

    [Fact]
    public void Vendere_tutto_un_lotto_lo_chiude()
    {
        var result = Calculate(BuyA, BuyB, SellFrom("A", 2m));

        var position = Assert.Single(result.Positions);
        Assert.Equal(2m, position.Quantity);
        Assert.Equal(Eur(2400m), position.CostBasis);
    }

    [Fact]
    public void Un_lotto_gia_venduto_del_tutto_non_si_puo_rivendere()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            Calculate(BuyA, BuyB, SellFrom("A", 2m), SellFrom("A", 1m, new DateOnly(2024, 10, 1))));

        Assert.Contains("'A'", ex.Message);
    }

    [Fact]
    public void Vendere_dal_lotto_piu_pezzi_di_quanti_ne_ha_viene_rifiutato()
    {
        // In tutto se ne possiedono 4, ma il lotto A ne ha solo 2.
        var ex = Assert.Throws<ArgumentException>(() => Calculate(BuyA, BuyB, SellFrom("A", 3m)));

        Assert.Contains("ne ha solo 2", ex.Message);
    }

    [Fact]
    public void Un_lotto_che_non_esiste_viene_rifiutato()
    {
        Assert.Throws<ArgumentException>(() => Calculate(BuyA, BuyB, SellFrom("Z", 1m)));
    }

    [Fact]
    public void Un_lotto_di_un_altro_prodotto_viene_rifiutato()
    {
        var marengo = Lot.Buy(TestProducts.Marengo, 5m, Eur(500m), Eur(0m), new DateOnly(2024, 1, 10)) with { Id = "M" };

        Assert.Throws<ArgumentException>(() => Calculate(BuyA, marengo, SellFrom("M", 1m)));
    }

    [Fact]
    public void Un_lotto_comprato_dopo_la_vendita_viene_rifiutato()
    {
        // A basta a coprire la vendita, ma la vendita indica B, che arriva dopo.
        Assert.Throws<ArgumentException>(() => Calculate(BuyA, BuyB, SellFrom("B", 1m, new DateOnly(2024, 2, 1))));
    }

    [Fact]
    public void Un_acquisto_senza_id_viene_rifiutato()
    {
        var withoutId = BuyA with { Id = null };

        var ex = Assert.Throws<ArgumentException>(() => Calculate(withoutId, BuyB));
        Assert.Contains("Id", ex.Message);
    }

    [Fact]
    public void Due_acquisti_con_lo_stesso_id_vengono_rifiutati()
    {
        Assert.Throws<ArgumentException>(() => Calculate(BuyA, BuyB with { Id = "A" }));
    }

    [Fact]
    public void Una_vendita_senza_lotto_d_origine_viene_rifiutata()
    {
        var sale = Lot.Sell(TestProducts.Sovereign, 1m, Eur(1300m), Eur(0m), new DateOnly(2024, 9, 10));

        var ex = Assert.Throws<ArgumentException>(() => Calculate(BuyA, BuyB, sale));
        Assert.Contains("MatchedLotId", ex.Message);
    }

    [Fact]
    public void Gli_altri_metodi_ignorano_il_lotto_d_origine()
    {
        var withLink = new[] { BuyA, BuyB, SellFrom("B", 1m) };
        var withoutLink = new[] { BuyA, BuyB, SellFrom("B", 1m) with { MatchedLotId = null } };

        foreach (var method in new[] { CostBasisMethod.WeightedAverage, CostBasisMethod.Fifo })
        {
            var a = CostBasisCalculator.Calculate(withLink, method, Currency.EUR);
            var b = CostBasisCalculator.Calculate(withoutLink, method, Currency.EUR);

            Assert.Equal(b.Positions, a.Positions);
            Assert.Equal(b.RealizedPnL, a.RealizedPnL);
        }
    }

    [Fact]
    public void Il_risultato_riporta_il_metodo()
    {
        Assert.Equal(CostBasisMethod.SpecificLot, Calculate(BuyA).Method);
    }

    [Fact]
    public void I_costi_accessori_del_lotto_entrano_nel_costo_venduto()
    {
        var buy = Lot.BuyForTotal(TestProducts.Sovereign, 2m, Eur(2000m), Eur(40m), new DateOnly(2024, 1, 10)) with { Id = "F" };

        var result = Calculate(buy, SellFrom("F", 1m));

        Assert.Equal(Eur(1020m), Assert.Single(result.Sales).CostOfSold);
        Assert.Equal(Eur(1020m), Assert.Single(result.Positions).CostBasis);
    }
}