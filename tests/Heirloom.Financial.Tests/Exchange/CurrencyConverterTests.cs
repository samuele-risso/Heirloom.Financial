using Heirloom.Financial.Domain;
using Heirloom.Financial.Exchange;

namespace Heirloom.Financial.Tests.Exchange;

public class CurrencyConverterTests
{
    private static readonly DateOnly Day = new(2026, 11, 20);
    private static readonly FxRate EurUsd = new(Currency.EUR, Currency.USD, 1.10m, Day);

    [Fact]
    public void Da_base_a_quotata_si_moltiplica()
    {
        // Il test "sulla direzione": 100 € con EUR/USD 1,10 sono 110 $, non 90,91 $.
        var result = CurrencyConverter.Convert(new Money(100m, Currency.EUR), Currency.USD, EurUsd);

        Assert.Equal(new Money(110m, Currency.USD), result);
    }

    [Fact]
    public void Da_quotata_a_base_si_divide()
    {
        var result = CurrencyConverter.Convert(new Money(110m, Currency.USD), Currency.EUR, EurUsd);

        Assert.Equal(new Money(100m, Currency.EUR), result);
    }

    [Fact]
    public void Funziona_anche_con_il_cambio_espresso_al_contrario()
    {
        // USD/EUR 0,8 equivale a EUR/USD 1,25: 100 € devono diventare 125 $.
        var usdEur = new FxRate(Currency.USD, Currency.EUR, 0.8m, Day);

        var result = CurrencyConverter.Convert(new Money(100m, Currency.EUR), Currency.USD, usdEur);

        Assert.Equal(new Money(125m, Currency.USD), result);
    }

    [Fact]
    public void Nella_stessa_valuta_l_importo_non_cambia()
    {
        var amount = new Money(42m, Currency.EUR);

        Assert.Equal(amount, CurrencyConverter.Convert(amount, Currency.EUR, EurUsd));
    }

    [Fact]
    public void Non_arrotonda()
    {
        var result = CurrencyConverter.Convert(new Money(100m, Currency.USD), Currency.EUR, EurUsd);

        Assert.Equal(100m / 1.10m, result.Amount);
    }

    [Fact]
    public void Un_cambio_null_viene_rifiutato()
    {
        Assert.Throws<ArgumentNullException>(
            () => CurrencyConverter.Convert(new Money(1m, Currency.EUR), Currency.USD, null!));
    }
}