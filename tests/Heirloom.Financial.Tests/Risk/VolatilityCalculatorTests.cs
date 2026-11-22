using Heirloom.Financial.Domain;
using Heirloom.Financial.Risk;

namespace Heirloom.Financial.Tests.Risk;

public class VolatilityCalculatorTests
{
    private const string File = "volatility_cases.csv";

    public static TheoryData<string> Cases() => ReferenceSeries.Names(File);

    [Theory]
    [MemberData(nameof(Cases))]
    public void Coincide_con_DEV_ST_dei_rendimenti(string caseName)
    {
        var (values, expected) = ReferenceSeries.Load(File)[caseName];

        Assert.Equal(expected, VolatilityCalculator.PeriodVolatility(values)!.Value, 9);
    }

    [Fact]
    public void L_annualizzazione_moltiplica_per_la_radice_dei_periodi()
    {
        var (values, daily) = ReferenceSeries.Load(File)["random_walk"];

        Assert.Equal(daily * Math.Sqrt(252), VolatilityCalculator.Annualized(values, AnnualizationBasis.TradingDays252)!.Value, 9);
        Assert.Equal(daily * Math.Sqrt(365), VolatilityCalculator.Annualized(values, AnnualizationBasis.CalendarDays365)!.Value, 9);
    }

    [Fact]
    public void Una_serie_costante_ha_volatilita_zero()
    {
        Assert.Equal(0.0, VolatilityCalculator.Annualized([100m, 100m, 100m, 100m], AnnualizationBasis.CalendarDays365));
    }

    [Fact]
    public void Con_meno_di_tre_valori_la_volatilita_e_assente()
    {
        Assert.Null(VolatilityCalculator.Annualized([100m, 101m], AnnualizationBasis.TradingDays252));
        Assert.Null(VolatilityCalculator.Annualized(Array.Empty<decimal>(), AnnualizationBasis.TradingDays252));
    }

    [Fact]
    public void La_base_di_annualizzazione_e_obbligatoria()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => VolatilityCalculator.Annualized([100m, 101m, 102m], default(AnnualizationBasis)));
    }

    [Fact]
    public void Sul_portafoglio_un_acquisto_non_e_un_oscillazione()
    {
        // Il valore passa da 1.000 a 11.000 per un acquisto, ma il rendimento è sempre zero.
        var day = new DateOnly(2025, 1, 1);
        WalletSnapshot Snap(int d, decimal value, decimal invested) =>
            new(day.AddDays(d), new Money(value, Currency.EUR), new Money(invested, Currency.EUR), Weight.Zero, Weight.Zero);

        var snapshots = new[] { Snap(0, 1000m, 1000m), Snap(1, 1000m, 1000m), Snap(2, 11000m, 11000m), Snap(3, 11000m, 11000m) };

        Assert.Equal(0.0, VolatilityCalculator.Annualized(snapshots, AnnualizationBasis.CalendarDays365));
    }
}