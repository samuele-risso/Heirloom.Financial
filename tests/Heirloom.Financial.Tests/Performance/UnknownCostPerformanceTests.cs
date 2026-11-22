using Heirloom.Financial.Domain;
using Heirloom.Financial.Exchange;
using Heirloom.Financial.Performance;
using Heirloom.Financial.Valuation;
using static Heirloom.Financial.Tests.TestData.OneOunce;

namespace Heirloom.Financial.Tests.Performance;

/// <summary>
/// Nei rendimenti, un lotto a costo sconosciuto entra come conferimento al valore di mercato del suo giorno.
/// </summary>
public class UnknownCostPerformanceTests
{
    private static readonly DateOnly Start = new(2024, 1, 1);

    // ---------- SnapshotBuilder ----------

    [Fact]
    public void Lo_storico_conta_i_pezzi_ereditati_come_capitale_al_valore_del_giorno()
    {
        var inherited = Lot.BuyWithUnknownCost(GoldCoin, 1m, Start, Currency.EUR);
        var spots = new[] { GoldSpot(2000m, Start), GoldSpot(2200m, Start.AddDays(5)) };

        var snapshots = SnapshotBuilder.Build([inherited], spots, SpotValuation.Instance, Currency.EUR, Start, Start.AddDays(9));

        Assert.All(snapshots, s => Assert.Equal(Eur(2000m), s.InvestedCapital));
        Assert.Equal(Eur(2000m), snapshots[0].Value);
        Assert.Equal(Eur(2200m), snapshots[^1].Value);
        Assert.Equal(Eur(200m), snapshots[^1].UnrealizedPnL);
    }

    [Fact]
    public void L_arrivo_di_un_eredita_non_sembra_un_guadagno()
    {
        var bought = Lot.Buy(GoldCoin, 1m, Eur(2000m), Eur(0m), Start);
        var inherited = Lot.BuyWithUnknownCost(GoldCoin, 3m, Start.AddDays(10), Currency.EUR);
        var spots = new[] { GoldSpot(2000m, Start) };

        var snapshots = SnapshotBuilder.Build([bought, inherited], spots, SpotValuation.Instance, Currency.EUR, Start, Start.AddDays(20));

        Assert.Equal(Eur(2000m), snapshots[9].InvestedCapital);
        Assert.Equal(Eur(8000m), snapshots[10].InvestedCapital);
        Assert.Equal(Eur(8000m), snapshots[10].Value);
        Assert.Equal(0m, TimeWeightedReturn.Calculate(snapshots));
    }

    [Fact]
    public void Un_eredita_precedente_al_periodo_usa_lo_spot_del_suo_giorno()
    {
        var inherited = Lot.BuyWithUnknownCost(GoldCoin, 1m, Start, Currency.EUR);
        var spots = new[] { GoldSpot(1800m, Start), GoldSpot(2100m, Start.AddDays(30)) };

        var snapshots = SnapshotBuilder.Build([inherited], spots, SpotValuation.Instance, Currency.EUR, Start.AddDays(30), Start.AddDays(31));

        Assert.All(snapshots, s => Assert.Equal(Eur(1800m), s.InvestedCapital));
        Assert.All(snapshots, s => Assert.Equal(Eur(2100m), s.Value));
    }

    [Fact]
    public void Senza_spot_nel_giorno_dell_eredita_lo_storico_si_ferma_con_un_errore()
    {
        var inherited = Lot.BuyWithUnknownCost(GoldCoin, 1m, Start, Currency.EUR);
        var spots = new[] { GoldSpot(2000m, Start.AddDays(3)) };

        Assert.Throws<InvalidOperationException>(() =>
            SnapshotBuilder.Build([inherited], spots, SpotValuation.Instance, Currency.EUR, Start.AddDays(3), Start.AddDays(4)));
    }

    [Fact]
    public void Lo_storico_converte_il_valore_dell_eredita_con_il_cambio_del_giorno()
    {
        var inherited = Lot.BuyWithUnknownCost(GoldCoin, 1m, Start, Currency.EUR);
        var spots = new[] { new SpotPrice(Metal.Gold, new Money(2200m, Currency.USD), Start) };
        var rates = new FxRateSeries([new FxRate(Currency.EUR, Currency.USD, 1.10m, Start)]);

        var snapshots = SnapshotBuilder.Build([inherited], spots, SpotValuation.Instance, Currency.EUR, Start, Start, rates);

        Assert.Equal(Eur(2000m), Assert.Single(snapshots).InvestedCapital);
    }

    // ---------- XIRR ----------

    [Fact]
    public void I_flussi_senza_spot_rifiutano_i_lotti_a_costo_sconosciuto()
    {
        var inherited = Lot.BuyWithUnknownCost(GoldCoin, 1m, Start, Currency.EUR);

        var ex = Assert.Throws<ArgumentException>(() => Xirr.CashFlowsFrom([inherited], Eur(2200m), Start.AddDays(365)));
        Assert.Contains("costo sconosciuto", ex.Message);
    }

    [Fact]
    public void I_flussi_con_gli_spot_valutano_l_eredita_al_prezzo_del_suo_giorno()
    {
        var inherited = Lot.BuyWithUnknownCost(GoldCoin, 2m, Start, Currency.EUR);
        var end = Start.AddDays(365);

        var flows = Xirr.CashFlowsFrom([inherited], Eur(4400m), end, [GoldSpot(2000m, Start)], SpotValuation.Instance);

        Assert.Equal(new[] { new CashFlow(Start, -4000m), new CashFlow(end, 4400m) }, flows);
        Assert.Equal(0.10, Xirr.Calculate(flows).Rate!.Value, 6);
    }

    [Fact]
    public void I_flussi_con_gli_spot_lasciano_invariati_i_lotti_a_costo_noto()
    {
        var bought = Lot.Buy(GoldCoin, 1m, Eur(1900m), Eur(10m), Start);
        var sold = Lot.Sell(GoldCoin, 1m, Eur(2100m), Eur(5m), Start.AddDays(100));
        var end = Start.AddDays(200);

        var withSpots = Xirr.CashFlowsFrom([bought, sold], Eur(0m), end, [GoldSpot(2000m, Start)], SpotValuation.Instance);
        var withoutSpots = Xirr.CashFlowsFrom([bought, sold], Eur(0m), end);

        Assert.Equal(withoutSpots, withSpots);
    }

    [Fact]
    public void I_flussi_con_gli_spot_convertono_con_il_cambio()
    {
        var inherited = Lot.BuyWithUnknownCost(GoldCoin, 1m, Start, Currency.EUR);
        var spots = new[] { new SpotPrice(Metal.Gold, new Money(2200m, Currency.USD), Start) };
        var rates = new FxRateSeries([new FxRate(Currency.EUR, Currency.USD, 1.10m, Start)]);

        var flows = Xirr.CashFlowsFrom([inherited], Eur(2100m), Start.AddDays(30), spots, SpotValuation.Instance, rates);

        Assert.Equal(-2000m, flows[0].Amount);
    }

    [Fact]
    public void I_flussi_con_gli_spot_senza_cambio_danno_un_errore_chiaro()
    {
        var inherited = Lot.BuyWithUnknownCost(GoldCoin, 1m, Start, Currency.EUR);
        var spots = new[] { new SpotPrice(Metal.Gold, new Money(2200m, Currency.USD), Start) };

        Assert.Throws<InvalidOperationException>(() =>
            Xirr.CashFlowsFrom([inherited], Eur(2100m), Start.AddDays(30), spots, SpotValuation.Instance));
    }
}