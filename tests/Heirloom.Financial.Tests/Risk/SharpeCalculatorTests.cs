using Heirloom.Financial.Risk;

namespace Heirloom.Financial.Tests.Risk;

public class SharpeCalculatorTests
{
    [Fact]
    public void Rendimento_8_rischio_zero_3_volatilita_15_danno_0_333()
    {
        Assert.Equal(0.333, SharpeCalculator.Calculate(0.08, 0.03, 0.15)!.Value, 3);
    }

    [Fact]
    public void Un_rendimento_sotto_il_tasso_privo_di_rischio_da_uno_Sharpe_negativo()
    {
        Assert.True(SharpeCalculator.Calculate(0.01, 0.03, 0.10) < 0);
    }

    [Fact]
    public void Con_volatilita_zero_lo_Sharpe_e_assente_non_infinito()
    {
        Assert.Null(SharpeCalculator.Calculate(0.08, 0.03, 0.0));
    }

    [Fact]
    public void Una_volatilita_negativa_viene_rifiutata()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => SharpeCalculator.Calculate(0.08, 0.03, -0.1));
    }
}