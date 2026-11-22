using Heirloom.Financial.Domain;
using Heirloom.Financial.Positions;
using Heirloom.Financial.Results;
using Heirloom.Financial.Risk;
using Heirloom.Financial.Taxation;
using Heirloom.Financial.Valuation;
using static Heirloom.Financial.Tests.TestData.OneOunce;

namespace Heirloom.Financial.Tests.Results;

/// <summary>
/// Lotti aperti, metalli, flusso di cassa e tasse nel riepilogo. Stesso portafoglio di WalletAnalyzerTests,
/// con la fattura sul lotto A:
/// <list type="bullet">
/// <item>A: 2 monete d'oro da 1 oz pagate 4.000 € (con fattura), 1 venduta a 2.200 €;</item>
/// <item>B: 1 moneta ereditata; S: lingotto d'argento da 10 oz a 250 € (senza fattura).</item>
/// <item>Valutazione: oro a 2.500 €/oz, argento a 30 €/oz.</item>
/// </list>
/// </summary>
public class WalletAnalyzerTaxAndLotsTests
{
    private static readonly DateOnly Start = new(2024, 1, 1);
    private static readonly DateOnly End = new(2025, 1, 1);

    private static readonly Lot[] Lots =
    [
        Lot.BuyForTotal(GoldCoin, 2m, Eur(4000m), Eur(0m), Start) with { Id = "A", HasDocumentedCost = true },
        Lot.BuyWithUnknownCost(GoldCoin, 1m, Start, Currency.EUR) with { Id = "B" },
        Lot.Buy(SilverBar, 1m, Eur(250m), Eur(0m), Start) with { Id = "S" },
        Lot.Sell(GoldCoin, 1m, Eur(2200m), Eur(0m), new DateOnly(2024, 7, 1)) with { MatchedLotId = "A" },
    ];

    private static readonly SpotPrice[] Spots =
    [
        GoldSpot(2000m, Start), GoldSpot(2500m, End), SilverSpot(25m, Start), SilverSpot(30m, End),
    ];

    private static WalletAnalysisOptions Options(ITaxPolicy? policy = null) => new()
    {
        Currency = Currency.EUR,
        ValuationDate = End,
        Valuation = SpotValuation.Instance,
        CostBasisMethod = CostBasisMethod.SpecificLot,
        AnnualizationBasis = AnnualizationBasis.CalendarDays365,
        IncludeHistory = false,
        TaxPolicy = policy,
    };

    private static MoneyResult EurResult(decimal amount) => new(amount, "EUR");

    private static WalletSummary Analyze(ITaxPolicy? policy = null) => WalletAnalyzer.Analyze(Lots, Spots, Options(policy));

    // ---------- Lotti ----------

    [Fact]
    public void Ogni_lotto_aperto_ha_valore_costo_e_guadagno()
    {
        var lots = Analyze().Lots;

        Assert.Equal(3, lots.Count);

        var a = Assert.Single(lots, l => l.LotId == "A");
        Assert.Equal("Moneta oro 1 oz", a.Product);
        Assert.Equal(Metal.Gold, a.Metal);
        Assert.Equal(ProductForm.Coin, a.Form);
        Assert.Equal(Start, a.PurchaseDate);
        Assert.Equal(1m, a.Quantity);
        Assert.Equal(EurResult(2500m), a.Value);
        Assert.Equal(EurResult(2000m), a.Cost);
        Assert.Equal(EurResult(500m), a.UnrealizedPnL);
        Assert.Equal(0.25m, a.UnrealizedPercent);
        Assert.Equal(0.4717m, a.ValueShare);
        Assert.True(a.HasDocumentedCost);

        var b = Assert.Single(lots, l => l.LotId == "B");
        Assert.Null(b.Cost);
        Assert.Null(b.UnrealizedPnL);
        Assert.Null(b.UnrealizedPercent);

        var s = Assert.Single(lots, l => l.LotId == "S");
        Assert.Equal(EurResult(300m), s.Value);
        Assert.Equal(0.2m, s.UnrealizedPercent);
        Assert.Equal(0.0566m, s.ValueShare);
        Assert.Equal(311.0348m, s.FineWeightGrams);
    }

    [Fact]
    public void Senza_regola_fiscale_niente_tasse()
    {
        var summary = Analyze();

        Assert.Null(summary.EstimatedTax);
        Assert.Null(summary.ValueAfterTax);
        Assert.Null(summary.UnrealizedPnLAfterTax);
        Assert.All(summary.Lots, l => Assert.Null(l.EstimatedTax));
    }

    // ---------- Tasse ----------

