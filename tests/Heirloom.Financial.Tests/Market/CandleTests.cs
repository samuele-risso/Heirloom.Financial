using Heirloom.Financial.Domain;
using Heirloom.Financial.Exchange;
using Heirloom.Financial.Market;

namespace Heirloom.Financial.Tests.Market;

public class CandleTests
{
    private static readonly DateTime Monday = new(2024, 3, 4, 0, 0, 0, DateTimeKind.Utc);
    private static Money Usd(decimal amount) => new(amount, Currency.USD);
    private static Money Eur(decimal amount) => new(amount, Currency.EUR);

    private static Candle Hour(int hour, decimal open, decimal high, decimal low, decimal close, decimal volume = 1m, decimal trades = 10m) =>
        new(Monday.AddHours(hour), Monday.AddHours(hour + 1).AddMilliseconds(-1), Usd(open), Usd(high), Usd(low), Usd(close), volume, trades);

    // ---------- Validazione ----------

    [Fact]
    public void Una_candela_valida_conserva_i_dati()
    {
        var candle = Hour(9, 2000m, 2010m, 1990m, 2005m, 3.5m, 42m);

        Assert.Equal(Usd(2000m), candle.Open);
        Assert.Equal(Usd(2010m), candle.High);
        Assert.Equal(Usd(1990m), candle.Low);
        Assert.Equal(Usd(2005m), candle.Close);
        Assert.Equal(3.5m, candle.Volume);
        Assert.Equal(42m, candle.Trades);
        Assert.Equal(Currency.USD, candle.Currency);
        Assert.Equal(new DateOnly(2024, 3, 4), candle.Day);
    }

    [Fact]
    public void Massimo_sotto_il_minimo_viene_rifiutato()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Hour(0, 2000m, 1980m, 1990m, 2000m));
    }

    [Fact]
    public void Prezzi_negativi_vengono_rifiutati()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Hour(0, -1m, 10m, 0m, 5m));
        Assert.Throws<ArgumentOutOfRangeException>(() => Hour(0, 1m, 10m, -1m, 5m));
        Assert.Throws<ArgumentOutOfRangeException>(() => Hour(0, 1m, 10m, 0m, -5m));
    }

    [Fact]
    public void Volume_o_scambi_negativi_vengono_rifiutati()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Hour(0, 1m, 2m, 1m, 1m, volume: -1m));
        Assert.Throws<ArgumentOutOfRangeException>(() => Hour(0, 1m, 2m, 1m, 1m, trades: -1m));
    }

    [Fact]
    public void Fine_prima_dell_inizio_viene_rifiutata()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new Candle(Monday, Monday.AddHours(-1), Usd(1m), Usd(1m), Usd(1m), Usd(1m), 0m));
    }

    [Fact]
    public void Prezzi_in_valute_diverse_vengono_rifiutati()
    {
        Assert.Throws<ArgumentException>(() =>
            new Candle(Monday, Monday.AddHours(1), Usd(1m), Eur(2m), Usd(1m), Usd(1m), 0m));
    }

    // ---------- Conversione ----------

    [Fact]
    public void La_conversione_usa_il_cambio_del_giorno_della_candela()
    {
        // 1 EUR = 1,10 USD il lunedì, 1,25 USD dal martedì.
        var rates = new FxRateSeries(
        [
            new FxRate(Currency.EUR, Currency.USD, 1.10m, new DateOnly(2024, 3, 4)),
            new FxRate(Currency.EUR, Currency.USD, 1.25m, new DateOnly(2024, 3, 5)),
        ]);

        var converted = Hour(23, 2200m, 2420m, 1980m, 2310m).ConvertTo(Currency.EUR, rates);

        Assert.Equal(Eur(2000m), converted.Open);
        Assert.Equal(Eur(2200m), converted.High);
        Assert.Equal(Eur(1800m), converted.Low);
        Assert.Equal(Eur(2100m), converted.Close);
        Assert.Equal(Currency.EUR, converted.Currency);
        Assert.Equal(1m, converted.Volume);
    }

    [Fact]
    public void Convertire_nella_stessa_valuta_restituisce_la_stessa_candela()
    {
        var candle = Hour(0, 1m, 2m, 1m, 2m);
        var rates = new FxRateSeries([new FxRate(Currency.EUR, Currency.USD, 1.1m, new DateOnly(2024, 1, 1))]);

        Assert.Same(candle, candle.ConvertTo(Currency.USD, rates));
    }

    [Fact]
    public void Senza_cambio_fino_a_quel_giorno_la_conversione_si_ferma()
    {
        var rates = new FxRateSeries([new FxRate(Currency.EUR, Currency.USD, 1.1m, new DateOnly(2024, 6, 1))]);

        Assert.Throws<InvalidOperationException>(() => Hour(0, 1m, 2m, 1m, 2m).ConvertTo(Currency.EUR, rates));
    }

    [Fact]
    public void Lo_spot_e_la_chiusura_del_giorno()
    {
        var spot = Hour(9, 2000m, 2010m, 1990m, 2005m).ToSpot(Metal.Gold);

        Assert.Equal(new SpotPrice(Metal.Gold, Usd(2005m), new DateOnly(2024, 3, 4)), spot);
    }

    // ---------- Aggregazione ----------

    [Fact]
    public void Tre_ore_diventano_una_candela()
    {
        var day = CandleAggregator.Aggregate(
        [
            Hour(2, 2010m, 2030m, 2005m, 2020m, volume: 2m, trades: 5m),
            Hour(0, 2000m, 2015m, 1995m, 2010m, volume: 1m, trades: 3m),
            Hour(1, 2010m, 2012m, 1980m, 2010m, volume: 4m, trades: 7m),
        ]);

        Assert.Equal(Monday, day.OpenTime);
        Assert.Equal(Monday.AddHours(3).AddMilliseconds(-1), day.CloseTime);
        Assert.Equal(Usd(2000m), day.Open);
        Assert.Equal(Usd(2030m), day.High);
        Assert.Equal(Usd(1980m), day.Low);
        Assert.Equal(Usd(2020m), day.Close);
        Assert.Equal(7m, day.Volume);
        Assert.Equal(15m, day.Trades);
    }

    [Fact]
    public void Una_sola_candela_resta_uguale()
    {
        var hour = Hour(5, 1m, 3m, 1m, 2m);

        Assert.Equal(hour, CandleAggregator.Aggregate([hour]));
    }

    [Fact]
    public void Nessuna_candela_viene_rifiutata()
    {
        Assert.Throws<ArgumentException>(() => CandleAggregator.Aggregate([]));
    }

    [Fact]
    public void Candele_in_valute_diverse_vengono_rifiutate()
    {
        var eur = new Candle(Monday, Monday.AddHours(1), Eur(1m), Eur(2m), Eur(1m), Eur(2m), 0m);

        Assert.Throws<ArgumentException>(() => CandleAggregator.Aggregate([Hour(0, 1m, 2m, 1m, 2m), eur]));
    }
}