using System.Diagnostics;
using Heirloom.Financial.Domain;
using Heirloom.Financial.Exchange;
using Heirloom.Financial.Performance;
using Heirloom.Financial.Tests.TestData;
using Heirloom.Financial.Valuation;

namespace Heirloom.Financial.Tests.Performance;

public class SnapshotBuilderTests
{
    private static Money Eur(decimal amount) => new(amount, Currency.EUR);
    private static SpotPrice GoldEur(decimal price, DateOnly date) => new(Metal.Gold, Eur(price), date);

    [Fact]
    public void Un_acquisto_di_un_anno_fa_produce_uno_snapshot_per_ogni_giorno()
    {
        var day = TestLots.SingleBuy[0].Date;
        var spots = Enumerable.Range(0, 53).Select(w => GoldEur(2000m + w * 10m, day.AddDays(w * 7)));

        var snapshots = SnapshotBuilder.Build(TestLots.SingleBuy, spots, SpotValuation.Instance, Currency.EUR, day, day.AddDays(364));

        Assert.Equal(365, snapshots.Count);
        Assert.All(snapshots, s =>
        {
            Assert.Equal(Eur(1925m), s.InvestedCapital);
            Assert.Equal(TestProducts.Krugerrand1Oz.FineWeight, s.GoldFineWeight);
            Assert.Equal(Weight.Zero, s.SilverFineWeight);
        });
    }

    [Fact]
    public void Un_prezzo_del_giorno_X_non_influenza_i_giorni_precedenti()
    {
        var day = TestLots.SingleBuy[0].Date;
        var spots = new[] { GoldEur(2000m, day), GoldEur(3000m, day.AddDays(10)) };

        var snapshots = SnapshotBuilder.Build(TestLots.SingleBuy, spots, SpotValuation.Instance, Currency.EUR, day, day.AddDays(12));

        var before = snapshots.Take(10).Select(s => s.Value).Distinct().ToList();
        Assert.Single(before);   // stesso valore, spot 2.000, dal giorno 0 al 9
        Assert.True(snapshots[10].Value > snapshots[9].Value);
    }

    [Fact]
    public void Prima_del_primo_acquisto_il_portafoglio_e_vuoto_e_non_serve_lo_spot()
    {
        var day = TestLots.SingleBuy[0].Date;
        var spots = new[] { GoldEur(2000m, day) };   // nessuno spot prima dell'acquisto

        var snapshots = SnapshotBuilder.Build(TestLots.SingleBuy, spots, SpotValuation.Instance, Currency.EUR, day.AddDays(-5), day);

        Assert.All(snapshots.Take(5), s =>
        {
            Assert.True(s.Value.IsZero);
            Assert.True(s.InvestedCapital.IsZero);
        });
        Assert.False(snapshots[^1].Value.IsZero);
    }

    [Fact]
    public void Una_vendita_riduce_quantita_e_capitale_investito()
    {
        var lots = TestLots.WithPartialSale;   // 4 Krugerrand (8.800 €), poi 1 venduto il 01/03/2025 per 2.590 € netti
        var saleDay = new DateOnly(2025, 3, 1);
        var spots = new[] { GoldEur(2500m, new DateOnly(2024, 1, 1)) };

        var snapshots = SnapshotBuilder.Build(lots, spots, SpotValuation.Instance, Currency.EUR, saleDay.AddDays(-1), saleDay);

        Assert.Equal(Eur(8800m), snapshots[0].InvestedCapital);
        Assert.Equal(Eur(6210m), snapshots[1].InvestedCapital);
        Assert.Equal(TestProducts.Krugerrand1Oz.FineWeight * 3m, snapshots[1].GoldFineWeight);
    }

    [Fact]
    public void Spot_in_dollari_vengono_convertiti_con_il_cambio_del_giorno()
    {
        var day = TestLots.SingleBuy[0].Date;
        var spots = new[] { new SpotPrice(Metal.Gold, new Money(2200m, Currency.USD), day) };
        var rates = new FxRateSeries([new FxRate(Currency.EUR, Currency.USD, 1.10m, day)]);

        var snapshot = Assert.Single(SnapshotBuilder.Build(TestLots.SingleBuy, spots, SpotValuation.Instance, Currency.EUR, day, day, rates));

        Assert.Equal(Currency.EUR, snapshot.Currency);
        Assert.Equal(2000.01m, Math.Round(snapshot.Value.Amount, 2));
    }

    [Fact]
    public void Spot_in_un_altra_valuta_senza_cambi_danno_un_errore_chiaro()
    {
        var day = TestLots.SingleBuy[0].Date;
        var spots = new[] { new SpotPrice(Metal.Gold, new Money(2200m, Currency.USD), day) };

        Assert.Throws<InvalidOperationException>(
            () => SnapshotBuilder.Build(TestLots.SingleBuy, spots, SpotValuation.Instance, Currency.EUR, day, day));
    }

    [Fact]
    public void Se_manca_lo_spot_di_un_metallo_posseduto_l_errore_dice_quale_e_quando()
    {
        var day = TestLots.SingleBuy[0].Date;
        var spots = new[] { GoldEur(2000m, day.AddDays(3)) };

        var ex = Assert.Throws<InvalidOperationException>(
            () => SnapshotBuilder.Build(TestLots.SingleBuy, spots, SpotValuation.Instance, Currency.EUR, day, day.AddDays(5)));

        Assert.Contains("Gold", ex.Message);
        Assert.Contains(day.ToString("yyyy-MM-dd"), ex.Message);
    }

    [Fact]
    public void Date_invertite_vengono_rifiutate()
    {
        var day = new DateOnly(2025, 1, 10);

        Assert.Throws<ArgumentException>(
            () => SnapshotBuilder.Build([], [], SpotValuation.Instance, Currency.EUR, day, day.AddDays(-1)));
    }

    [Fact]
    public void Un_anno_con_50_lotti_si_ricostruisce_in_meno_di_un_secondo()
    {
        var start = new DateOnly(2025, 1, 1);
        var lots = Enumerable.Range(0, 50)
            .Select(i => Lot.Buy(i % 2 == 0 ? TestProducts.Krugerrand1Oz : TestProducts.SilverMapleLeaf1Oz,
                1m, Eur(100m), Eur(0m), start.AddDays(i * 7)))
            .ToList();
        var spots = Enumerable.Range(0, 365).SelectMany(d => new[]
        {
            GoldEur(2500m + d, start.AddDays(d)),
            new SpotPrice(Metal.Silver, Eur(30m), start.AddDays(d)),
        });

        var watch = Stopwatch.StartNew();
        var snapshots = SnapshotBuilder.Build(lots, spots, SpotValuation.Instance, Currency.EUR, start, start.AddDays(364));
        watch.Stop();

        Assert.Equal(365, snapshots.Count);
        Assert.True(watch.ElapsedMilliseconds < 1000, $"Tempo: {watch.ElapsedMilliseconds} ms");
    }
}