using Heirloom.Financial.Domain;

namespace Heirloom.Financial.Tests.Domain;

public class SpotPriceTests
{
    private static readonly DateOnly Day = new(2026, 11, 20);

    [Fact]
    public void Il_prezzo_spot_conserva_metallo_prezzo_e_data()
    {
        var spot = new SpotPrice(Metal.Gold, new Money(2650m, Currency.USD), Day);

        Assert.Equal(Metal.Gold, spot.Metal);
        Assert.Equal(new Money(2650m, Currency.USD), spot.PricePerTroyOunce);
        Assert.Equal(Day, spot.Date);
        Assert.Equal(Currency.USD, spot.Currency);
    }

    [Fact]
    public void Il_prezzo_al_grammo_e_il_prezzo_all_oncia_diviso_31_1034768()
    {
        var spot = new SpotPrice(Metal.Gold, new Money(31.1034768m, Currency.USD), Day);

        Assert.Equal(new Money(1m, Currency.USD), spot.PricePerGram);
    }

    [Fact]
    public void Un_metallo_non_definito_viene_rifiutato()
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(
            () => new SpotPrice((Metal)0, new Money(2650m, Currency.USD), Day));
        Assert.Equal("metal", ex.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Un_prezzo_non_positivo_viene_rifiutato(int price)
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(
            () => new SpotPrice(Metal.Silver, new Money(price, Currency.USD), Day));
        Assert.Equal("pricePerTroyOunce", ex.ParamName);
    }
}