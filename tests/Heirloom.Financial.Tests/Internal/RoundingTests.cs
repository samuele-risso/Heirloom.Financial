using Heirloom.Financial.Domain;
using Heirloom.Financial.Internal;

namespace Heirloom.Financial.Tests.Internal;

public class RoundingTests
{
    [Theory]
    [InlineData("2.345", "2.35")]
    [InlineData("-2.345", "-2.35")]
    [InlineData("2.344", "2.34")]
    [InlineData("0.005", "0.01")]
    public void Gli_importi_si_arrotondano_a_2_decimali_con_il_metodo_commerciale(string input, string expected)
    {
        Assert.Equal(Parse(expected), Rounding.Amount(Parse(input)));
    }

    [Fact]
    public void Il_metodo_bancario_di_default_darebbe_un_risultato_diverso()
    {
        // Questo test documenta PERCHÉ serve Rounding: Math.Round senza parametri sbaglia per i soldi.
        Assert.Equal(2.34m, Math.Round(2.345m, 2));
        Assert.Equal(2.35m, Rounding.Amount(2.345m));
    }

    [Fact]
    public void Un_importo_arrotondato_mantiene_la_valuta()
    {
        var rounded = Rounding.Amount(new Money(100m / 3m, Currency.USD));

        Assert.Equal(new Money(33.33m, Currency.USD), rounded);
    }

    [Fact]
    public void I_pesi_si_arrotondano_a_4_decimali()
    {
        Assert.Equal(new Weight(31.1035m), Rounding.Grams(new Weight(31.1034768m)));
    }

    [Fact]
    public void Le_percentuali_si_arrotondano_a_4_decimali()
    {
        Assert.Equal(0.1235m, Rounding.Percentage(0.123456m));
        Assert.Equal(-0.0185m, Rounding.Percentage(-0.01845m));
    }

    private static decimal Parse(string text) =>
        decimal.Parse(text, System.Globalization.CultureInfo.InvariantCulture);
}