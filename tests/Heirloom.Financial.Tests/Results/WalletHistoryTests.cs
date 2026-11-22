using Heirloom.Financial.Domain;
using Heirloom.Financial.Positions;
using Heirloom.Financial.Results;
using Heirloom.Financial.Risk;
using Heirloom.Financial.Valuation;
using static Heirloom.Financial.Tests.TestData.OneOunce;

namespace Heirloom.Financial.Tests.Results;

/// <summary>
/// Lo storico giorno per giorno: costo di carico, guadagno e valori per metallo, calcolati a mano.
/// <list type="bullet">
/// <item>1/1/2024: lotto A, 2 monete d'oro da 1 oz per 4.000 €; lotto B, 1 moneta ereditata;
/// lotto S, lingotto d'argento da 10 oz a 250 €. Oro 2.000 €/oz, argento 25 €/oz.</item>
/// <item>1/7/2024: oro a 2.200 €; si vende 1 moneta del lotto A a 2.200 €.</item>
/// <item>1/1/2025: oro a 2.500 €, argento a 30 €.</item>
/// </list>
/// </summary>
public class WalletHistoryTests
{
    private static readonly DateOnly Start = new(2024, 1, 1);
    private static readonly DateOnly SaleDay = new(2024, 7, 1);
    private static readonly DateOnly End = new(2025, 1, 1);

    private static readonly Lot[] Lots =
    [
        Lot.BuyForTotal(GoldCoin, 2m, Eur(4000m), Eur(0m), Start) with { Id = "A" },
        Lot.BuyWithUnknownCost(GoldCoin, 1m, Start, Currency.EUR) with { Id = "B" },
        Lot.Buy(SilverBar, 1m, Eur(250m), Eur(0m), Start) with { Id = "S" },
        Lot.Sell(GoldCoin, 1m, Eur(2200m), Eur(0m), SaleDay) with { MatchedLotId = "A" },
    ];

    private static readonly SpotPrice[] Spots =
    [
        GoldSpot(2000m, Start), GoldSpot(2200m, SaleDay), GoldSpot(2500m, End),
        SilverSpot(25m, Start), SilverSpot(30m, End),
    ];

    private static WalletAnalysisOptions Options(DateOnly? from = null) => new()
    {
        Currency = Currency.EUR,
        ValuationDate = End,
        Valuation = SpotValuation.Instance,
        CostBasisMethod = CostBasisMethod.SpecificLot,
        AnnualizationBasis = AnnualizationBasis.CalendarDays365,
        HistoryFrom = from,
    };

    private static WalletSummary Analyze(DateOnly? from = null) => WalletAnalyzer.Analyze(Lots, Spots, Options(from));

    private static HistoryPoint On(WalletSummary summary, DateOnly day) =>
        Assert.Single(summary.Performance!.History, p => p.Date == day);

    [Fact]
    public void Il_primo_giorno_il_guadagno_e_zero_e_l_eredita_non_e_nel_costo()
    {
        var day = On(Analyze(), Start);

        Assert.Equal(6250m, day.Value);
        Assert.Equal(4250m, day.CostBasis);
        Assert.Equal(0m, day.UnrealizedPnL);
        Assert.Equal(0m, day.UnrealizedPercent);
    }

    [Fact]
    public void Il_primo_giorno_ogni_metallo_ha_valore_costo_e_spot()
    {
        var metals = On(Analyze(), Start).Metals;

        Assert.Equal(new[] { Metal.Gold, Metal.Silver }, metals.Select(m => m.Metal));
        Assert.Equal(new MetalHistoryValue(Metal.Gold, 6000m, 4000m, 64.3015m), metals[0]);
        Assert.Equal(new MetalHistoryValue(Metal.Silver, 250m, 250m, 0.8038m), metals[1]);
    }

    [Fact]
    public void Il_giorno_della_vendita_il_costo_scende_e_compare_il_guadagno()
    {
        var day = On(Analyze(), SaleDay);

        // Restano A (1 moneta, 2.000 €), B (ereditata) e S (250 €); oro a 2.200 €.
        Assert.Equal(2250m, day.CostBasis);
        Assert.Equal(200m, day.UnrealizedPnL);
        Assert.Equal(0.0889m, day.UnrealizedPercent);
        Assert.Equal(4400m, day.Metals[0].Value);
        Assert.Equal(2000m, day.Metals[0].CostBasis);
    }

    [Fact]
    public void Il_giorno_prima_della_vendita_vale_ancora_la_situazione_iniziale()
    {
        var day = On(Analyze(), SaleDay.AddDays(-1));

        Assert.Equal(4250m, day.CostBasis);
        Assert.Equal(6000m, day.Metals[0].Value);
    }

    [Fact]
    public void L_ultimo_giorno_coincide_con_il_riepilogo()
    {
        var summary = Analyze();
        var last = summary.Performance!.History[^1];

        Assert.Equal(End, last.Date);
        Assert.Equal(summary.TotalValue.Amount, last.Value);
        Assert.Equal(summary.TotalCostBasis.Amount, last.CostBasis);
        Assert.Equal(summary.UnrealizedPnL.Amount, last.UnrealizedPnL);
        Assert.Equal(summary.UnrealizedPercent, last.UnrealizedPercent);

        foreach (var metal in summary.ByMetal)
        {
            var point = Assert.Single(last.Metals, m => m.Metal == metal.Metal);
            Assert.Equal(metal.Value.Amount, point.Value);
            Assert.Equal(metal.CostBasis.Amount, point.CostBasis);
            Assert.Equal(metal.SpotPerFineGram, point.SpotPerFineGram);
        }
    }

    [Fact]
    public void Ogni_giorno_il_valore_e_la_somma_dei_metalli()
    {
        Assert.All(Analyze().Performance!.History, p => Assert.Equal(p.Value, p.Metals.Sum(m => m.Value)));
    }

    [Fact]
    public void Lo_storico_puo_partire_dopo_le_operazioni()
    {
        var from = new DateOnly(2024, 12, 1);
        var history = Analyze(from).Performance!.History;

        Assert.Equal(from, history[0].Date);
        Assert.Equal(2250m, history[0].CostBasis);
        Assert.Equal(2, history[0].Metals.Count);
    }

    [Fact]
    public void Senza_costo_noto_la_percentuale_e_assente()
    {
        var inherited = new[] { Lot.BuyWithUnknownCost(GoldCoin, 1m, Start, Currency.EUR) with { Id = "H" } };

        var day = WalletAnalyzer.Analyze(inherited, Spots, Options()).Performance!.History[0];

        Assert.Equal(0m, day.CostBasis);
        Assert.Equal(0m, day.UnrealizedPnL);
        Assert.Null(day.UnrealizedPercent);
        Assert.Equal(2000m, day.Value);
    }
}