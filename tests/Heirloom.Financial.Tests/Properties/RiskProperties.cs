using FsCheck.Xunit;
using Heirloom.Financial.Performance;
using Heirloom.Financial.Risk;

namespace Heirloom.Financial.Tests.Properties;

/// <summary>
/// Regole di rischio che devono valere per qualsiasi serie di valori positivi.
/// </summary>
public class RiskProperties
{
    private static readonly DateOnly Day0 = new(2025, 1, 1);

    private static List<IndexPoint> Series(ushort[] raw) =>
        raw.Select((v, i) => new IndexPoint(Day0.AddDays(i), v + 1m)).ToList();

    [Property(MaxTest = 300)]
    public bool Il_drawdown_e_sempre_tra_meno_1_e_zero(ushort a, ushort b, ushort c, ushort d, ushort e)
    {
        var points = DrawdownCalculator.Series(Series([a, b, c, d, e]));

        return points.All(p => p.Drawdown <= 0m && p.Drawdown > -1m);
    }

    [Property(MaxTest = 300)]
    public bool Il_drawdown_massimo_e_il_minimo_della_serie_dei_drawdown(ushort a, ushort b, ushort c, ushort d, ushort e)
    {
        var series = Series([a, b, c, d, e]);

        return DrawdownCalculator.Max(series)!.Depth == DrawdownCalculator.Series(series).Min(p => p.Drawdown);
    }

    [Property(MaxTest = 300)]
    public bool Moltiplicare_tutti_i_valori_non_cambia_la_volatilita(ushort a, ushort b, ushort c, ushort d, byte factor)
    {
        var values = new[] { a + 1m, b + 1m, c + 1m, d + 1m };
        var scaled = values.Select(v => v * (factor + 1)).ToArray();

        var original = VolatilityCalculator.PeriodVolatility(values)!.Value;
        var multiplied = VolatilityCalculator.PeriodVolatility(scaled)!.Value;

        return Math.Abs(original - multiplied) < 1e-9;
    }
}