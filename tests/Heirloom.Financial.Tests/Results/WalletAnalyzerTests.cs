using Heirloom.Financial.Domain;
using Heirloom.Financial.Exchange;
using Heirloom.Financial.Positions;
using Heirloom.Financial.Results;
using Heirloom.Financial.Risk;
using Heirloom.Financial.Valuation;
using static Heirloom.Financial.Tests.TestData.OneOunce;

namespace Heirloom.Financial.Tests.Results;

/// <summary>
/// Il portafoglio di prova, con i risultati calcolati a mano:
/// <list type="bullet">
/// <item>1/1/2024: 2 monete d'oro da 1 oz pagate 4.000 € in tutto (lotto A), 1 moneta ereditata (lotto B),
/// 1 lingotto d'argento da 10 oz a 250 € (lotto S). Oro a 2.000 €/oz, argento a 25 €/oz.</item>
/// <item>1/7/2024: oro a 2.200; si vende 1 moneta del lotto A a 2.200 € (guadagno 200 €).</item>
/// <item>1/1/2025 (valutazione): oro a 2.500, argento a 30.</item>
/// </list>
/// </summary>
public class PortfolioAnalyzerTests
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

    private static WalletAnalysisOptions Options(DateOnly? valuationDate = null) => new()
    {
        Currency = Currency.EUR,
        ValuationDate = valuationDate ?? End,
        Valuation = SpotValuation.Instance,
        CostBasisMethod = CostBasisMethod.SpecificLot,
        AnnualizationBasis = AnnualizationBasis.CalendarDays365,
        RiskFreeRate = 0.03m,
    };

    private static MoneyResult EurResult(decimal amount) => new(amount, "EUR");

    private static WalletSummary Analyze() => WalletAnalyzer.Analyze(Lots, Spots, Options());

    // ---------- Totali ----------

    [Fact]
    public void I_totali_coincidono_con_il_calcolo_a_mano()
    {
        var summary = Analyze();

        Assert.Equal("EUR", summary.Currency);
        Assert.Equal(End, summary.ValuationDate);
        Assert.Equal(CostBasisMethod.SpecificLot, summary.CostBasisMethod);
        Assert.Equal(EurResult(5300m), summary.TotalValue);
        Assert.Equal(EurResult(2800m), summary.KnownCostValue);
        Assert.Equal(EurResult(2500m), summary.UnknownCostValue);
        Assert.Equal(EurResult(2250m), summary.TotalCostBasis);
        Assert.Equal(EurResult(550m), summary.UnrealizedPnL);
        Assert.Equal(0.2444m, summary.UnrealizedPercent);
        Assert.Equal(EurResult(200m), summary.RealizedPnL);
        Assert.Equal(EurResult(0m), summary.UnknownCostProceeds);
        Assert.Equal(EurResult(750m), summary.TotalPnL);
        Assert.Equal(62.2070m, summary.GoldFineWeightGrams);
        Assert.Equal(311.0348m, summary.SilverFineWeightGrams);
        Assert.Empty(summary.Notes);
    }

    [Fact]
    public void Le_posizioni_coincidono_con_il_calcolo_a_mano()
    {
        var positions = Analyze().Positions;

        Assert.Equal(2, positions.Count);

        var gold = positions[0];
        Assert.Equal("Moneta oro 1 oz", gold.Product);
        Assert.Equal(Metal.Gold, gold.Metal);
        Assert.Equal(ProductForm.Coin, gold.Form);
        Assert.Equal(2m, gold.Quantity);
        Assert.Equal(1m, gold.UnknownCostQuantity);
        Assert.Equal(EurResult(5000m), gold.Value);
        Assert.Equal(EurResult(2000m), gold.CostBasis);
        Assert.Equal(EurResult(2000m), gold.AverageCost);
        Assert.Equal(EurResult(500m), gold.UnrealizedPnL);
        Assert.Equal(0.25m, gold.UnrealizedPercent);

        var silver = positions[1];
        Assert.Equal(Metal.Silver, silver.Metal);
        Assert.Equal(EurResult(300m), silver.Value);
        Assert.Equal(EurResult(50m), silver.UnrealizedPnL);
        Assert.Equal(0.2m, silver.UnrealizedPercent);
    }

    [Fact]
    public void La_ripartizione_somma_a_uno()
    {
        var summary = Analyze();

        Assert.Equal(new[] { 0.9434m, 0.0566m }, summary.AllocationByMetal.Select(s => s.Percent!.Value).ToArray());
        Assert.Equal(Metal.Gold, summary.AllocationByMetal[0].Metal);
        Assert.Null(summary.AllocationByMetal[0].Form);

        Assert.Equal(new[] { 0.9434m, 0.0566m }, summary.AllocationByForm.Select(s => s.Percent!.Value).ToArray());
        Assert.Equal(ProductForm.Coin, summary.AllocationByForm[0].Form);
        Assert.Equal(EurResult(300m), summary.AllocationByForm[1].Value);
    }

    // ---------- Rendimenti ----------

    [Fact]
    public void Lo_storico_ha_un_punto_per_giorno_e_il_capitale_giusto()
    {
        var performance = Analyze().Performance!;

        Assert.Equal(Start, performance.From);
        Assert.Equal(End, performance.To);
        Assert.Equal(367, performance.History.Count);

        // Il capitale iniziale comprende la moneta ereditata, al prezzo di quel giorno.
        Assert.Equal(6250m, performance.History[0].InvestedCapital);
        Assert.Equal(6250m, performance.History[0].Value);
        Assert.Equal(1m, performance.History[0].Index);

        Assert.Equal(4050m, performance.History[^1].InvestedCapital);
        Assert.Equal(5300m, performance.History[^1].Value);
    }

    [Fact]
    public void Twr_e_xirr_coincidono_con_il_calcolo_a_mano()
    {
        var performance = Analyze().Performance!;

        // TWR: (6.850 / 6.250) × (5.300 / 4.650) − 1 = 0,24920...
        Assert.Equal(0.2492m, performance.TimeWeightedReturn);
        Assert.Equal(1.2492m, performance.History[^1].Index);

        // XIRR dei flussi −6.250 (1/1/24), +2.200 (1/7/24), +5.300 (1/1/25), calcolato con Python.
        Assert.Equal(0.2395m, performance.MoneyWeightedReturn);

        // Il periodo è di 366 giorni: l'annualizzato esiste ed è quasi uguale al totale.
        Assert.NotNull(performance.AnnualizedReturn);
        Assert.True(Math.Abs(performance.AnnualizedReturn!.Value - 0.2492m) < 0.002m);
    }

    [Fact]
    public void Il_rischio_coincide_con_il_calcolo_a_mano()
    {
        var risk = Analyze().Risk!;

        // Deviazione standard dei 366 rendimenti giornalieri × √365, calcolata con Python.
        Assert.Equal(AnnualizationBasis.CalendarDays365, risk.AnnualizationBasis);
        Assert.Equal(0.1691m, risk.Volatility);

        // Il valore TWR non scende mai: nessun drawdown.
        Assert.Null(risk.MaxDrawdown);

        // Sharpe = (annualizzato − 3%) / volatilità.
        Assert.NotNull(risk.Sharpe);
        Assert.True(Math.Abs(risk.Sharpe!.Value - 1.2963m) < 0.02m);
    }

    [Fact]
    public void Senza_tasso_privo_di_rischio_niente_sharpe()
    {
        var summary = WalletAnalyzer.Analyze(Lots, Spots, Options() with { RiskFreeRate = null });

        Assert.Null(summary.Risk!.Sharpe);
    }

    [Fact]
    public void Un_calo_produce_il_drawdown()
    {
        var spots = Spots.Append(GoldSpot(1500m, new DateOnly(2024, 10, 1))).ToArray();

        var drawdown = WalletAnalyzer.Analyze(Lots, spots, Options()).Risk!.MaxDrawdown!;

        Assert.Equal(SaleDay, drawdown.PeakDate);
        Assert.Equal(new DateOnly(2024, 10, 1), drawdown.TroughDate);
        Assert.True(drawdown.Depth < 0m);
        Assert.Equal(End, drawdown.RecoveryDate);
    }

    [Fact]
    public void Lo_storico_si_puo_escludere()
    {
        var summary = WalletAnalyzer.Analyze(Lots, Spots, Options() with { IncludeHistory = false });

        Assert.Empty(summary.Performance!.History);
        Assert.Equal(0.2492m, summary.Performance.TimeWeightedReturn);
    }

    [Fact]
    public void Lo_storico_puo_partire_piu_tardi()
    {
        var summary = WalletAnalyzer.Analyze(Lots, Spots, Options() with { HistoryFrom = new DateOnly(2024, 12, 1) });

        Assert.Equal(new DateOnly(2024, 12, 1), summary.Performance!.From);
        Assert.Equal(32, summary.Performance.History.Count);
        Assert.Null(summary.Performance.AnnualizedReturn);
        Assert.Null(summary.Risk!.Sharpe);
    }

    [Fact]
    public void Un_inizio_dello_storico_dopo_la_valutazione_viene_rifiutato()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            WalletAnalyzer.Analyze(Lots, Spots, Options() with { HistoryFrom = End.AddDays(1) }));
    }

    // ---------- Casi limite ----------

    [Fact]
    public void Portafoglio_vuoto_tutto_a_zero_senza_rendimenti()
    {
        var summary = WalletAnalyzer.Analyze([], Spots, Options());

        Assert.Equal(EurResult(0m), summary.TotalValue);
        Assert.Empty(summary.Positions);
        Assert.All(summary.AllocationByMetal, s => Assert.Null(s.Percent));
        Assert.Null(summary.UnrealizedPercent);
        Assert.Null(summary.Performance);
        Assert.Null(summary.Risk);
        Assert.Single(summary.Notes);
    }

    [Fact]
    public void Senza_prezzi_storici_il_riepilogo_arriva_senza_rendimenti()
    {
        var onlyToday = new[] { GoldSpot(2500m, End), SilverSpot(30m, End) };

        var summary = WalletAnalyzer.Analyze(Lots, onlyToday, Options());

        Assert.Equal(EurResult(5300m), summary.TotalValue);
        Assert.Null(summary.Performance);
        Assert.Null(summary.Risk);
        Assert.Contains(summary.Notes, n => n.Contains("non disponibili"));
    }

    [Fact]
    public void Storico_troppo_corto_niente_rischio()
    {
        var lot = Lot.Buy(GoldCoin, 1m, Eur(2500m), Eur(0m), End);

        var summary = WalletAnalyzer.Analyze([lot], Spots, Options() with { CostBasisMethod = CostBasisMethod.Fifo });

        Assert.NotNull(summary.Performance);
        Assert.Null(summary.Risk);
        Assert.Null(summary.Performance!.AnnualizedReturn);
    }

    [Fact]
    public void Senza_spot_del_giorno_per_un_metallo_posseduto_si_ferma()
    {
        var goldOnly = Spots.Where(s => s.Metal == Metal.Gold);

        Assert.Throws<InvalidOperationException>(() => WalletAnalyzer.Analyze(Lots, goldOnly, Options()));
    }

    [Fact]
    public void Un_lotto_dopo_la_data_di_valutazione_viene_rifiutato()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            WalletAnalyzer.Analyze(Lots, Spots, Options(valuationDate: SaleDay.AddDays(-1))));
    }

    [Fact]
    public void Opzioni_non_definite_vengono_rifiutate()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            WalletAnalyzer.Analyze(Lots, Spots, Options() with { CostBasisMethod = (CostBasisMethod)0 }));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            WalletAnalyzer.Analyze(Lots, Spots, Options() with { AnnualizationBasis = (AnnualizationBasis)0 }));
    }

    // ---------- Valute ----------

    [Fact]
    public void Lotti_in_dollari_senza_cambi_vengono_rifiutati()
    {
        var usd = Lot.Buy(GoldCoin, 1m, new Money(2200m, Currency.USD), new Money(0m, Currency.USD), Start);

        Assert.Throws<ArgumentException>(() => WalletAnalyzer.Analyze([usd], Spots, Options()));
    }

    [Fact]
    public void Lotti_e_spot_in_dollari_vengono_convertiti()
    {
        var rates = new FxRateSeries([new FxRate(Currency.EUR, Currency.USD, 1.10m, Start), new FxRate(Currency.EUR, Currency.USD, 1.25m, End)]);
        var usd = Lot.Buy(GoldCoin, 1m, new Money(2200m, Currency.USD), new Money(0m, Currency.USD), Start);
        var spots = new[]
        {
            new SpotPrice(Metal.Gold, new Money(2200m, Currency.USD), Start),
            new SpotPrice(Metal.Gold, new Money(2500m, Currency.USD), End),
        };

        var summary = WalletAnalyzer.Analyze([usd], spots, Options() with { CostBasisMethod = CostBasisMethod.Fifo }, rates);

        // Costo con il cambio dell'acquisto (2.200 / 1,10), valore con il cambio di oggi (2.500 / 1,25).
        Assert.Equal(EurResult(2000m), summary.TotalCostBasis);
        Assert.Equal(EurResult(2000m), summary.TotalValue);
        Assert.Equal(0m, summary.Performance!.TimeWeightedReturn);
    }

    [Fact]
    public void Spot_in_dollari_senza_cambi_vengono_rifiutati()
    {
        var spots = new[] { new SpotPrice(Metal.Gold, new Money(2500m, Currency.USD), End) };
        var lot = Lot.Buy(GoldCoin, 1m, Eur(2000m), Eur(0m), Start);

        Assert.Throws<InvalidOperationException>(() =>
            WalletAnalyzer.Analyze([lot], spots, Options() with { CostBasisMethod = CostBasisMethod.Fifo }));
    }
}