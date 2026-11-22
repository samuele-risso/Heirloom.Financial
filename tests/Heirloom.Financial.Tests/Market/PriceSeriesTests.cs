using Heirloom.Financial.Market;

namespace Heirloom.Financial.Tests.Market;

public class PriceSeriesTests
{
    // Venerdì 21 e lunedì 24 novembre 2025.
    private static readonly DateOnly Friday = new(2025, 11, 21);

    private static DatedValue V(int dayOffset, decimal value) => new(Friday.AddDays(dayOffset), value);

    [Fact]
    public void Il_weekend_prende_il_valore_del_venerdi()
    {
        var filled = PriceSeries.ForwardFill([V(0, 100m), V(3, 103m)], Friday, Friday.AddDays(3));

        Assert.Equal(new[] { 100m, 100m, 100m, 103m }, filled.Select(v => v.Value));
        Assert.Equal(4, filled.Count);
    }

    [Fact]
    public void Un_buco_di_cinque_giorni_prende_l_ultimo_valore_noto_senza_interpolare()
    {
        var filled = PriceSeries.ForwardFill([V(0, 100m), V(6, 200m)], Friday, Friday.AddDays(6));

        Assert.Equal(new[] { 100m, 100m, 100m, 100m, 100m, 100m, 200m }, filled.Select(v => v.Value));
    }

    [Fact]
    public void I_giorni_prima_del_primo_valore_sono_esclusi()
    {
        var filled = PriceSeries.ForwardFill([V(2, 100m)], Friday, Friday.AddDays(3));

        Assert.Equal(Friday.AddDays(2), filled[0].Date);
        Assert.Equal(2, filled.Count);
    }

    [Fact]
    public void Due_serie_si_allineano_sull_unione_delle_date_dal_primo_giorno_comune()
    {
        // Oro dal giorno 0, argento dal giorno 1; date diverse in mezzo.
        var gold = new[] { V(0, 2000m), V(1, 2010m), V(3, 2030m) };
        var silver = new[] { V(1, 25m), V(2, 26m), V(3, 27m) };

        var aligned = PriceSeries.Align(gold, silver);

        Assert.Equal(new[] { Friday.AddDays(1), Friday.AddDays(2), Friday.AddDays(3) }, aligned.Select(a => a.Date));
        Assert.Equal(new[] { 2010m, 2010m, 2030m }, aligned.Select(a => a.First));    // il giorno 2 l'oro prende il valore del giorno 1
        Assert.Equal(new[] { 25m, 26m, 27m }, aligned.Select(a => a.Second));
    }

    [Fact]
    public void Se_una_serie_e_vuota_l_allineamento_e_vuoto()
    {
        Assert.Empty(PriceSeries.Align([V(0, 1m)], []));
    }

    [Fact]
    public void Date_ripetute_vengono_rifiutate()
    {
        Assert.Throws<ArgumentException>(() => PriceSeries.Align([V(0, 1m), V(0, 2m)], [V(0, 1m)]));
    }
}