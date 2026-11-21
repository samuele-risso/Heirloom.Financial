using Heirloom.Financial.Domain;
using Heirloom.Financial.Tests.TestData;

namespace Heirloom.Financial.Tests.Domain;

public class MetalProductTests
{
    // ---------- Peso fino ----------

    [Fact]
    public void Il_Krugerrand_contiene_un_oncia_troy_di_oro_fino()
    {
        var fine = TestProducts.Krugerrand1Oz.FineWeight;

        Assert.Equal(1.0000m, Math.Round(fine.TroyOunces, 4));
    }

    [Fact]
    public void Un_lingotto_da_100_g_a_9999_contiene_99_99_g_di_fino()
    {
        Assert.Equal(new Weight(99.99m), TestProducts.GoldBar100g.FineWeight);
    }

    [Fact]
    public void Un_lingotto_d_argento_da_1_kg_a_999_contiene_999_g_di_fino()
    {
        Assert.Equal(new Weight(999m), TestProducts.SilverBar1kg.FineWeight);
    }

    [Fact]
    public void Il_Marengo_contiene_5_805_g_di_oro_fino()
    {
        Assert.Equal(new Weight(5.805m), TestProducts.Marengo.FineWeight);
    }

    [Fact]
    public void In_nessun_prodotto_il_peso_fino_supera_il_peso_lordo()
    {
        foreach (var product in TestProducts.All)
        {
            Assert.True(product.FineWeight <= product.GrossWeight, product.Name);
        }
    }

    // ---------- Dati ----------

    [Fact]
    public void Il_prodotto_conserva_i_dati_ricevuti()
    {
        var p = new MetalProduct("Lingotto oro 100 g", Metal.Gold, ProductForm.Bar, new Weight(100m), 0.9999m);

        Assert.Equal("Lingotto oro 100 g", p.Name);
        Assert.Equal(Metal.Gold, p.Metal);
        Assert.Equal(ProductForm.Bar, p.Form);
        Assert.Equal(new Weight(100m), p.GrossWeight);
        Assert.Equal(0.9999m, p.Fineness);
    }

    [Fact]
    public void Gli_spazi_attorno_al_nome_vengono_tolti()
    {
        var p = new MetalProduct("  Sterlina  ", Metal.Gold, ProductForm.Coin, new Weight(7.98m), 0.9167m);

        Assert.Equal("Sterlina", p.Name);
    }

    [Fact]
    public void Due_prodotti_con_gli_stessi_dati_sono_uguali()
    {
        var a = new MetalProduct("Lingotto oro 100 g", Metal.Gold, ProductForm.Bar, new Weight(100m), 0.9999m);
        var b = new MetalProduct("Lingotto oro 100 g", Metal.Gold, ProductForm.Bar, new Weight(100m), 0.9999m);

        Assert.Equal(a, b);
    }

    [Fact]
    public void I_prodotti_di_prova_sono_otto_e_hanno_nomi_diversi()
    {
        Assert.Equal(8, TestProducts.All.Count);
        Assert.Equal(TestProducts.All.Count, TestProducts.All.Select(p => p.Name).Distinct().Count());
    }

    // ---------- Validazione ----------

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Un_nome_vuoto_viene_rifiutato(string name)
    {
        Assert.Throws<ArgumentException>(() => Bar(name: name));
    }

    [Fact]
    public void Un_nome_null_viene_rifiutato()
    {
        Assert.Throws<ArgumentNullException>(() => Bar(name: null!));
    }

    [Fact]
    public void Un_metallo_non_definito_viene_rifiutato()
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => Bar(metal: (Metal)0));
        Assert.Equal("metal", ex.ParamName);
    }

    [Fact]
    public void Una_forma_non_definita_viene_rifiutata()
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => Bar(form: (ProductForm)0));
        Assert.Equal("form", ex.ParamName);
    }

    [Fact]
    public void Un_peso_lordo_zero_viene_rifiutato()
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => Bar(grams: 0m));
        Assert.Equal("grossWeight", ex.ParamName);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-0.1")]
    [InlineData("1.5")]
    public void Un_titolo_fuori_intervallo_viene_rifiutato(string fineness)
    {
        // decimal non si può usare in [InlineData]: lo passiamo come testo e lo convertiamo.
        var value = decimal.Parse(fineness, System.Globalization.CultureInfo.InvariantCulture);

        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => Bar(fineness: value));
        Assert.Equal("fineness", ex.ParamName);
    }

    [Fact]
    public void Un_titolo_pari_a_1_e_accettato()
    {
        var p = Bar(fineness: 1m);

        Assert.Equal(p.GrossWeight, p.FineWeight);
    }

    /// <summary>Crea un lingotto valido, cambiando solo il dato che il test vuole provare.</summary>
    private static MetalProduct Bar(
        string name = "Lingotto di prova",
        Metal metal = Metal.Gold,
        ProductForm form = ProductForm.Bar,
        decimal grams = 100m,
        decimal fineness = 0.9999m)
        => new(name, metal, form, new Weight(grams), fineness);
}