using Heirloom.Financial.Domain;
using Heirloom.Financial.Positions;
using Heirloom.Financial.Tests.TestData;
using Heirloom.Financial.Validation;

namespace Heirloom.Financial.Tests.Domain;

/// <summary>
/// I casi al confine (valori uguali, valute diverse, valori non definiti) dei tipi di base.
/// </summary>
public class DomainBoundaryTests
{
    private static Money Eur(decimal amount) => new(amount, Currency.EUR);
    private static Money Usd(decimal amount) => new(amount, Currency.USD);

    // ---------- Money: confronti ----------

    [Fact]
    public void Importi_uguali_non_sono_ne_minori_ne_maggiori()
    {
        Assert.False(Eur(10m) < Eur(10m));
        Assert.False(Eur(10m) > Eur(10m));
        Assert.True(Eur(10m) <= Eur(10m));
        Assert.True(Eur(10m) >= Eur(10m));
    }

    [Fact]
    public void Confrontare_valute_diverse_viene_rifiutato_con_ogni_operatore()
    {
        Assert.Throws<InvalidOperationException>(() => Eur(1m) < Usd(2m));
        Assert.Throws<InvalidOperationException>(() => Eur(1m) > Usd(2m));
        Assert.Throws<InvalidOperationException>(() => Eur(1m) <= Usd(2m));
        Assert.Throws<InvalidOperationException>(() => Eur(1m) >= Usd(2m));
    }

    // ---------- Weight: confronti ----------

    [Fact]
    public void Pesi_uguali_non_sono_ne_minori_ne_maggiori()
    {
        Assert.False(new Weight(5m) < new Weight(5m));
        Assert.False(new Weight(5m) > new Weight(5m));
        Assert.True(new Weight(5m) <= new Weight(5m));
        Assert.True(new Weight(5m) >= new Weight(5m));
    }

    [Fact]
    public void Il_peso_si_scrive_in_grammi()
    {
        Assert.Equal("31.1035 g", new Weight(31.1035m).ToString());
    }

    // ---------- Lot ----------

    [Fact]
    public void Costi_negativi_su_un_totale_vengono_rifiutati()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Lot.BuyForTotal(TestProducts.Sovereign, 1m, Eur(500m), Eur(-1m), new DateOnly(2024, 1, 1)));
    }

    // ---------- Valori non definiti ----------

    [Fact]
    public void Metallo_non_definito_nel_peso_fino_totale_viene_rifiutato()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => HoldingsAggregator.TotalFineWeight(TestLots.SingleBuy, (Metal)0));
    }

    [Fact]
    public void Metodo_o_valuta_non_definiti_nel_costo_di_carico_vengono_rifiutati()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            CostBasisCalculator.Calculate(TestLots.SingleBuy, (CostBasisMethod)0, Currency.EUR));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            CostBasisCalculator.Calculate([], CostBasisMethod.Fifo, (Currency)0));
    }

    // ---------- DomainGuards.NoOversell ----------

    [Fact]
    public void Due_vendite_che_insieme_superano_il_posseduto_vengono_rifiutate()
    {
        var day = new DateOnly(2024, 1, 1);
        var lots = new[]
        {
            Lot.Buy(TestProducts.Sovereign, 3m, Eur(500m), Eur(0m), day),
            Lot.Sell(TestProducts.Sovereign, 2m, Eur(600m), Eur(0m), day.AddDays(1)),
            Lot.Sell(TestProducts.Sovereign, 2m, Eur(600m), Eur(0m), day.AddDays(2)),
        };

        Assert.Throws<ArgumentException>(() => DomainGuards.NoOversell(lots));
    }

    [Fact]
    public void Due_vendite_che_insieme_esauriscono_il_posseduto_sono_valide()
    {
        var day = new DateOnly(2024, 1, 1);
        var lots = new[]
        {
            Lot.Buy(TestProducts.Sovereign, 3m, Eur(500m), Eur(0m), day),
            Lot.Sell(TestProducts.Sovereign, 2m, Eur(600m), Eur(0m), day.AddDays(1)),
            Lot.Sell(TestProducts.Sovereign, 1m, Eur(600m), Eur(0m), day.AddDays(2)),
        };

        Assert.Equal(3, DomainGuards.NoOversell(lots).Count);
    }
}