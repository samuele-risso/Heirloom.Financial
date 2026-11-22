using FsCheck.Xunit;
using Heirloom.Financial.Domain;
using Heirloom.Financial.Performance;

namespace Heirloom.Financial.Tests.Properties;

/// <summary>
/// Regole dei rendimenti che devono valere per qualsiasi serie di valori.
/// I valori sono generati come centesimi positivi (ushort + 1).
/// </summary>
public class PerformanceProperties
{
    private static readonly DateOnly Day0 = new(2025, 1, 1);

    [Property(MaxTest = 300)]
    public bool Senza_flussi_il_TWR_coincide_con_il_rendimento_semplice(ushort a, ushort b, ushort c, ushort d)
    {
        var values = new[] { a + 1m, b + 1m, c + 1m, d + 1m };
        var snapshots = values.Select((v, i) =>
            new WalletSnapshot(Day0.AddDays(i), new Money(v, Currency.EUR), new Money(100m, Currency.EUR), Weight.Zero, Weight.Zero));

        var twr = TimeWeightedReturn.Calculate(snapshots)!.Value;
        var simple = ReturnsCalculator.SimpleReturn(values[0], values[^1]);

        return Math.Abs(twr - simple) < 0.0000001m;
    }

    [Property(MaxTest = 300)]
    public bool La_somma_dei_rendimenti_logaritmici_e_il_logaritmo_del_cumulato(ushort a, ushort b, ushort c, ushort d)
    {
        var values = new[] { a + 1m, b + 1m, c + 1m, d + 1m };

        var sumOfLogs = values.Zip(values.Skip(1), ReturnsCalculator.LogReturn).Sum();
        var logOfTotal = ReturnsCalculator.LogReturn(values[0], values[^1]);

        return Math.Abs(sumOfLogs - logOfTotal) < 1e-9;
    }

    [Property(MaxTest = 300)]
    public bool Metallo_e_cambio_si_compongono_nel_rendimento_totale(short assetBasisPoints, ushort startRate, ushort endRate)
    {
        var start = startRate % 20_000 + 5_000;   // cambio tra 0,5 e 2,5
        var end = endRate % 20_000 + 5_000;
        var assetReturn = assetBasisPoints / 10_000m;

        var result = CurrencyAttribution.Calculate(
            assetReturn, Currency.USD, Currency.EUR,
            new FxRate(Currency.EUR, Currency.USD, start / 10_000m, Day0),
            new FxRate(Currency.EUR, Currency.USD, end / 10_000m, Day0.AddDays(365)));

        var recomposed = (1m + result.AssetReturn) * (1m + result.CurrencyEffect) - 1m;
        return Math.Abs(recomposed - result.TotalReturn) < 0.0000001m;
    }
}