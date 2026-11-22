using Heirloom.Financial.Domain;
using Heirloom.Financial.Market;

namespace Heirloom.Financial.Tests.Market;

public class MarketRatiosTests
{
    private static readonly DateOnly Day = new(2025, 11, 21);

    private static SpotPrice Spot(Metal metal, decimal price, Currency currency = Currency.USD) =>
        new(metal, new Money(price, currency), Day);

    [Fact]
    public void Oro_2000_e_argento_25_danno_un_rapporto_di_80()
    {
        Assert.Equal(80m, MarketRatios.GoldSilverRatio(Spot(Metal.Gold, 2000m), Spot(Metal.Silver, 25m)));
    }

    [Fact]
    public void Spot_in_valute_diverse_vengono_rifiutati()
    {
        Assert.Throws<ArgumentException>(
            () => MarketRatios.GoldSilverRatio(Spot(Metal.Gold, 2000m), Spot(Metal.Silver, 23m, Currency.EUR)));
    }

    [Fact]
    public void Metalli_scambiati_vengono_rifiutati()
    {
        Assert.Throws<ArgumentException>(
            () => MarketRatios.GoldSilverRatio(Spot(Metal.Silver, 25m), Spot(Metal.Gold, 2000m)));
    }

    [Fact]
    public void La_serie_storica_del_rapporto()
    {
        var gold = new[] { new DatedValue(Day, 2000m), new DatedValue(Day.AddDays(1), 2100m) };
        var silver = new[] { new DatedValue(Day, 25m), new DatedValue(Day.AddDays(1), 30m) };

        var ratio = MarketRatios.GoldSilverRatioSeries(gold, silver);

        Assert.Equal(new[] { 80m, 70m }, ratio.Select(r => r.Value));
    }
}