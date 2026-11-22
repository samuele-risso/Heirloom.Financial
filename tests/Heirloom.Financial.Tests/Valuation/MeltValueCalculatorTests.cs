using Heirloom.Financial.Domain;
using Heirloom.Financial.Tests.TestData;
using Heirloom.Financial.Valuation;

namespace Heirloom.Financial.Tests.Valuation;

public class MeltValueCalculatorTests
{
    private static readonly DateOnly Day = new(2026, 11, 20);

    private static SpotPrice GoldUsd(decimal price) => new(Metal.Gold, new Money(price, Currency.USD), Day);
    private static SpotPrice SilverUsd(decimal price) => new(Metal.Silver, new Money(price, Currency.USD), Day);

    /// <summary>
    /// Confronta due importi ignorando le ultime cifre decimali. I calcoli con una divisione
    /// (qui per 31,1034768) arrotondano alla 28ª cifra significativa, e l'ordine delle operazioni
    /// può cambiare quell'ultima cifra: 2 + 2 − 1 pezzi e 3 pezzi possono differire di 0,0000…0002.
    /// </summary>
    private static void AssertClose(Money expected, Money actual)
    {
        Assert.Equal(expected.Currency, actual.Currency);
        Assert.True(Math.Abs(expected.Amount - actual.Amount) < 0.000000001m, $"Atteso {expected}, ottenuto {actual}");
    }

    [Fact]
    public void Un_Krugerrand_vale_quanto_un_oncia_di_oro()
    {
        var value = MeltValueCalculator.MeltValue(TestProducts.Krugerrand1Oz, GoldUsd(2000m));

        Assert.Equal(Currency.USD, value.Currency);
        Assert.True(Math.Abs(value.Amount - 2000m) <= 0.01m, $"Valore: {value}");
    }

    [Fact]
    public void Un_lingotto_d_argento_da_1_kg_a_999_con_spot_30_vale_963_56_dollari()
    {
        var value = MeltValueCalculator.MeltValue(TestProducts.SilverBar1kg, SilverUsd(30m));

        Assert.Equal(963.56m, Math.Round(value.Amount, 2));
    }

    [Fact]
    public void Un_lingotto_d_oro_da_100_g_con_spot_2650_vale_8519_10_dollari()
    {
        var value = MeltValueCalculator.MeltValue(TestProducts.GoldBar100g, GoldUsd(2650m));

        Assert.Equal(8519.10m, Math.Round(value.Amount, 2));
    }

    [Fact]
    public void Piu_pezzi_valgono_la_quantita_per_il_valore_unitario()
    {
        var one = MeltValueCalculator.MeltValue(TestProducts.Marengo, GoldUsd(2650m));
        var five = MeltValueCalculator.MeltValue(TestProducts.Marengo, 5m, GoldUsd(2650m));

        AssertClose(one * 5m, five);
    }

    [Fact]
    public void Uno_spot_di_un_altro_metallo_viene_rifiutato()
    {
        var ex = Assert.Throws<ArgumentException>(
            () => MeltValueCalculator.MeltValue(TestProducts.SilverBar1kg, GoldUsd(2650m)));

        Assert.Equal("spot", ex.ParamName);
    }

    [Fact]
    public void Una_quantita_negativa_viene_rifiutata()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => MeltValueCalculator.MeltValue(TestProducts.Marengo, -1m, GoldUsd(2650m)));
    }

    [Fact]
    public void Si_puo_avere_il_valore_in_euro_con_un_cambio()
    {
        // 1 oz a 2.200 $, EUR/USD 1,10 → circa 2.000 €.
        var rate = new FxRate(Currency.EUR, Currency.USD, 1.10m, Day);

        var value = MeltValueCalculator.MeltValue(TestProducts.Krugerrand1Oz, GoldUsd(2200m), Currency.EUR, rate);

        Assert.Equal(Currency.EUR, value.Currency);
        Assert.Equal(2000.01m, Math.Round(value.Amount, 2));
    }

    // ---------- Elenco di lotti ----------

    [Fact]
    public void Il_valore_dei_lotti_conta_solo_quello_che_si_possiede_ancora()
    {
        // 4 Krugerrand comprati, 1 venduto: restano 3.
        var value = MeltValueCalculator.MeltValue(TestLots.WithPartialSale, [GoldUsd(2000m)]);

        var expected = MeltValueCalculator.MeltValue(TestProducts.Krugerrand1Oz, 3m, GoldUsd(2000m));
        AssertClose(expected, value);
    }

    [Fact]
    public void Il_portafoglio_misto_usa_lo_spot_di_ciascun_metallo()
    {
        var gold = GoldUsd(2650m);
        var silver = SilverUsd(31m);

        var value = MeltValueCalculator.MeltValue(TestLots.MixedMultiYear, [gold, silver]);

        var expected =
            MeltValueCalculator.MeltValue(TestProducts.GoldBar100g, 1m, gold) +
            MeltValueCalculator.MeltValue(TestProducts.SilverBar1kg, 1m, silver) +
            MeltValueCalculator.MeltValue(TestProducts.SilverMapleLeaf1Oz, 5m, silver) +   // 10 comprati − 5 venduti
            MeltValueCalculator.MeltValue(TestProducts.Krugerrand1Oz, 1m, gold) +
            MeltValueCalculator.MeltValue(TestProducts.Marengo, 3m, gold);

        AssertClose(expected, value);
    }

    [Fact]
    public void Senza_lo_spot_di_un_metallo_presente_viene_rifiutato()
    {
        var ex = Assert.Throws<ArgumentException>(
            () => MeltValueCalculator.MeltValue(TestLots.MixedMultiYear, [GoldUsd(2650m)]));

        Assert.Contains("Silver", ex.Message);
    }

    [Fact]
    public void Spot_in_valute_diverse_vengono_rifiutati()
    {
        var silverEur = new SpotPrice(Metal.Silver, new Money(28m, Currency.EUR), Day);

        Assert.Throws<ArgumentException>(
            () => MeltValueCalculator.MeltValue(TestLots.MixedMultiYear, [GoldUsd(2650m), silverEur]));
    }

    [Fact]
    public void Nessun_lotto_vale_zero()
    {
        var value = MeltValueCalculator.MeltValue([], [GoldUsd(2650m)]);

        Assert.Equal(Money.Zero(Currency.USD), value);
    }
}