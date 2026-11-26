using Heirloom.Financial.Domain;
using Heirloom.Financial.Positions;
using Heirloom.Financial.Results;
using Heirloom.Financial.Risk;
using Heirloom.Financial.Taxation;
using Heirloom.Financial.Valuation;
using static Heirloom.Financial.Tests.TestData.OneOunce;

namespace Heirloom.Financial.Tests.Taxation;

/// <summary>
/// La regola per data e le tasse sulle vendite già fatte.
/// <list type="bullet">
/// <item>A: 2 monete pagate 4.000 € con fattura; 1 venduta a 2.600 € nel 2023 e 1 a 3.000 € nel 2025.</item>
/// <item>N: 1 moneta pagata 1.500 € senza fattura, venduta a 2.400 € nel 2023.</item>
/// <item>H: 1 moneta ereditata senza documento, venduta a 3.000 € nel 2025.</item>
/// </list>
/// </summary>
public class SaleTaxTests
{
    private static readonly ItalianTaxPolicy Policy = ItalianTaxPolicy.Standard;
    private static readonly DateOnly Start = new(2022, 3, 1);
    private static readonly DateOnly In2023 = new(2023, 6, 1);
    private static readonly DateOnly In2025 = new(2025, 5, 1);
    private static readonly DateOnly End = new(2025, 12, 31);

    private static readonly Lot[] Lots =
    [
        Lot.BuyForTotal(GoldCoin, 2m, Eur(4000m), Eur(0m), Start) with { Id = "A", HasDocumentedCost = true },
        Lot.Buy(GoldCoin, 1m, Eur(1500m), Eur(0m), Start) with { Id = "N" },
        Lot.BuyWithUnknownCost(GoldCoin, 1m, Start, Currency.EUR) with { Id = "H" },
        Lot.Sell(GoldCoin, 1m, Eur(2600m), Eur(0m), In2023) with { Id = "S1", MatchedLotId = "A" },
        Lot.Sell(GoldCoin, 1m, Eur(2400m), Eur(0m), In2023) with { Id = "S2", MatchedLotId = "N" },
        Lot.Sell(GoldCoin, 1m, Eur(3000m), Eur(0m), In2025) with { Id = "S3", MatchedLotId = "A" },
        Lot.Sell(GoldCoin, 1m, Eur(3000m), Eur(0m), In2025) with { Id = "S4", MatchedLotId = "H" },
    ];

    private static WalletSummary Analyze(ITaxPolicy? policy) => WalletAnalyzer.Analyze(
        Lots,
        [GoldSpot(2000m, Start), GoldSpot(3000m, End)],
        new WalletAnalysisOptions
        {
            Currency = Currency.EUR,
            ValuationDate = End,
            Valuation = SpotValuation.Instance,
            CostBasisMethod = CostBasisMethod.SpecificLot,
            AnnualizationBasis = AnnualizationBasis.CalendarDays365,
            IncludeHistory = false,
            TaxPolicy = policy,
        });

    // ---------- Regola per data ----------

    [Fact]
    public void Senza_fattura_prima_del_2024_si_paga_sul_25_per_cento()
    {
        // 26% di 25% di 2.000 € = 130 €.
        Assert.Equal(Eur(130m), Policy.EstimateTax(Eur(2000m), Eur(1500m), false, new DateOnly(2023, 12, 31)));
    }

    [Fact]
    public void Senza_fattura_dal_2024_si_paga_su_tutto()
    {
        Assert.Equal(Eur(520m), Policy.EstimateTax(Eur(2000m), Eur(1500m), false, ItalianTaxPolicy.FullValueRuleFrom));
    }

    [Fact]
    public void Con_fattura_la_data_non_cambia_niente()
    {
        Assert.Equal(Eur(130m), Policy.EstimateTax(Eur(2000m), Eur(1500m), true, new DateOnly(2020, 1, 1)));
        Assert.Equal(Eur(130m), Policy.EstimateTax(Eur(2000m), Eur(1500m), true, new DateOnly(2026, 1, 1)));
    }

    [Fact]
    public void Senza_data_vale_la_regola_di_oggi()
    {
        Assert.Equal(
            Policy.EstimateTax(Eur(2000m), null, false, ItalianTaxPolicy.FullValueRuleFrom),
            Policy.EstimateTax(Eur(2000m), null, false));
    }

