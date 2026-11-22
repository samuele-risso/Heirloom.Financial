using Heirloom.Financial.Domain;
using Heirloom.Financial.Performance;
using Heirloom.Financial.Positions;
using Heirloom.Financial.Tests.TestData;
using Heirloom.Financial.Valuation;

namespace Heirloom.Financial.Tests.Performance;

public class TimeWeightedReturnTests
{
    private static readonly DateOnly Day0 = new(2025, 1, 1);

    private static WalletSnapshot Snap(int day, decimal value, decimal invested) =>
        new(Day0.AddDays(day), new Money(value, Currency.EUR), new Money(invested, Currency.EUR), Weight.Zero, Weight.Zero);

    [Fact]
    public void Senza_acquisti_ne_vendite_il_TWR_e_il_rendimento_semplice()
    {
        var snapshots = new[] { Snap(0, 1000m, 1000m), Snap(1, 1100m, 1000m), Snap(2, 1210m, 1000m) };

        Assert.Equal(0.21m, TimeWeightedReturn.Calculate(snapshots));
    }

    [Fact]
    public void Un_grosso_acquisto_a_meta_periodo_non_altera_il_TWR()
    {
        // Stessi rendimenti giornalieri (+10%, +5%, +2%), con e senza un acquisto di 10.000 € il giorno 2.
        var without = new[] { Snap(0, 1000m, 1000m), Snap(1, 1100m, 1000m), Snap(2, 1155m, 1000m), Snap(3, 1178.10m, 1000m) };
        var with = new[] { Snap(0, 1000m, 1000m), Snap(1, 1100m, 1000m), Snap(2, 11155m, 11000m), Snap(3, 11378.10m, 11000m) };

        var expected = 1.10m * 1.05m * 1.02m - 1m;

        Assert.Equal(expected, TimeWeightedReturn.Calculate(without));
        Assert.Equal(expected, TimeWeightedReturn.Calculate(with));
    }

    [Fact]
    public void Con_un_solo_prodotto_e_nessun_flusso_il_TWR_e_il_rendimento_dello_spot()
    {
        var lots = TestLots.SingleBuy;   // 1 Krugerrand il 15/03/2024
        var day = lots[0].Date;
        var spots = new[]
        {
            new SpotPrice(Metal.Gold, new Money(2000m, Currency.EUR), day),
            new SpotPrice(Metal.Gold, new Money(2200m, Currency.EUR), day.AddDays(30)),
        };

        var snapshots = SnapshotBuilder.Build(lots, spots, SpotValuation.Instance, Currency.EUR, day, day.AddDays(30));

        Assert.Equal(0.1m, Math.Round(TimeWeightedReturn.Calculate(snapshots)!.Value, 10));
    }

    [Fact]
    public void Un_primo_giorno_a_portafoglio_vuoto_non_conta()
    {
        var snapshots = new[] { Snap(0, 0m, 0m), Snap(1, 1000m, 1000m), Snap(2, 1100m, 1000m) };

        Assert.Equal(0.1m, TimeWeightedReturn.Calculate(snapshots));
    }

    [Fact]
    public void La_serie_indicizzata_parte_da_1_e_segue_il_rendimento()
    {
        var snapshots = new[] { Snap(2, 1210m, 1000m), Snap(0, 1000m, 1000m), Snap(1, 1100m, 1000m) };   // in disordine apposta

        var index = TimeWeightedReturn.Index(snapshots);

        Assert.Equal(new[] { 1m, 1.1m, 1.21m }, index.Select(p => p.Value));
        Assert.Equal(Day0, index[0].Date);
    }

    [Fact]
    public void Con_meno_di_due_snapshot_il_TWR_e_assente()
    {
        Assert.Null(TimeWeightedReturn.Calculate([]));
        Assert.Null(TimeWeightedReturn.Calculate([Snap(0, 1000m, 1000m)]));
    }

    [Fact]
    public void Snapshot_ripetuti_o_in_valute_diverse_vengono_rifiutati()
    {
        Assert.Throws<ArgumentException>(() => TimeWeightedReturn.Calculate([Snap(0, 1000m, 1000m), Snap(0, 1100m, 1000m)]));

        var usd = new WalletSnapshot(Day0.AddDays(1), new Money(1100m, Currency.USD), new Money(1000m, Currency.USD), Weight.Zero, Weight.Zero);
        Assert.Throws<ArgumentException>(() => TimeWeightedReturn.Calculate([Snap(0, 1000m, 1000m), usd]));
    }
}