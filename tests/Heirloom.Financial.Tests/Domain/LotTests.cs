using Heirloom.Financial.Domain;
using Heirloom.Financial.Tests.TestData;

namespace Heirloom.Financial.Tests.Domain;

public class LotTests
{
    private static Money Eur(decimal amount) => new(amount, Currency.EUR);
    private static Money Usd(decimal amount) => new(amount, Currency.USD);
    private static readonly DateOnly Day = new(2024, 6, 1);

    // ---------- Importi ----------

    [Fact]
    public void Il_controvalore_lordo_e_quantita_per_prezzo()
    {
        var lot = Lot.Buy(TestProducts.Krugerrand1Oz, 3m, Eur(2000m), Eur(30m), Day);

        Assert.Equal(Eur(6000m), lot.GrossAmount);
    }

    [Fact]
    public void Per_un_acquisto_il_netto_include_i_costi()
    {
        var lot = Lot.Buy(TestProducts.Krugerrand1Oz, 3m, Eur(2000m), Eur(30m), Day);

        Assert.Equal(Eur(6030m), lot.NetAmount);
    }

    [Fact]
    public void Per_una_vendita_il_netto_toglie_i_costi()
    {
        var lot = Lot.Sell(TestProducts.Krugerrand1Oz, 3m, Eur(2000m), Eur(30m), Day);

        Assert.Equal(Eur(5970m), lot.NetAmount);
    }

    [Fact]
    public void Il_flusso_di_cassa_e_negativo_per_un_acquisto_e_positivo_per_una_vendita()
    {
        var buy = Lot.Buy(TestProducts.Krugerrand1Oz, 1m, Eur(2000m), Eur(20m), Day);
        var sell = Lot.Sell(TestProducts.Krugerrand1Oz, 1m, Eur(2000m), Eur(20m), Day);

        Assert.Equal(Eur(-2020m), buy.CashFlow);
        Assert.Equal(Eur(1980m), sell.CashFlow);
    }

    [Fact]
    public void Il_costo_totale_del_singolo_acquisto_di_prova_e_1925_euro()
    {
        Assert.Equal(Eur(1925m), TestLots.SingleBuy[0].NetAmount);
    }

    [Fact]
    public void La_valuta_del_lotto_e_quella_del_prezzo()
    {
        var lot = Lot.Buy(TestProducts.Krugerrand1Oz, 1m, Usd(2100m), Money.Zero(Currency.USD), Day);

        Assert.Equal(Currency.USD, lot.Currency);
    }

    // ---------- Peso fino ----------

    [Fact]
    public void Tre_Krugerrand_contengono_tre_once_di_oro_fino()
    {
        var lot = Lot.Buy(TestProducts.Krugerrand1Oz, 3m, Eur(2000m), Eur(0m), Day);

        Assert.Equal(3.0000m, Math.Round(lot.FineWeight.TroyOunces, 4));
    }

    [Fact]
    public void Dieci_Maple_Leaf_argento_contengono_311_06889_g_di_fino()
    {
        // 31,11 g x 0,9999 x 10 = 311,06889 g
        var lot = Lot.Buy(TestProducts.SilverMapleLeaf1Oz, 10m, Eur(28m), Eur(0m), Day);

        Assert.Equal(new Weight(311.06889m), lot.FineWeight);
    }

    // ---------- Costruzione ----------

    [Fact]
    public void Buy_e_Sell_impostano_il_verso()
    {
        Assert.Equal(LotSide.Buy, Lot.Buy(TestProducts.Marengo, 1m, Eur(480m), Eur(0m), Day).Side);
        Assert.Equal(LotSide.Sell, Lot.Sell(TestProducts.Marengo, 1m, Eur(480m), Eur(0m), Day).Side);
    }

    [Fact]
    public void Due_lotti_con_gli_stessi_dati_sono_uguali()
    {
        var a = Lot.Buy(TestProducts.Marengo, 2m, Eur(480m), Eur(10m), Day);
        var b = Lot.Buy(TestProducts.Marengo, 2m, Eur(480m), Eur(10m), Day);

        Assert.Equal(a, b);
    }

    [Fact]
    public void Un_prezzo_zero_e_accettato()
    {
        // Per esempio una moneta ricevuta in regalo o in eredità.
        var lot = Lot.Buy(TestProducts.Marengo, 1m, Eur(0m), Eur(0m), Day);

        Assert.True(lot.GrossAmount.IsZero);
    }

    // ---------- Validazione ----------

    [Fact]
    public void Un_prodotto_null_viene_rifiutato()
    {
        Assert.Throws<ArgumentNullException>(() => Lot.Buy(null!, 1m, Eur(100m), Eur(0m), Day));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Una_quantita_non_positiva_viene_rifiutata(int quantity)
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(
            () => Lot.Buy(TestProducts.Marengo, quantity, Eur(480m), Eur(0m), Day));
        Assert.Equal("quantity", ex.ParamName);
    }

    [Fact]
    public void Un_prezzo_negativo_viene_rifiutato()
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(
            () => Lot.Buy(TestProducts.Marengo, 1m, Eur(-1m), Eur(0m), Day));
        Assert.Equal("unitPrice", ex.ParamName);
    }

    [Fact]
    public void Costi_negativi_vengono_rifiutati()
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(
            () => Lot.Buy(TestProducts.Marengo, 1m, Eur(480m), Eur(-5m), Day));
        Assert.Equal("fees", ex.ParamName);
    }

    [Fact]
    public void Prezzo_e_costi_in_valute_diverse_vengono_rifiutati()
    {
        var ex = Assert.Throws<ArgumentException>(
            () => Lot.Buy(TestProducts.Marengo, 1m, Eur(480m), Usd(5m), Day));
        Assert.Equal("fees", ex.ParamName);
        Assert.Contains("EUR", ex.Message);
        Assert.Contains("USD", ex.Message);
    }

    [Fact]
    public void Un_verso_non_definito_viene_rifiutato()
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(
            () => new Lot(TestProducts.Marengo, 1m, Eur(480m), Eur(0m), Day, (LotSide)0));
        Assert.Equal("side", ex.ParamName);
    }

    // ---------- Storie di prova ----------

    [Fact]
    public void Le_storie_di_prova_sono_coerenti()
    {
        Assert.Single(TestLots.SingleBuy);
        Assert.Equal(2, TestLots.MultipleBuys.Count);
        Assert.Single(TestLots.WithPartialSale, l => l.Side == LotSide.Sell);

        var mixed = TestLots.MixedMultiYear;
        Assert.Contains(mixed, l => l.Product.Metal == Metal.Gold);
        Assert.Contains(mixed, l => l.Product.Metal == Metal.Silver);
        Assert.Contains(mixed, l => l.Product.Form == ProductForm.Coin);
        Assert.Contains(mixed, l => l.Product.Form == ProductForm.Bar);
        Assert.True(mixed.Max(l => l.Date).Year - mixed.Min(l => l.Date).Year >= 3);
    }

    [Fact]
    public void Tutte_le_storie_di_prova_sono_in_euro()
    {
        var all = TestLots.SingleBuy
            .Concat(TestLots.MultipleBuys)
            .Concat(TestLots.WithPartialSale)
            .Concat(TestLots.MixedMultiYear);

        Assert.All(all, l => Assert.Equal(Currency.EUR, l.Currency));
    }
}