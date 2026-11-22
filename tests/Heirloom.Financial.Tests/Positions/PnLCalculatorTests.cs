using Heirloom.Financial.Domain;
using Heirloom.Financial.Exchange;
using Heirloom.Financial.Positions;
using Heirloom.Financial.Tests.TestData;
using Heirloom.Financial.Valuation;

namespace Heirloom.Financial.Tests.Positions;

public class PnLCalculatorTests
{
    private static readonly DateOnly Today = new(2026, 11, 20);
    private static Money Eur(decimal amount) => new(amount, Currency.EUR);

    private static SpotPrice GoldEur(decimal price) => new(Metal.Gold, Eur(price), Today);

    [Fact]
    public void Guadagno_sulla_carta_di_un_singolo_acquisto()
    {
        // Costo 1.925 €; oggi 1 oz vale 2.100 € → circa +175 € (+9,09%).
        var basis = CostBasisCalculator.Calculate(TestLots.SingleBuy, CostBasisMethod.WeightedAverage, Currency.EUR);

        var pnl = PnLCalculator.Calculate(basis, [GoldEur(2100m)], SpotValuation.Instance);

        Assert.Equal(175.01m, Math.Round(pnl.UnrealizedPnL.Amount, 2));
        Assert.Equal(0.0909m, Math.Round(pnl.UnrealizedPercent!.Value, 4));
        Assert.Equal(Eur(0m), pnl.RealizedPnL);
    }

    [Fact]
    public void Realizzato_e_non_realizzato_sono_separati()
    {
        var basis = CostBasisCalculator.Calculate(TestLots.WithPartialSale, CostBasisMethod.WeightedAverage, Currency.EUR);

        var pnl = PnLCalculator.Calculate(basis, [GoldEur(2500m)], SpotValuation.Instance);

        Assert.Equal(Eur(390m), pnl.RealizedPnL);
        Assert.Equal(Eur(6600m), pnl.TotalCostBasis);
        Assert.Equal(pnl.RealizedPnL + pnl.UnrealizedPnL, pnl.TotalPnL);
    }

    [Fact]
    public void Le_commissioni_di_vendita_riducono_il_realizzato()
    {
        var withFees = CostBasisCalculator.Calculate(TestLots.WithPartialSale, CostBasisMethod.WeightedAverage, Currency.EUR);

        var noFeesLots = TestLots.MultipleBuys.Append(
            Lot.Sell(TestProducts.Krugerrand1Oz, 1m, Eur(2600m), Eur(0m), new DateOnly(2025, 3, 1)));
        var withoutFees = CostBasisCalculator.Calculate(noFeesLots, CostBasisMethod.WeightedAverage, Currency.EUR);

        Assert.Equal(withoutFees.RealizedPnL - Eur(10m), withFees.RealizedPnL);
    }

    [Fact]
    public void Con_costo_zero_la_percentuale_e_assente()
    {
        // Una moneta ricevuta in regalo.
        var gift = new[] { Lot.Buy(TestProducts.Marengo, 1m, Eur(0m), Eur(0m), new DateOnly(2024, 1, 1)) };
        var basis = CostBasisCalculator.Calculate(gift, CostBasisMethod.WeightedAverage, Currency.EUR);

        var pnl = PnLCalculator.Calculate(basis, [GoldEur(2600m)], SpotValuation.Instance);

        Assert.Null(pnl.UnrealizedPercent);
        Assert.Null(Assert.Single(pnl.Positions).UnrealizedPercent);
        Assert.True(pnl.UnrealizedPnL.Amount > 0m);
    }

    [Fact]
    public void La_strategia_di_valutazione_cambia_il_valore()
    {
        var basis = CostBasisCalculator.Calculate(TestLots.SingleBuy, CostBasisMethod.WeightedAverage, Currency.EUR);
        var premium = new SpotPlusPremiumValuation(new Dictionary<ProductForm, decimal>
        {
            [ProductForm.Coin] = 0.04m,
            [ProductForm.Bar] = 0.01m,
        });

        var spot = PnLCalculator.Calculate(basis, [GoldEur(2100m)], SpotValuation.Instance);
        var withPremium = PnLCalculator.Calculate(basis, [GoldEur(2100m)], premium);

        Assert.True(withPremium.TotalValue > spot.TotalValue);
    }

    [Fact]
    public void Spot_in_un_altra_valuta_vengono_rifiutati()
    {
        var basis = CostBasisCalculator.Calculate(TestLots.SingleBuy, CostBasisMethod.WeightedAverage, Currency.EUR);
        var usd = new SpotPrice(Metal.Gold, new Money(2300m, Currency.USD), Today);

        Assert.Throws<ArgumentException>(() => PnLCalculator.Calculate(basis, [usd], SpotValuation.Instance));
    }

    [Fact]
    public void Manca_lo_spot_di_un_metallo_posseduto()
    {
        var basis = CostBasisCalculator.Calculate(TestLots.MixedMultiYear, CostBasisMethod.WeightedAverage, Currency.EUR);

        var ex = Assert.Throws<ArgumentException>(() => PnLCalculator.Calculate(basis, [GoldEur(2500m)], SpotValuation.Instance));
        Assert.Contains("Silver", ex.Message);
    }

    // ---------- L'esempio euro/dollaro della documentazione ----------

    [Fact]
    public void Lo_stesso_investimento_guadagna_11_1_percento_in_dollari_e_1_85_percento_in_euro()
    {
        // 1 oncia d'oro puro comprata a 1.800 € quando EUR/USD era 1,10 (= 1.980 $).
        // Oggi l'oro vale 2.200 $ e EUR/USD è 1,20 (= 1.833,33 €).
        var ounce = new MetalProduct("Oncia d'oro puro", Metal.Gold, ProductForm.Bar, Weight.FromTroyOunces(1m), 1m);
        var purchaseDay = new DateOnly(2024, 1, 2);
        var lots = new[] { Lot.Buy(ounce, 1m, Eur(1800m), Eur(0m), purchaseDay) };

        var rates = new FxRateSeries(
        [
            new FxRate(Currency.EUR, Currency.USD, 1.10m, purchaseDay),
            new FxRate(Currency.EUR, Currency.USD, 1.20m, Today),
        ]);
        var spotUsd = new SpotPrice(Metal.Gold, new Money(2200m, Currency.USD), Today);

        // In dollari: ogni lotto convertito con il cambio della SUA data.
        var lotsUsd = LotCurrencyConverter.Convert(lots, Currency.USD, rates);
        var basisUsd = CostBasisCalculator.Calculate(lotsUsd, CostBasisMethod.WeightedAverage, Currency.USD);
        var pnlUsd = PnLCalculator.Calculate(basisUsd, [spotUsd], SpotValuation.Instance);

        // In euro: il costo resta quello pagato, lo spot di oggi si converte con il cambio di oggi.
        var basisEur = CostBasisCalculator.Calculate(lots, CostBasisMethod.WeightedAverage, Currency.EUR);
        var spotEur = new SpotPrice(Metal.Gold, rates.Convert(spotUsd.PricePerTroyOunce, Currency.EUR, Today), Today);
        var pnlEur = PnLCalculator.Calculate(basisEur, [spotEur], SpotValuation.Instance);

        Assert.Equal(new Money(1980m, Currency.USD), basisUsd.TotalCostBasis);
        Assert.Equal(0.1111m, Math.Round(pnlUsd.UnrealizedPercent!.Value, 4));
        Assert.Equal(0.0185m, Math.Round(pnlEur.UnrealizedPercent!.Value, 4));
    }
}