    [Fact]
    public void Le_tasse_si_stimano_lotto_per_lotto()
    {
        var summary = Analyze(ItalianTaxPolicy.Standard);

        // A con fattura: 26% di 500. B ereditato: 26% di 2.500. S senza fattura: 26% di 300.
        Assert.Equal(EurResult(130m), Assert.Single(summary.Lots, l => l.LotId == "A").EstimatedTax);
        Assert.Equal(EurResult(650m), Assert.Single(summary.Lots, l => l.LotId == "B").EstimatedTax);
        Assert.Equal(EurResult(78m), Assert.Single(summary.Lots, l => l.LotId == "S").EstimatedTax);

        Assert.Equal(EurResult(858m), summary.EstimatedTax);
        Assert.Equal(EurResult(4442m), summary.ValueAfterTax);

        // Guadagno netto dei lotti a costo noto: A 2.500 − 130 − 2.000; S 300 − 78 − 250.
        Assert.Equal(EurResult(342m), summary.UnrealizedPnLAfterTax);
    }

    [Fact]
    public void Le_tasse_con_la_media_ponderata_vengono_rifiutate()
    {
        var options = Options(ItalianTaxPolicy.Standard) with { CostBasisMethod = CostBasisMethod.WeightedAverage };

        Assert.Throws<ArgumentException>(() => WalletAnalyzer.Analyze(Lots, Spots, options));
    }

    [Fact]
    public void Con_la_media_ponderata_senza_tasse_i_lotti_sono_vuoti()
    {
        var summary = WalletAnalyzer.Analyze(Lots.Select(l => l with { MatchedLotId = null }), Spots,
            Options() with { CostBasisMethod = CostBasisMethod.WeightedAverage });

        Assert.Empty(summary.Lots);
        Assert.NotEmpty(summary.Positions);
    }

    // ---------- Metalli ----------

    [Fact]
    public void Il_riepilogo_per_metallo_coincide_con_il_calcolo_a_mano()
    {
        var byMetal = Analyze().ByMetal;

        Assert.Equal(new[] { Metal.Gold, Metal.Silver }, byMetal.Select(m => m.Metal));

        var gold = byMetal[0];
        Assert.Equal(67.86m, gold.GrossWeightGrams);
        Assert.Equal(62.2070m, gold.FineWeightGrams);
        Assert.Equal(2m, gold.TroyOunces);
        Assert.Equal(EurResult(5000m), gold.Value);
        Assert.Equal(EurResult(2000m), gold.CostBasis);
        Assert.Equal(64.3015m, gold.AverageCostPerFineGram);
        Assert.Equal(80.3769m, gold.SpotPerFineGram);
        Assert.Equal(1m, gold.CoinWeightShare);
        Assert.Equal(0m, gold.BarWeightShare);

        var silver = byMetal[1];
        Assert.Equal(311.1m, silver.GrossWeightGrams);
        Assert.Equal(10m, silver.TroyOunces);
        Assert.Equal(0.8038m, silver.AverageCostPerFineGram);
        Assert.Equal(0.9645m, silver.SpotPerFineGram);
        Assert.Equal(0m, silver.CoinWeightShare);
        Assert.Equal(1m, silver.BarWeightShare);
    }

    [Fact]
    public void Un_metallo_tutto_ereditato_non_ha_costo_medio()
    {
        var summary = WalletAnalyzer.Analyze(
            [Lot.BuyWithUnknownCost(GoldCoin, 1m, Start, Currency.EUR) with { Id = "H" }], Spots, Options());

        Assert.Null(Assert.Single(summary.ByMetal).AverageCostPerFineGram);
    }

    // ---------- Flusso di cassa ----------

    [Fact]
    public void Il_flusso_di_cassa_somma_i_totali()
    {
        var cashFlow = Analyze().CashFlow;

        Assert.Equal(EurResult(4250m), cashFlow.TotalPurchases);
        Assert.Equal(3, cashFlow.PurchaseCount);
        Assert.Equal(EurResult(2200m), cashFlow.TotalSales);
        Assert.Equal(1, cashFlow.SalesCount);
        Assert.Equal(EurResult(-2050m), cashFlow.NetCashFlow);
    }

    [Fact]
    public void Il_flusso_di_cassa_include_i_costi_accessori()
    {
        var lots = new[]
        {
            Lot.Buy(GoldCoin, 1m, Eur(2000m), Eur(30m), Start) with { Id = "X" },
            Lot.Sell(GoldCoin, 1m, Eur(2500m), Eur(20m), End) with { MatchedLotId = "X" },
        };

        var cashFlow = WalletAnalyzer.Analyze(lots, Spots, Options()).CashFlow;

        Assert.Equal(EurResult(2030m), cashFlow.TotalPurchases);
        Assert.Equal(EurResult(2480m), cashFlow.TotalSales);
        Assert.Equal(EurResult(450m), cashFlow.NetCashFlow);
    }

    [Fact]
    public void Portafoglio_vuoto_niente_lotti_niente_metalli_flusso_a_zero()
    {
        var summary = WalletAnalyzer.Analyze([], Spots, Options(ItalianTaxPolicy.Standard));

        Assert.Empty(summary.Lots);
        Assert.Empty(summary.ByMetal);
        Assert.Equal(EurResult(0m), summary.CashFlow.NetCashFlow);
        Assert.Equal(EurResult(0m), summary.EstimatedTax);
    }
}