    [Fact]
    public void Una_regola_senza_data_propria_usa_quella_senza_data()
    {
        ITaxPolicy flat = new FlatPolicy();
        Assert.Equal(Eur(10m), flat.EstimateTax(Eur(100m), null, false, new DateOnly(2010, 1, 1)));
    }

    // ---------- Vendite già fatte ----------

    [Fact]
    public void Ogni_vendita_ha_le_sue_tasse_con_la_regola_del_suo_giorno()
    {
        var sales = Analyze(Policy).SaleTaxes;

        Assert.Equal(4, sales.Count);
        // S1, 2023, con fattura: 26% di (2.600 − 2.000) = 156 €.
        Assert.Equal(new MoneyResult(156m, "EUR"), sales.Single(s => s.LotId == "S1").Tax);
        // S2, 2023, senza fattura: 26% di 25% di 2.400 = 156 €.
        Assert.Equal(new MoneyResult(156m, "EUR"), sales.Single(s => s.LotId == "S2").Tax);
        // S3, 2025, con fattura: 26% di (3.000 − 2.000) = 260 €.
        Assert.Equal(new MoneyResult(260m, "EUR"), sales.Single(s => s.LotId == "S3").Tax);
        // S4, 2025, ereditata senza documento: 26% di 3.000 = 780 €.
        Assert.Equal(new MoneyResult(780m, "EUR"), sales.Single(s => s.LotId == "S4").Tax);
    }

    [Fact]
    public void Le_vendite_con_fattura_lo_dicono()
    {
        var sales = Analyze(Policy).SaleTaxes;

        Assert.Equal(1m, sales.Single(s => s.LotId == "S1").DocumentedQuantity);
        Assert.Equal(0m, sales.Single(s => s.LotId == "S2").DocumentedQuantity);
        Assert.Equal(0m, sales.Single(s => s.LotId == "S4").DocumentedQuantity);
    }

    [Fact]
    public void Gli_anni_sommano_le_loro_vendite_dal_piu_recente()
    {
        var summary = Analyze(Policy);

        Assert.Equal(2, summary.TaxYears.Count);
        Assert.Equal(2025, summary.TaxYears[0].Year);
        Assert.Equal(new MoneyResult(1040m, "EUR"), summary.TaxYears[0].Tax);
        Assert.Equal(new MoneyResult(6000m, "EUR"), summary.TaxYears[0].Proceeds);
        Assert.Equal(2023, summary.TaxYears[1].Year);
        Assert.Equal(2, summary.TaxYears[1].SalesCount);
        Assert.Equal(new MoneyResult(312m, "EUR"), summary.TaxYears[1].Tax);
        Assert.Equal(new MoneyResult(1352m, "EUR"), summary.RealizedTax);
    }

    [Fact]
    public void Senza_regola_fiscale_niente_tasse_sulle_vendite()
    {
        var summary = Analyze(null);

        Assert.Null(summary.RealizedTax);
        Assert.Empty(summary.SaleTaxes);
        Assert.Empty(summary.TaxYears);
    }

    [Fact]
    public void Fifo_divide_la_vendita_tra_pezzi_con_e_senza_fattura()
    {
        var lots = new[]
        {
            Lot.Buy(GoldCoin, 1m, Eur(2000m), Eur(0m), Start) with { Id = "F", HasDocumentedCost = true },
            Lot.Buy(GoldCoin, 1m, Eur(2000m), Eur(0m), Start.AddDays(1)) with { Id = "G" },
            Lot.Sell(GoldCoin, 2m, Eur(6000m), Eur(0m), In2025),
        };

        var sale = Assert.Single(CostBasisCalculator.Calculate(lots, CostBasisMethod.Fifo, Currency.EUR).Sales);

        Assert.Equal(1m, sale.DocumentedCostQuantity);
        Assert.Equal(Eur(2000m), sale.DocumentedCostOfSold);
        Assert.Equal(Eur(4000m), sale.CostOfSold);
    }

    private sealed class FlatPolicy : ITaxPolicy
    {
        public Money EstimateTax(Money value, Money? cost, bool hasDocumentedCost) => value * 0.10m;
    }
}