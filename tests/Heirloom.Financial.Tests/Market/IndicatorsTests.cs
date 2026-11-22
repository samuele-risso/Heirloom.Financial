using Heirloom.Financial.Market;

namespace Heirloom.Financial.Tests.Market;

public class IndicatorsTests
{
    private static readonly DateOnly Day0 = new(2025, 1, 1);

    /// <summary>25 chiusure: 101, 102, …, 125.</summary>
    private static readonly DatedValue[] Closes =
        Enumerable.Range(1, 25).Select(i => new DatedValue(Day0.AddDays(i - 1), 100m + i)).ToArray();

    [Fact]
    public void La_SMA_a_20_periodi_coincide_con_il_calcolo_a_mano_sugli_ultimi_3_punti()
    {
        var sma = Indicators.Sma(Closes, 20);

        // Media di 104..123 = 113,5 · di 105..124 = 114,5 · di 106..125 = 115,5
        Assert.Equal(113.5m, sma[22].Value);
        Assert.Equal(114.5m, sma[23].Value);
        Assert.Equal(115.5m, sma[24].Value);
    }

    [Fact]
    public void I_primi_N_meno_1_punti_non_hanno_valore()
    {
        var sma = Indicators.Sma(Closes, 20);

        Assert.All(sma.Take(19), p => Assert.Null(p.Value));
        Assert.Equal(110.5m, sma[19].Value);   // media di 101..120
    }

    [Fact]
    public void L_EMA_parte_dalla_SMA_e_poi_pesa_di_piu_le_chiusure_recenti()
    {
        var closes = new[] { 10m, 11m, 12m, 13m, 20m }
            .Select((v, i) => new DatedValue(Day0.AddDays(i), v))
            .ToArray();

        var ema = Indicators.Ema(closes, 3);

        // Primo valore: SMA di 10, 11, 12 = 11. k = 2/4 = 0,5.
        Assert.Null(ema[1].Value);
        Assert.Equal(11m, ema[2].Value);
        Assert.Equal(12m, ema[3].Value);    // 13 × 0,5 + 11 × 0,5
        Assert.Equal(16m, ema[4].Value);    // 20 × 0,5 + 12 × 0,5
    }

    [Fact]
    public void L_ordine_delle_chiusure_non_conta()
    {
        var shuffled = Enumerable.Reverse(Closes).ToArray();

        Assert.Equal(Indicators.Sma(Closes, 5), Indicators.Sma(shuffled, 5));
    }

    [Fact]
    public void Un_numero_di_periodi_non_positivo_viene_rifiutato()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Indicators.Sma(Closes, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => Indicators.Ema(Closes, -1));
    }
}