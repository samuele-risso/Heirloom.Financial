using Heirloom.Financial.Domain;
using Heirloom.Financial.Exchange;
using Heirloom.Financial.Positions;
using Heirloom.Financial.Results;
using static Heirloom.Financial.Tests.TestData.OneOunce;

namespace Heirloom.Financial.Tests.Results;

/// <summary>
/// Il riepilogo senza prezzi di mercato. Stesso portafoglio di WalletAnalyzerTaxAndLotsTests:
/// <list type="bullet">
/// <item>A: 2 monete d'oro da 1 oz pagate 4.000 € in totale, 1 venduta a 2.200 €;</item>
/// <item>B: 1 moneta ereditata; S: lingotto d'argento da 10 oz a 250 €.</item>
/// </list>
/// </summary>
public class HoldingsAnalyzerTests
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

    private static MoneyResult EurResult(decimal amount) => new(amount, "EUR");

    private static HoldingsSummary Summarize(IEnumerable<Lot> lots, CostBasisMethod method = CostBasisMethod.SpecificLot) =>
        HoldingsAnalyzer.Summarize(lots, method, Currency.EUR, End);

    [Fact]
    public void Totali_dei_lotti_aperti()
    {
        var s = Summarize(Lots);

        Assert.Equal("EUR", s.Currency);
        Assert.Equal(End, s.AsOf);
        Assert.Equal(3, s.OpenLotCount);
        Assert.Equal(2, s.ProductCount);
        Assert.Equal(3m, s.Quantity);
        Assert.Equal(1m, s.UnknownCostQuantity);
        Assert.Equal(378.96m, s.GrossWeightGrams);
        Assert.Equal(373.2417m, s.FineWeightGrams);
        Assert.Equal(EurResult(2250m), s.CostBasis);
        Assert.Equal(366m, s.AverageHoldingDays);
    }

    [Fact]
    public void Riepilogo_per_metallo()
    {
        var s = Summarize(Lots);

        Assert.Equal([Metal.Gold, Metal.Silver], s.ByMetal.Select(m => m.Metal));

        var gold = s.For(Metal.Gold)!;
        Assert.Equal(2, gold.OpenLotCount);
        Assert.Equal(2m, gold.Quantity);
        Assert.Equal(1m, gold.UnknownCostQuantity);
        Assert.Equal(67.86m, gold.GrossWeightGrams);
        Assert.Equal(62.2070m, gold.FineWeightGrams);
        Assert.Equal(EurResult(2000m), gold.CostBasis);
        // Solo la moneta a costo noto: 2.000 € per un'oncia.
        Assert.Equal(64.3015m, gold.AverageCostPerFineGram);

        var silver = s.For(Metal.Silver)!;
        Assert.Equal(1, silver.OpenLotCount);
        Assert.Equal(0m, silver.UnknownCostQuantity);
        Assert.Equal(EurResult(250m), silver.CostBasis);
    }

    [Fact]
    public void Flusso_di_cassa_totale_e_per_metallo()
    {
        var s = Summarize(Lots);

        Assert.Equal(new CashFlowSummary(EurResult(4250m), 3, EurResult(2200m), 1, EurResult(-2050m)), s.CashFlow);
        Assert.Equal(new CashFlowSummary(EurResult(4000m), 2, EurResult(2200m), 1, EurResult(-1800m)), s.For(Metal.Gold)!.CashFlow);
        Assert.Equal(new CashFlowSummary(EurResult(250m), 1, EurResult(0m), 0, EurResult(-250m)), s.For(Metal.Silver)!.CashFlow);
    }

    [Fact]
    public void Anzianita_media_pesata_sul_peso_lordo()
    {
        var lots = new[]
        {
            Lot.Buy(GoldCoin, 1m, Eur(2000m), Eur(0m), Start) with { Id = "G" },
            Lot.Buy(SilverBar, 1m, Eur(250m), Eur(0m), End.AddDays(-30)) with { Id = "S" },
        };

        // (366 × 33,93 + 30 × 311,1) / 345,03
        Assert.Equal(63.04m, Summarize(lots).AverageHoldingDays);
    }

    [Fact]
    public void Metallo_venduto_del_tutto_resta_con_il_suo_flusso_di_cassa()
    {
        var lots = new[]
        {
            Lot.Buy(SilverBar, 1m, Eur(250m), Eur(0m), Start) with { Id = "S" },
            Lot.Sell(SilverBar, 1m, Eur(300m), Eur(0m), End) with { MatchedLotId = "S" },
        };

        var s = Summarize(lots);

        Assert.Equal(0, s.OpenLotCount);
        Assert.Equal(0, s.ProductCount);
        Assert.Equal(0m, s.GrossWeightGrams);
        Assert.Null(s.AverageHoldingDays);

        var silver = Assert.Single(s.ByMetal);
        Assert.Equal(0m, silver.Quantity);
        Assert.Equal(EurResult(0m), silver.CostBasis);
        Assert.Null(silver.AverageCostPerFineGram);
        Assert.Equal(EurResult(50m), silver.CashFlow.NetCashFlow);
        Assert.Null(s.For(Metal.Gold));
    }

    [Fact]
    public void Nessuna_operazione_riepilogo_vuoto()
    {
        var s = Summarize([]);

        Assert.Equal(0, s.OpenLotCount);
        Assert.Equal(0m, s.Quantity);
        Assert.Equal(EurResult(0m), s.CostBasis);
        Assert.Null(s.AverageHoldingDays);
        Assert.Empty(s.ByMetal);
        Assert.Equal(0, s.CashFlow.PurchaseCount);
    }

    [Fact]
    public void Fifo_da_gli_stessi_lotti_aperti_senza_collegamenti()
    {
        var lots = new[]
        {
            Lot.Buy(GoldCoin, 1m, Eur(1800m), Eur(0m), Start),
            Lot.Buy(GoldCoin, 1m, Eur(2100m), Eur(0m), Start.AddDays(10)),
            Lot.Sell(GoldCoin, 1m, Eur(2200m), Eur(0m), Start.AddDays(20)),
        };

        var s = Summarize(lots, CostBasisMethod.Fifo);

        Assert.Equal(1, s.OpenLotCount);
        Assert.Equal(EurResult(2100m), s.CostBasis);
        Assert.Equal(356m, s.AverageHoldingDays);
    }

    [Fact]
    public void Operazioni_in_altra_valuta_convertite_con_il_cambio_del_giorno()
    {
        var lots = new[] { Lot.Buy(GoldCoin, 1m, new Money(2200m, Currency.USD), new Money(0m, Currency.USD), Start) with { Id = "U" } };
        var rates = new FxRateSeries([new FxRate(Currency.EUR, Currency.USD, 1.10m, Start)]);

        var s = HoldingsAnalyzer.Summarize(lots, CostBasisMethod.SpecificLot, Currency.EUR, End, rates);

        Assert.Equal(EurResult(2000m), s.CostBasis);
        Assert.Equal(EurResult(-2000m), s.CashFlow.NetCashFlow);
    }

    [Fact]
    public void Operazioni_in_altra_valuta_senza_cambi_errore()
    {
        var lots = new[] { Lot.Buy(GoldCoin, 1m, new Money(2200m, Currency.USD), new Money(0m, Currency.USD), Start) with { Id = "U" } };

        Assert.Throws<ArgumentException>(() => Summarize(lots));
    }

    [Fact]
    public void Media_ponderata_non_ammessa()
    {
        Assert.Throws<ArgumentException>(() => Summarize(Lots, CostBasisMethod.WeightedAverage));
    }

    [Fact]
    public void Operazione_dopo_il_giorno_del_riepilogo_errore()
    {
        var lots = new[] { Lot.Buy(GoldCoin, 1m, Eur(2000m), Eur(0m), End.AddDays(1)) with { Id = "G" } };

        Assert.Throws<ArgumentOutOfRangeException>(() => Summarize(lots));
    }

    [Fact]
    public void Argomenti_non_validi()
    {
        Assert.Throws<ArgumentNullException>(() => HoldingsAnalyzer.Summarize(null!, CostBasisMethod.Fifo, Currency.EUR, End));
        Assert.Throws<ArgumentOutOfRangeException>(() => HoldingsAnalyzer.Summarize(Lots, (CostBasisMethod)99, Currency.EUR, End));
        Assert.Throws<ArgumentOutOfRangeException>(() => HoldingsAnalyzer.Summarize(Lots, CostBasisMethod.Fifo, (Currency)99, End));
    }

    [Fact]
    public void Coerente_con_WalletAnalyzer()
    {
        var spots = new[] { GoldSpot(2000m, Start), GoldSpot(2500m, End), SilverSpot(25m, Start), SilverSpot(30m, End) };
        var wallet = WalletAnalyzer.Analyze(Lots, spots, new WalletAnalysisOptions
        {
            Currency = Currency.EUR,
            ValuationDate = End,
            Valuation = Heirloom.Financial.Valuation.SpotValuation.Instance,
            CostBasisMethod = CostBasisMethod.SpecificLot,
            AnnualizationBasis = Heirloom.Financial.Risk.AnnualizationBasis.CalendarDays365,
            IncludeHistory = false,
        });

        var s = Summarize(Lots);

        Assert.Equal(wallet.TotalCostBasis, s.CostBasis);
        Assert.Equal(wallet.CashFlow, s.CashFlow);
        Assert.Equal(wallet.Lots.Count, s.OpenLotCount);
    }
}
