using Heirloom.Financial.Constants;
using Heirloom.Financial.Domain;

namespace Heirloom.Financial.Tests.Constants;

public class ConstantsTests
{
    [Fact]
    public void Un_oncia_troy_vale_31_1034768_grammi()
    {
        Assert.Equal(31.1034768m, UnitConstants.GramsPerTroyOunce);
    }

    [Fact]
    public void Weight_usa_la_stessa_costante_di_UnitConstants()
    {
        Assert.Equal(UnitConstants.GramsPerTroyOunce, Weight.GramsPerTroyOunce);
    }

    [Fact]
    public void Un_chilo_vale_1000_grammi()
    {
        Assert.Equal(new Weight(1000m), Weight.FromKilograms(1m));
        Assert.Equal(new Weight(250m), Weight.FromKilograms(0.25m));
    }

    [Fact]
    public void I_titoli_comuni_sono_tutti_validi()
    {
        var all = new[]
        {
            CommonFineness.Gold9999, CommonFineness.Gold9167, CommonFineness.Gold900,
            CommonFineness.Silver9999, CommonFineness.Silver999, CommonFineness.Silver925,
        };

        Assert.All(all, f => Assert.InRange(f, 0.0001m, 1m));
    }
}