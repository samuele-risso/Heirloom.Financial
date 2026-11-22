using Heirloom.Financial.Domain;

namespace Heirloom.Financial.Tests.Domain;

public class FxRateTests
{
    private static readonly DateOnly Day = new(2026, 11, 20);

    [Fact]
    public void Il_cambio_conserva_valute_tasso_e_data()
    {
        // EUR/USD 1,08: 1 euro vale 1,08 dollari.
        var fx = new FxRate(Currency.EUR, Currency.USD, 1.08m, Day);

        Assert.Equal(Currency.EUR, fx.Base);
        Assert.Equal(Currency.USD, fx.Quote);
        Assert.Equal(1.08m, fx.Rate);
        Assert.Equal(Day, fx.Date);
    }

    [Fact]
    public void Il_cambio_inverso_scambia_le_valute_e_inverte_il_tasso()
    {
        var inverse = new FxRate(Currency.EUR, Currency.USD, 1.25m, Day).Inverse();

        Assert.Equal(Currency.USD, inverse.Base);
        Assert.Equal(Currency.EUR, inverse.Quote);
        Assert.Equal(0.8m, inverse.Rate);
        Assert.Equal(Day, inverse.Date);
    }

    [Fact]
    public void Involves_riconosce_la_coppia_in_entrambe_le_direzioni()
    {
        var fx = new FxRate(Currency.EUR, Currency.USD, 1.08m, Day);

        Assert.True(fx.Involves(Currency.EUR, Currency.USD));
        Assert.True(fx.Involves(Currency.USD, Currency.EUR));
        Assert.False(fx.Involves(Currency.EUR, Currency.EUR));
    }

    [Fact]
    public void ToString_mostra_coppia_tasso_e_data()
    {
        var fx = new FxRate(Currency.EUR, Currency.USD, 1.08m, Day);

        Assert.Equal("EUR/USD 1.08 @ 2026-11-20", fx.ToString());
    }

    [Fact]
    public void Base_e_quotata_uguali_vengono_rifiutate()
    {
        var ex = Assert.Throws<ArgumentException>(() => new FxRate(Currency.EUR, Currency.EUR, 1m, Day));
        Assert.Equal("quoteCurrency", ex.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Un_cambio_non_positivo_viene_rifiutato(int rate)
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => new FxRate(Currency.EUR, Currency.USD, rate, Day));
        Assert.Equal("rate", ex.ParamName);
    }

    [Fact]
    public void Una_valuta_non_definita_viene_rifiutata()
    {
        var ex1 = Assert.Throws<ArgumentOutOfRangeException>(() => new FxRate((Currency)0, Currency.USD, 1.08m, Day));
        Assert.Equal("baseCurrency", ex1.ParamName);

        var ex2 = Assert.Throws<ArgumentOutOfRangeException>(() => new FxRate(Currency.EUR, (Currency)99, 1.08m, Day));
        Assert.Equal("quoteCurrency", ex2.ParamName);
    }
}