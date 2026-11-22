using Heirloom.Financial.Domain;
using Heirloom.Financial.Performance;

namespace Heirloom.Financial.Tests.Performance;

public class CurrencyAttributionTests
{
    private static FxRate EurUsd(decimal rate, int year) => new(Currency.EUR, Currency.USD, rate, new DateOnly(year, 1, 2));

    [Fact]
    public void L_esempio_della_documentazione()
    {
        // Oro da 1.980 $ a 2.200 $ (+11,1%), EUR/USD da 1,10 a 1,20.
        var assetReturn = 2200m / 1980m - 1m;

        var result = CurrencyAttribution.Calculate(assetReturn, Currency.USD, Currency.EUR, EurUsd(1.10m, 2024), EurUsd(1.20m, 2026));

        Assert.Equal(0.1111m, Math.Round(result.AssetReturn, 4));
        Assert.Equal(-0.0833m, Math.Round(result.CurrencyEffect, 4));
        Assert.Equal(0.0185m, Math.Round(result.TotalReturn, 4));
    }

    [Fact]
    public void Con_il_cambio_invariato_l_effetto_cambio_e_zero()
    {
        var result = CurrencyAttribution.Calculate(0.15m, Currency.USD, Currency.EUR, EurUsd(1.10m, 2024), EurUsd(1.10m, 2026));

        Assert.Equal(0m, result.CurrencyEffect);
        Assert.Equal(0.15m, result.TotalReturn);
    }

    [Fact]
    public void Un_dollaro_che_si_rafforza_aumenta_il_guadagno_in_euro()
    {
        // EUR/USD scende da 1,20 a 1,00: ogni dollaro vale più euro (+20%).
        var result = CurrencyAttribution.Calculate(0m, Currency.USD, Currency.EUR, EurUsd(1.20m, 2024), EurUsd(1.00m, 2026));

        Assert.Equal(0.2m, result.CurrencyEffect);
        Assert.Equal(0.2m, result.TotalReturn);
    }

    [Fact]
    public void Nella_stessa_valuta_non_c_e_effetto_cambio()
    {
        var result = CurrencyAttribution.Calculate(0.07m, Currency.EUR, Currency.EUR, EurUsd(1.10m, 2024), EurUsd(1.30m, 2026));

        Assert.Equal(0m, result.CurrencyEffect);
        Assert.Equal(0.07m, result.TotalReturn);
    }
}