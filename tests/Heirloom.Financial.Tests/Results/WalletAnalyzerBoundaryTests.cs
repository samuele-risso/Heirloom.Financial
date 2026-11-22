using Heirloom.Financial.Domain;
using Heirloom.Financial.Positions;
using Heirloom.Financial.Results;
using Heirloom.Financial.Risk;
using Heirloom.Financial.Valuation;
using static Heirloom.Financial.Tests.TestData.OneOunce;

namespace Heirloom.Financial.Tests.Results;

/// <summary>
/// I casi al confine dell'analisi completa: un anno esatto di storico, XIRR non calcolabile.
/// </summary>
public class WalletAnalyzerBoundaryTests
{
    private static readonly DateOnly Start = new(2023, 1, 1);

    private static WalletAnalysisOptions Options(DateOnly valuationDate) => new()
    {
        Currency = Currency.EUR,
        ValuationDate = valuationDate,
        Valuation = SpotValuation.Instance,
        CostBasisMethod = CostBasisMethod.Fifo,
        AnnualizationBasis = AnnualizationBasis.CalendarDays365,
    };

    [Fact]
    public void Con_esattamente_365_giorni_il_rendimento_si_annualizza()
    {
        var end = Start.AddDays(365);
        var lot = Lot.Buy(GoldCoin, 1m, Eur(2000m), Eur(0m), Start);

        var summary = WalletAnalyzer.Analyze([lot], [GoldSpot(2000m, Start), GoldSpot(2200m, end)], Options(end));

        Assert.NotNull(summary.Performance!.AnnualizedReturn);
    }

    [Fact]
    public void Con_364_giorni_il_rendimento_non_si_annualizza()
    {
        var end = Start.AddDays(364);
        var lot = Lot.Buy(GoldCoin, 1m, Eur(2000m), Eur(0m), Start);

        var summary = WalletAnalyzer.Analyze([lot], [GoldSpot(2000m, Start), GoldSpot(2200m, end)], Options(end));

        Assert.Null(summary.Performance!.AnnualizedReturn);
    }

    [Fact]
    public void Se_l_xirr_non_e_calcolabile_lo_dice_nelle_note()
    {
        // Un regalo registrato a prezzo zero: nessun flusso negativo, quindi niente XIRR.
        var end = Start.AddDays(30);
        var gift = Lot.Buy(GoldCoin, 1m, Eur(0m), Eur(0m), Start);

        var summary = WalletAnalyzer.Analyze([gift], [GoldSpot(2000m, Start)], Options(end));

        Assert.Null(summary.Performance!.MoneyWeightedReturn);
        Assert.Contains(summary.Notes, n => n.Contains("XIRR"));
    }

    [Fact]
    public void Se_l_xirr_e_calcolabile_non_ci_sono_note()
    {
        var end = Start.AddDays(30);
        var lot = Lot.Buy(GoldCoin, 1m, Eur(2000m), Eur(0m), Start);

        var summary = WalletAnalyzer.Analyze([lot], [GoldSpot(2000m, Start), GoldSpot(2100m, end)], Options(end));

        Assert.NotNull(summary.Performance!.MoneyWeightedReturn);
        Assert.Empty(summary.Notes);
    }
}