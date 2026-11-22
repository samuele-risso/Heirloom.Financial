using Heirloom.Financial.Domain;
using Heirloom.Financial.Exchange;

namespace Heirloom.Financial.Tests.Exchange;

public class FxRateSeriesTests
{
    // Una settimana di cambi BCE: niente sabato 22 e domenica 23 novembre 2026.
    private static readonly DateOnly Thursday = new(2026, 11, 19);
    private static readonly DateOnly Friday = new(2026, 11, 20);
    private static readonly DateOnly Saturday = new(2026, 11, 21);
    private static readonly DateOnly Sunday = new(2026, 11, 22);
    private static readonly DateOnly Monday = new(2026, 11, 23);

    private static FxRate EurUsd(decimal rate, DateOnly date) => new(Currency.EUR, Currency.USD, rate, date);

    private static FxRateSeries Week() => new(
    [
        EurUsd(1.10m, Monday),      // in disordine apposta: la serie li ordina
        EurUsd(1.08m, Thursday),
        EurUsd(1.09m, Friday),
    ]);

    [Fact]
    public void Restituisce_il_cambio_del_giorno_esatto()
    {
        Assert.Equal(1.08m, Week().GetRate(Thursday).Rate);
        Assert.Equal(1.10m, Week().GetRate(Monday).Rate);
    }

    [Fact]
    public void Sabato_e_domenica_usano_il_cambio_del_venerdi()
    {
        var series = Week();

        Assert.Equal(1.09m, series.GetRate(Saturday).Rate);
        Assert.Equal(1.09m, series.GetRate(Sunday).Rate);
        Assert.Equal(Friday, series.GetRate(Sunday).Date);
    }

    [Fact]
    public void Dopo_l_ultimo_cambio_usa_l_ultimo_disponibile()
    {
        Assert.Equal(1.10m, Week().GetRate(Monday.AddDays(30)).Rate);
    }

    [Fact]
    public void Prima_del_primo_cambio_lancia_un_errore_con_la_data()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => Week().GetRate(new DateOnly(2026, 11, 18)));

        Assert.Contains("2026-11-18", ex.Message);
    }

    [Fact]
    public void Espone_coppia_numero_e_intervallo_di_date()
    {
        var series = Week();

        Assert.Equal(Currency.EUR, series.Base);
        Assert.Equal(Currency.USD, series.Quote);
        Assert.Equal(3, series.Count);
        Assert.Equal(Thursday, series.FirstDate);
        Assert.Equal(Monday, series.LastDate);
    }

    [Fact]
    public void Converte_con_il_cambio_della_data_indicata()
    {
        // Acquisto pagato 1.000 € di sabato: si usa il cambio del venerdì (1,09).
        var usd = Week().Convert(new Money(1000m, Currency.EUR), Currency.USD, Saturday);

        Assert.Equal(new Money(1090m, Currency.USD), usd);
    }

    [Fact]
    public void Date_ripetute_vengono_rifiutate()
    {
        Assert.Throws<ArgumentException>(() => new FxRateSeries([EurUsd(1.08m, Friday), EurUsd(1.09m, Friday)]));
    }

    [Fact]
    public void Coppie_diverse_vengono_rifiutate()
    {
        var usdEur = new FxRate(Currency.USD, Currency.EUR, 0.92m, Monday);

        Assert.Throws<ArgumentException>(() => new FxRateSeries([EurUsd(1.08m, Friday), usdEur]));
    }

    [Fact]
    public void Una_serie_vuota_viene_rifiutata()
    {
        Assert.Throws<ArgumentException>(() => new FxRateSeries([]));
    }

    [Fact]
    public void Funziona_su_dieci_anni_di_cambi()
    {
        var start = new DateOnly(2016, 1, 1);
        var rates = Enumerable.Range(0, 3650)
            .Select(i => start.AddDays(i))
            .Where(d => d.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday))
            .Select(d => EurUsd(1m + d.DayNumber % 100 / 1000m, d));

        var series = new FxRateSeries(rates);

        // Un sabato qualsiasi del 2020 deve dare il cambio del venerdì precedente.
        var saturday = new DateOnly(2020, 3, 14);
        Assert.Equal(saturday.AddDays(-1), series.GetRate(saturday).Date);
    }
}