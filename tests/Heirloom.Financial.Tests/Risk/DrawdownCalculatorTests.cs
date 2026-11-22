using Heirloom.Financial.Domain;
using Heirloom.Financial.Performance;
using Heirloom.Financial.Risk;

namespace Heirloom.Financial.Tests.Risk;

public class DrawdownCalculatorTests
{
    private const string File = "drawdown_cases.csv";
    private static readonly DateOnly Day0 = new(2025, 1, 1);

    private static List<IndexPoint> Series(params decimal[] values) =>
        values.Select((v, i) => new IndexPoint(Day0.AddDays(i), v)).ToList();

    public static TheoryData<string> Cases() => ReferenceSeries.Names(File);

    [Theory]
    [MemberData(nameof(Cases))]
    public void Coincide_con_il_drawdown_di_riferimento(string caseName)
    {
        var (values, expected) = ReferenceSeries.Load(File)[caseName];

        var max = DrawdownCalculator.Max(Series([.. values]))!;

        Assert.Equal(expected, (double)max.Depth, 9);
    }

    [Fact]
    public void Una_serie_sempre_crescente_non_ha_drawdown()
    {
        var points = DrawdownCalculator.Series(Series(100m, 101m, 105m, 110m));

        Assert.All(points, p => Assert.Equal(0m, p.Drawdown));
        Assert.Equal(0m, DrawdownCalculator.Max(Series(100m, 101m, 105m, 110m))!.Depth);
    }

    [Fact]
    public void Da_120_a_90_e_un_calo_del_25_per_cento_recuperato_a_130()
    {
        var max = DrawdownCalculator.Max(Series(100m, 120m, 90m, 130m))!;

        Assert.Equal(-0.25m, max.Depth);
        Assert.Equal(Day0.AddDays(1), max.PeakDate);
        Assert.Equal(Day0.AddDays(2), max.TroughDate);
        Assert.Equal(Day0.AddDays(3), max.RecoveryDate);
    }

    [Fact]
    public void Un_calo_non_ancora_recuperato_non_ha_data_di_recupero()
    {
        var max = DrawdownCalculator.Max(Series(100m, 120m, 90m, 110m))!;

        Assert.Equal(-0.25m, max.Depth);
        Assert.Null(max.RecoveryDate);
    }

    [Fact]
    public void La_serie_del_drawdown_giorno_per_giorno()
    {
        var points = DrawdownCalculator.Series(Series(100m, 120m, 90m, 108m, 130m));

        Assert.Equal(new[] { 0m, 0m, -0.25m, -0.1m, 0m }, points.Select(p => p.Drawdown));
    }

    [Fact]
    public void Una_serie_vuota_non_ha_drawdown()
    {
        Assert.Null(DrawdownCalculator.Max(new List<IndexPoint>()));
        Assert.Empty(DrawdownCalculator.Series([]));
    }

    [Fact]
    public void Un_grosso_acquisto_durante_un_calo_non_viene_letto_come_recupero()
    {
        // Il valore cala del 20%, poi un acquisto di 10.000 € lo fa "salire": ma è capitale nuovo, non un recupero.
        WalletSnapshot Snap(int d, decimal value, decimal invested) =>
            new(Day0.AddDays(d), new Money(value, Currency.EUR), new Money(invested, Currency.EUR), Weight.Zero, Weight.Zero);

        var snapshots = new[]
        {
            Snap(0, 1000m, 1000m),
            Snap(1, 800m, 1000m),
            Snap(2, 10800m, 11000m),
            Snap(3, 10800m, 11000m),
        };

        var max = DrawdownCalculator.Max(snapshots)!;

        Assert.Equal(-0.2m, max.Depth);
        Assert.Null(max.RecoveryDate);
    }
}