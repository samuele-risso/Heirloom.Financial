using Heirloom.Financial.Domain;
using Heirloom.Financial.Positions;
using Heirloom.Financial.Results;
using Heirloom.Financial.Risk;
using Heirloom.Financial.Tests.TestData;
using Heirloom.Financial.Valuation;

namespace Heirloom.Financial.Tests.Results;

/// <summary>
/// Nei risultati gli importi hanno al massimo 2 decimali, i pesi e le percentuali 4.
/// </summary>
public class ResultsRoundingTests
{
    private static bool HasAtMost(decimal value, int decimals) => Math.Round(value, decimals) == value;

    [Fact]
    public void MoneyResult_arrotonda_per_eccesso_dal_5()
    {
        Assert.Equal(new MoneyResult(2.35m, "EUR"), MoneyResult.From(new Money(2.345m, Currency.EUR)));
        Assert.Equal(new MoneyResult(-2.35m, "USD"), MoneyResult.From(new Money(-2.345m, Currency.USD)));
        Assert.Null(MoneyResult.FromOptional(null));
    }

    [Fact]
    public void Un_portafoglio_con_numeri_scomodi_ha_risultati_arrotondati()
    {
        // Spot con molti decimali e un portafoglio misto: i calcoli producono decimali a 28 cifre.
        var spots = new List<SpotPrice>();
        for (var day = new DateOnly(2022, 5, 20); day <= new DateOnly(2025, 6, 30); day = day.AddDays(7))
        {
            var t = day.DayNumber % 97;
            spots.Add(new SpotPrice(Metal.Gold, new Money(1700.123457m + t * 13.333333m, Currency.EUR), day));
            spots.Add(new SpotPrice(Metal.Silver, new Money(21.987654m + t * 0.111111m, Currency.EUR), day));
        }

        var summary = WalletAnalyzer.Analyze(TestLots.MixedMultiYear, spots, new WalletAnalysisOptions
        {
            Currency = Currency.EUR,
            ValuationDate = new DateOnly(2025, 6, 30),
            Valuation = SpotValuation.Instance,
            CostBasisMethod = CostBasisMethod.Fifo,
            AnnualizationBasis = AnnualizationBasis.TradingDays252,
            RiskFreeRate = 0.025m,
        });

        var amounts = new List<MoneyResult>
        {
            summary.TotalValue, summary.KnownCostValue, summary.UnknownCostValue, summary.TotalCostBasis,
            summary.UnrealizedPnL, summary.RealizedPnL, summary.UnknownCostProceeds, summary.TotalPnL,
        };
        amounts.AddRange(summary.Positions.SelectMany(p => new[] { p.Value, p.CostBasis, p.UnrealizedPnL, p.AverageCost! }));
        amounts.AddRange(summary.AllocationByMetal.Concat(summary.AllocationByForm).Select(s => s.Value));

        Assert.All(amounts, a => Assert.True(HasAtMost(a.Amount, 2), $"{a.Amount} ha più di 2 decimali."));

        var fourDecimals = new List<decimal?>
        {
            summary.GoldFineWeightGrams, summary.SilverFineWeightGrams, summary.UnrealizedPercent,
            summary.Performance!.TimeWeightedReturn, summary.Performance.AnnualizedReturn, summary.Performance.MoneyWeightedReturn,
            summary.Risk!.Volatility, summary.Risk.Sharpe, summary.Risk.MaxDrawdown?.Depth,
        };
        fourDecimals.AddRange(summary.Positions.SelectMany(p => new decimal?[] { p.FineWeightGrams, p.UnrealizedPercent }));
        fourDecimals.AddRange(summary.AllocationByMetal.Select(s => s.Percent));

        Assert.All(fourDecimals.Where(v => v is not null), v => Assert.True(HasAtMost(v!.Value, 4), $"{v} ha più di 4 decimali."));

        Assert.All(summary.Performance.History, p =>
        {
            Assert.True(HasAtMost(p.Value, 2));
            Assert.True(HasAtMost(p.InvestedCapital, 2));
            Assert.True(HasAtMost(p.Index, 4));
        });

        Assert.Equal(1m, summary.AllocationByMetal.Sum(s => s.Percent!.Value));
        Assert.Equal(1m, summary.AllocationByForm.Sum(s => s.Percent!.Value));
    }
}