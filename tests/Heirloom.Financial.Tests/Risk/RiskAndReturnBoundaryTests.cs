using Heirloom.Financial.Domain;
using Heirloom.Financial.Market;
using Heirloom.Financial.Performance;
using Heirloom.Financial.Risk;

namespace Heirloom.Financial.Tests.Risk;

/// <summary>
/// I casi al confine di rendimenti, rischio e serie di mercato: numero minimo di valori,
/// valori a zero, uguaglianze esatte.
/// </summary>
public class RiskAndReturnsBoundaryTests
{
    private static readonly DateOnly Day = new(2024, 1, 1);

    private static IndexPoint[] Index(params decimal[] values) =>
        values.Select((v, i) => new IndexPoint(Day.AddDays(i), v)).ToArray();

    // ---------- Rendimenti ----------

    [Fact]
    public void Perdere_tutto_e_un_rendimento_di_meno_100_per_cento()
    {
        Assert.Equal(-1m, ReturnsCalculator.SimpleReturn(100m, 0m));
    }

    [Fact]
    public void Il_logaritmo_rifiuta_un_valore_iniziale_zero()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ReturnsCalculator.LogReturn(0m, 100m));
    }

    [Fact]
    public void Due_rendimenti_del_10_per_cento_fanno_21_per_cento()
    {
        Assert.Equal(0.21m, ReturnsCalculator.Cumulative([0.1m, 0.1m]));
    }

    [Fact]
    public void Due_valori_bastano_per_il_rendimento_cumulato()
    {
        Assert.Equal(0.1m, ReturnsCalculator.CumulativeFromValues([100m, 110m]));
    }

    // ---------- TWR ----------

    [Fact]
    public void Due_snapshot_bastano_per_il_twr()
    {
        var snapshots = new[]
        {
            new WalletSnapshot(Day, new Money(100m, Currency.EUR), new Money(100m, Currency.EUR), Weight.Zero, Weight.Zero),
            new WalletSnapshot(Day.AddDays(1), new Money(110m, Currency.EUR), new Money(100m, Currency.EUR), Weight.Zero, Weight.Zero),
        };

        Assert.Equal(0.1m, TimeWeightedReturn.Calculate(snapshots));
    }

    // ---------- Volatilità ----------

    [Fact]
    public void Tre_valori_bastano_per_la_volatilita()
    {
        Assert.NotNull(VolatilityCalculator.PeriodVolatility([100m, 110m, 121m]));
    }

    [Fact]
    public void La_volatilita_usa_gli_scarti_dalla_media()
    {
        // Rendimenti 10%, 10%, 0%: media 6,67%, deviazione standard campionaria 0,0577350.
        var volatility = VolatilityCalculator.PeriodVolatility([100m, 110m, 121m, 121m]);

        Assert.Equal(0.0577350, volatility!.Value, 6);
    }

    [Fact]
    public void Base_dei_giorni_non_definita_viene_rifiutata_anche_con_pochi_valori()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => VolatilityCalculator.Annualized([100m], (AnnualizationBasis)0));
    }

    // ---------- Drawdown ----------

    [Fact]
    public void Una_serie_che_sale_sempre_non_ha_data_di_recupero()
    {
        var drawdown = DrawdownCalculator.Max(Index(1m, 1.1m, 1.2m))!;

        Assert.Equal(0m, drawdown.Depth);
        Assert.Null(drawdown.RecoveryDate);
    }

    [Fact]
    public void Tornare_esattamente_al_massimo_e_un_recupero()
    {
        var drawdown = DrawdownCalculator.Max(Index(1m, 1.2m, 0.9m, 1.2m))!;

        Assert.Equal(Day.AddDays(3), drawdown.RecoveryDate);
    }

    [Fact]
    public void Una_serie_che_parte_da_zero_non_divide_per_zero()
    {
        var series = DrawdownCalculator.Series(Index(0m, 0m));

        Assert.All(series, p => Assert.Equal(0m, p.Drawdown));
        Assert.Equal(0m, DrawdownCalculator.Max(Index(0m, 0m))!.Depth);
    }

    // ---------- Mercato ----------

    [Fact]
    public void Rapporto_con_due_spot_d_argento_viene_rifiutato()
    {
        var silver = new SpotPrice(Metal.Silver, new Money(25m, Currency.EUR), Day);

        Assert.Throws<ArgumentException>(() => MarketRatios.GoldSilverRatio(silver, silver));
    }

    [Fact]
    public void Forward_fill_di_un_solo_giorno_e_valido()
    {
        var filled = PriceSeries.ForwardFill([new DatedValue(Day, 10m)], Day, Day);

        Assert.Equal(10m, Assert.Single(filled).Value);
    }

    [Fact]
    public void Forward_fill_con_date_invertite_viene_rifiutato()
    {
        Assert.Throws<ArgumentException>(() => PriceSeries.ForwardFill([new DatedValue(Day, 10m)], Day, Day.AddDays(-1)));
    }

    [Fact]
    public void L_allineamento_parte_dalla_data_piu_recente_tra_i_due_inizi()
    {
        var early = new[] { new DatedValue(Day, 1m), new DatedValue(Day.AddDays(2), 2m) };
        var late = new[] { new DatedValue(Day.AddDays(1), 10m), new DatedValue(Day.AddDays(2), 20m) };

        Assert.Equal(Day.AddDays(1), PriceSeries.Align(early, late)[0].Date);
        Assert.Equal(Day.AddDays(1), PriceSeries.Align(late, early)[0].Date);
    }

    [Fact]
    public void Gli_indicatori_rifiutano_date_ripetute()
    {
        var closes = new[] { new DatedValue(Day, 1m), new DatedValue(Day, 2m), new DatedValue(Day.AddDays(1), 3m) };

        Assert.Throws<ArgumentException>(() => Indicators.Sma(closes, 2));
    }
}