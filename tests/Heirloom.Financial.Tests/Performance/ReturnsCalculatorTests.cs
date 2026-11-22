using Heirloom.Financial.Performance;

namespace Heirloom.Financial.Tests.Performance;

public class ReturnsCalculatorTests
{
    [Fact]
    public void Piu_dieci_e_poi_meno_dieci_fa_meno_uno_per_cento()
    {
        // I rendimenti si compongono: 1,10 × 0,90 = 0,99.
        Assert.Equal(-0.01m, ReturnsCalculator.Cumulative([0.10m, -0.10m]));
    }

    [Fact]
    public void Il_rendimento_semplice_e_fine_su_inizio_meno_uno()
    {
        Assert.Equal(0.25m, ReturnsCalculator.SimpleReturn(2000m, 2500m));
        Assert.Equal(-0.2m, ReturnsCalculator.SimpleReturn(2500m, 2000m));
    }

    [Fact]
    public void Venti_per_cento_in_tre_anni_sono_circa_6_27_per_cento_annuo()
    {
        Assert.Equal(0.0627m, Math.Round(ReturnsCalculator.Annualized(0.20m, 3.0), 4));
    }

    [Fact]
    public void Annualizzato_tra_due_date()
    {
        // Circa un anno (365 giorni su 365,25): l'annualizzato è quasi uguale al totale.
        var annual = ReturnsCalculator.Annualized(0.10m, new DateOnly(2025, 1, 1), new DateOnly(2026, 1, 1));

        Assert.True(Math.Abs(annual - 0.10m) < 0.001m, $"Annualizzato: {annual}");
    }

    [Fact]
    public void I_rendimenti_di_periodo_di_una_serie()
    {
        var returns = ReturnsCalculator.PeriodReturns([100m, 110m, 99m]);

        Assert.Equal(new[] { 0.1m, -0.1m }, returns);
    }

    [Fact]
    public void Con_meno_di_due_valori_il_cumulato_e_assente()
    {
        Assert.Null(ReturnsCalculator.CumulativeFromValues([]));
        Assert.Null(ReturnsCalculator.CumulativeFromValues([100m]));
        Assert.Empty(ReturnsCalculator.PeriodReturns([100m]));
    }

    [Fact]
    public void Il_cumulato_di_nessun_periodo_e_zero()
    {
        Assert.Equal(0m, ReturnsCalculator.Cumulative([]));
    }

    [Fact]
    public void Il_rendimento_logaritmico()
    {
        Assert.Equal(Math.Log(1.1), ReturnsCalculator.LogReturn(100m, 110m), 12);
    }

    [Fact]
    public void Valori_non_validi_vengono_rifiutati()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ReturnsCalculator.SimpleReturn(0m, 100m));
        Assert.Throws<ArgumentOutOfRangeException>(() => ReturnsCalculator.SimpleReturn(100m, -1m));
        Assert.Throws<ArgumentOutOfRangeException>(() => ReturnsCalculator.LogReturn(100m, 0m));
        Assert.Throws<ArgumentOutOfRangeException>(() => ReturnsCalculator.Annualized(-1m, 1.0));
        Assert.Throws<ArgumentOutOfRangeException>(() => ReturnsCalculator.Annualized(0.1m, 0.0));
    }
}