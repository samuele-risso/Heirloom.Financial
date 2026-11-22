using Heirloom.Financial.Domain;
using Heirloom.Financial.Positions;
using Heirloom.Financial.Tests.TestData;

namespace Heirloom.Financial.Tests.Positions;

public class CostBasisCalculatorTests
{
    private static Money Eur(decimal amount) => new(amount, Currency.EUR);

    /// <summary>L'esempio del backlog: 2 pezzi a 1.000 € e 2 a 1.200 €, poi si vende 1 pezzo.</summary>
    private static readonly Lot[] BacklogExample =
    [
        Lot.Buy(TestProducts.Sovereign, 2m, Eur(1000m), Eur(0m), new DateOnly(2024, 1, 10)),
        Lot.Buy(TestProducts.Sovereign, 2m, Eur(1200m), Eur(0m), new DateOnly(2024, 5, 10)),
        Lot.Sell(TestProducts.Sovereign, 1m, Eur(1300m), Eur(0m), new DateOnly(2024, 9, 10)),
    ];

    // ---------- Media ponderata ----------

    [Fact]
    public void Media_ponderata_prima_della_vendita_il_prezzo_medio_e_1100()
    {
        var result = CostBasisCalculator.Calculate(BacklogExample.Take(2), CostBasisMethod.WeightedAverage, Currency.EUR);

        var position = Assert.Single(result.Positions);
        Assert.Equal(4m, position.Quantity);
        Assert.Equal(Eur(1100m), position.AverageCost);
    }

    [Fact]
    public void Media_ponderata_dopo_la_vendita_restano_3_pezzi_a_1100()
    {
        var result = CostBasisCalculator.Calculate(BacklogExample, CostBasisMethod.WeightedAverage, Currency.EUR);

        var position = Assert.Single(result.Positions);
        Assert.Equal(3m, position.Quantity);
        Assert.Equal(Eur(3300m), position.CostBasis);
        Assert.Equal(Eur(1100m), position.AverageCost);

        var sale = Assert.Single(result.Sales);
        Assert.Equal(Eur(1100m), sale.CostOfSold);
        Assert.Equal(Eur(200m), sale.RealizedPnL);
    }

    [Fact]
    public void I_costi_accessori_aumentano_il_prezzo_medio()
    {
        var result = CostBasisCalculator.Calculate(TestLots.SingleBuy, CostBasisMethod.WeightedAverage, Currency.EUR);

        Assert.Equal(Eur(1925m), Assert.Single(result.Positions).AverageCost);
    }

    // ---------- FIFO ----------

    [Fact]
    public void Fifo_vende_prima_i_pezzi_piu_vecchi()
    {
        var result = CostBasisCalculator.Calculate(BacklogExample, CostBasisMethod.Fifo, Currency.EUR);

        var sale = Assert.Single(result.Sales);
        Assert.Equal(Eur(1000m), sale.CostOfSold);
        Assert.Equal(Eur(300m), sale.RealizedPnL);

        // Restano 1 pezzo a 1.000 € e 2 a 1.200 €.
        var position = Assert.Single(result.Positions);
        Assert.Equal(3m, position.Quantity);
        Assert.Equal(Eur(3400m), position.CostBasis);
    }

    [Fact]
    public void Fifo_gestisce_una_vendita_che_consuma_un_lotto_e_mezzo()
    {
        // Lotti da 2 a 1.000 € e 2 a 1.200 €; vendo 3 → escono 2×1.000 + 1×1.200 = 3.200 €.
        var lots = BacklogExample.Take(2).Append(
            Lot.Sell(TestProducts.Sovereign, 3m, Eur(1300m), Eur(0m), new DateOnly(2024, 9, 10)));

        var result = CostBasisCalculator.Calculate(lots, CostBasisMethod.Fifo, Currency.EUR);

        Assert.Equal(Eur(3200m), Assert.Single(result.Sales).CostOfSold);
        Assert.Equal(Eur(1200m), Assert.Single(result.Positions).CostBasis);
    }

    [Fact]
    public void Senza_vendite_Fifo_e_media_ponderata_danno_lo_stesso_costo_totale()
    {
        var buys = TestLots.MixedMultiYear.Where(l => l.Side == LotSide.Buy).ToList();

        var average = CostBasisCalculator.Calculate(buys, CostBasisMethod.WeightedAverage, Currency.EUR);
        var fifo = CostBasisCalculator.Calculate(buys, CostBasisMethod.Fifo, Currency.EUR);

        Assert.Equal(average.TotalCostBasis, fifo.TotalCostBasis);
    }

    // ---------- Storie di prova ----------

    [Fact]
    public void Le_storie_di_prova_danno_i_valori_attesi()
    {
        var average = CostBasisCalculator.Calculate(TestLots.WithPartialSale, CostBasisMethod.WeightedAverage, Currency.EUR);
        var fifo = CostBasisCalculator.Calculate(TestLots.WithPartialSale, CostBasisMethod.Fifo, Currency.EUR);

        // Venduto 1 Krugerrand a 2.600 € con 10 € di commissioni → incasso 2.590 €.
        Assert.Equal(Eur(390m), average.RealizedPnL);   // costo medio 2.200
        Assert.Equal(Eur(590m), fifo.RealizedPnL);      // costo del primo lotto 2.000
        Assert.Equal(Eur(6600m), average.TotalCostBasis);
        Assert.Equal(Eur(6800m), fifo.TotalCostBasis);
    }

    [Fact]
    public void Nel_portafoglio_misto_la_vendita_d_argento_e_in_lieve_perdita()
    {
        // 10 Maple Leaf a 28 € + 61,60 € di costi = 341,60 €; venduti 5 a 35 € − 5 € = 170 €.
        // Costo dei 5 venduti: 170,80 € → perdita di 0,80 €.
        var result = CostBasisCalculator.Calculate(TestLots.MixedMultiYear, CostBasisMethod.WeightedAverage, Currency.EUR);

        Assert.Equal(Eur(-0.80m), Assert.Single(result.Sales).RealizedPnL);
        Assert.Equal(5, result.Positions.Count);
    }

    // ---------- Validazione ----------

    [Fact]
    public void Lotti_in_un_altra_valuta_vengono_rifiutati()
    {
        var ex = Assert.Throws<ArgumentException>(
            () => CostBasisCalculator.Calculate(TestLots.SingleBuy, CostBasisMethod.Fifo, Currency.USD));

        Assert.Contains("USD", ex.Message);
    }

    [Fact]
    public void Nessun_lotto_nessuna_posizione_e_tutto_a_zero()
    {
        var result = CostBasisCalculator.Calculate([], CostBasisMethod.Fifo, Currency.EUR);

        Assert.Empty(result.Positions);
        Assert.Equal(Eur(0m), result.TotalCostBasis);
        Assert.Equal(Eur(0m), result.RealizedPnL);
    }
}