using Heirloom.Financial.Domain;
using Heirloom.Financial.Tests.TestData;
using Heirloom.Financial.Valuation;

namespace Heirloom.Financial.Tests.Valuation;

/// <summary>
/// I casi al confine della valutazione: quantità zero o negative, spot ripetuti o mancanti,
/// valute diverse, configurazioni fuori intervallo.
/// </summary>
public class ValuationBoundaryTests
{
    private static readonly DateOnly Day = new(2024, 6, 1);
    private static Money Eur(decimal amount) => new(amount, Currency.EUR);
    private static SpotPrice Gold(decimal price) => new(Metal.Gold, Eur(price), Day);

    // ---------- Strategie ----------

    [Fact]
    public void Quantita_zero_vale_zero()
    {
        Assert.Equal(Eur(0m), SpotValuation.Instance.Value(OneOunce.GoldCoin, 0m, Gold(2000m)));
    }

    [Fact]
    public void Quantita_negativa_viene_rifiutata()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => SpotValuation.Instance.Value(OneOunce.GoldCoin, -1m, Gold(2000m)));
    }

    [Fact]
    public void Premio_negativo_per_un_prodotto_viene_rifiutato()
    {
        var byForm = new Dictionary<ProductForm, decimal> { [ProductForm.Coin] = 0.04m, [ProductForm.Bar] = 0.01m };
        var byProduct = new Dictionary<MetalProduct, decimal> { [OneOunce.GoldCoin] = -0.01m };

        Assert.Throws<ArgumentOutOfRangeException>(() => new SpotPlusPremiumValuation(byForm, byProduct));
    }

    [Fact]
    public void Sconto_di_riacquisto_del_100_per_cento_per_un_prodotto_viene_rifiutato()
    {
        var byForm = new Dictionary<ProductForm, decimal> { [ProductForm.Coin] = 0.02m, [ProductForm.Bar] = 0.01m };
        var byProduct = new Dictionary<MetalProduct, decimal> { [OneOunce.GoldCoin] = 1m };

        Assert.Throws<ArgumentOutOfRangeException>(() => new BuybackValuation(byForm, byProduct));
    }

    // ---------- Valore di fusione dei lotti ----------

    [Fact]
    public void Valore_dei_lotti_senza_spot_viene_rifiutato()
    {
        Assert.Throws<ArgumentException>(() => MeltValueCalculator.MeltValue(TestLots.SingleBuy, []));
    }

    [Fact]
    public void Valore_dei_lotti_con_due_spot_dello_stesso_metallo_viene_rifiutato()
    {
        Assert.Throws<ArgumentException>(() => MeltValueCalculator.MeltValue(TestLots.SingleBuy, [Gold(2000m), Gold(2100m)]));
    }

    [Fact]
    public void Valore_dei_lotti_con_uno_spot_per_metallo_e_accettato()
    {
        var lots = new[] { Lot.Buy(OneOunce.GoldCoin, 2m, Eur(1m), Eur(0m), Day), Lot.Buy(OneOunce.SilverBar, 1m, Eur(1m), Eur(0m), Day) };

        var value = MeltValueCalculator.MeltValue(lots, [Gold(2000m), new SpotPrice(Metal.Silver, Eur(25m), Day)]);

        Assert.Equal(Eur(4250m), value);
    }

    // ---------- Premi ----------

    [Fact]
    public void Premio_per_oncia_con_prezzo_in_altra_valuta_viene_rifiutato()
    {
        Assert.Throws<ArgumentException>(() =>
            PremiumCalculator.PremiumPerTroyOunce(new Money(2100m, Currency.USD), OneOunce.GoldCoin, Gold(2000m)));
    }

    [Fact]
    public void Premio_effettivo_divide_il_costo_per_il_numero_di_pezzi()
    {
        // 2 pezzi a 2.050 € più 100 € di costi: 2.100 € l'uno, 5% sopra uno spot di 2.000 €.
        var lot = Lot.Buy(OneOunce.GoldCoin, 2m, Eur(2050m), Eur(100m), Day);

        Assert.Equal(0.05m, PremiumCalculator.EffectivePremiumPercent(lot, Gold(2000m)));
    }
}