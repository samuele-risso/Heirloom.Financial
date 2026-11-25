using Heirloom.Financial.Domain;
using Heirloom.Financial.Positions;
using Heirloom.Financial.Results;
using Heirloom.Financial.Risk;
using Heirloom.Financial.Taxation;
using Heirloom.Financial.Valuation;
using static Heirloom.Financial.Tests.TestData.OneOunce;

namespace Heirloom.Financial.Tests.Results;

/// <summary>
/// I valori aggiunti nella 1.6.0: valore e guadagno dopo le tasse e peso lordo per lotto,
/// peso lordo e valore unitario per posizione, variazione rispetto a un giorno dello storico.
/// </summary>
public class ResultsAdditionsTests
{
    private static readonly DateOnly Start = new(2024, 1, 1);
    private static readonly DateOnly End = new(2025, 1, 1);

    private static MoneyResult EurResult(decimal amount) => new(amount, "EUR");

    private static WalletSummary Analyze(params Lot[] lots) => WalletAnalyzer.Analyze(
        lots,
        [GoldSpot(2000m, Start), GoldSpot(2500m, End), SilverSpot(25m, Start), SilverSpot(30m, End)],
        new WalletAnalysisOptions
        {
            Currency = Currency.EUR,
            ValuationDate = End,
            Valuation = SpotValuation.Instance,
            CostBasisMethod = CostBasisMethod.SpecificLot,
            AnnualizationBasis = AnnualizationBasis.CalendarDays365,
            TaxPolicy = ItalianTaxPolicy.Standard,
            IncludeHistory = false,
        });

    [Fact]
    public void Lotto_documentato_tasse_sul_guadagno()
    {
        var lot = Analyze(Lot.BuyForTotal(GoldCoin, 2m, Eur(4000m), Eur(0m), Start) with { Id = "A", HasDocumentedCost = true }).Lots.Single();

        // Valore 5.000, costo 4.000, tasse 26% di 1.000.
        Assert.Equal(EurResult(260m), lot.EstimatedTax);
        Assert.Equal(EurResult(4740m), lot.ValueAfterTax);
        Assert.Equal(EurResult(740m), lot.UnrealizedPnLAfterTax);
        Assert.Equal(67.86m, lot.GrossWeightGrams);
    }

    [Fact]
    public void Lotto_non_documentato_tasse_sul_valore()
    {
        var lot = Analyze(Lot.Buy(GoldCoin, 1m, Eur(2000m), Eur(0m), Start) with { Id = "A" }).Lots.Single();

        // Valore 2.500, tasse 26% di 2.500 = 650.
        Assert.Equal(EurResult(1850m), lot.ValueAfterTax);
        Assert.Equal(EurResult(-150m), lot.UnrealizedPnLAfterTax);
    }

    [Fact]
    public void Lotto_a_costo_sconosciuto_niente_guadagno_dopo_le_tasse()
    {
        var lot = Analyze(Lot.BuyWithUnknownCost(GoldCoin, 1m, Start, Currency.EUR) with { Id = "B" }).Lots.Single();

        Assert.Equal(EurResult(1850m), lot.ValueAfterTax);
        Assert.Null(lot.UnrealizedPnLAfterTax);
    }

    [Fact]
    public void Senza_regola_fiscale_niente_valori_dopo_le_tasse()
    {
        var summary = WalletAnalyzer.Analyze(
            [Lot.Buy(GoldCoin, 1m, Eur(2000m), Eur(0m), Start) with { Id = "A" }],
            [GoldSpot(2500m, End)],
            new WalletAnalysisOptions
            {
                Currency = Currency.EUR,
                ValuationDate = End,
                Valuation = SpotValuation.Instance,
                CostBasisMethod = CostBasisMethod.SpecificLot,
                AnnualizationBasis = AnnualizationBasis.CalendarDays365,
                IncludeHistory = false,
            });

        var lot = summary.Lots.Single();
        Assert.Null(lot.ValueAfterTax);
        Assert.Null(lot.UnrealizedPnLAfterTax);
    }

    [Fact]
    public void Posizione_con_peso_lordo_e_valore_unitario()
    {
        var position = Analyze(
            Lot.Buy(GoldCoin, 2m, Eur(4000m), Eur(0m), Start) with { Id = "A" },
            Lot.Buy(GoldCoin, 1m, Eur(2100m), Eur(0m), Start) with { Id = "B" }).Positions.Single();

        Assert.Equal(101.79m, position.GrossWeightGrams);
        Assert.Equal(EurResult(2500m), position.UnitValue);
    }

    private static HistoryPoint Point(DateOnly date, decimal value, decimal pnl) =>
        new(date, value, value - pnl, 1m, value - pnl, pnl, null, []);

    [Fact]
    public void Variazione_dal_giorno_piu_vicino_senza_superarlo()
    {
        var history = new[]
        {
            Point(new DateOnly(2024, 11, 1), 1000m, 100m),
            Point(new DateOnly(2024, 12, 1), 1100m, 200m),
            Point(new DateOnly(2024, 12, 15), 1150m, 250m),
        };

        var change = PeriodChange.Since(history, new DateOnly(2024, 12, 10), 1210m, 160m)!;

        Assert.Equal(new DateOnly(2024, 12, 1), change.From);
        Assert.Equal(110m, change.ValueChange);
        Assert.Equal(0.1m, change.ValueChangePercent);
        Assert.Equal(-40m, change.PnLChange);
        Assert.Equal(-0.2m, change.PnLChangePercent);
    }

    [Fact]
    public void Storico_che_comincia_dopo_si_usa_il_primo_giorno()
    {
        var history = new[] { Point(new DateOnly(2024, 12, 20), 500m, -50m) };

        var change = PeriodChange.Since(history, new DateOnly(2024, 12, 1), 600m, 50m)!;

        Assert.Equal(new DateOnly(2024, 12, 20), change.From);
        Assert.Equal(0.2m, change.ValueChangePercent);
        // Da −50 a +50: +100 su 50 in valore assoluto.
        Assert.Equal(2m, change.PnLChangePercent);
    }

    [Fact]
    public void Valori_di_partenza_a_zero_niente_percentuali()
    {
        var change = PeriodChange.Since([Point(Start, 0m, 0m)], End, 100m, 10m)!;

        Assert.Equal(100m, change.ValueChange);
        Assert.Null(change.ValueChangePercent);
        Assert.Null(change.PnLChangePercent);
    }

    [Fact]
    public void Storico_vuoto_o_null()
    {
        Assert.Null(PeriodChange.Since([], End, 1m, 1m));
        Assert.Throws<ArgumentNullException>(() => PeriodChange.Since(null!, End, 1m, 1m));
    }

    [Fact]
    public void Spot_rispetto_al_costo_medio_per_metallo()
    {
        var summary = Analyze(
            Lot.BuyForTotal(GoldCoin, 1m, Eur(2000m), Eur(0m), Start) with { Id = "A" },
            Lot.BuyWithUnknownCost(SilverBar, 1m, Start, Currency.EUR) with { Id = "B" });

        // Costo 2.000 per un'oncia, spot 2.500: +25%.
        Assert.Equal(0.25m, summary.ByMetal.Single(m => m.Metal == Metal.Gold).SpotVsAverageCost);
        Assert.Null(summary.ByMetal.Single(m => m.Metal == Metal.Silver).SpotVsAverageCost);
    }
}