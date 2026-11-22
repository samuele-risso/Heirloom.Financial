using Heirloom.Financial.Domain;

namespace Heirloom.Financial.Tests.Domain;

public class MetalProductFromFineWeightTests
{
    private static MetalProduct Krugerrand(decimal gross = 33.93m, decimal fine = 31.10m) =>
        MetalProduct.FromFineWeight("Krugerrand", Metal.Gold, ProductForm.Coin, new Weight(gross), new Weight(fine));

    [Fact]
    public void Il_peso_fino_resta_esattamente_quello_del_catalogo()
    {
        var product = Krugerrand();

        Assert.Equal(31.10m, product.FineWeight.Grams);
        Assert.Equal(33.93m, product.GrossWeight.Grams);
    }

    [Fact]
    public void Il_titolo_viene_ricavato_dai_due_pesi()
    {
        var product = Krugerrand();

        Assert.True(Math.Abs(product.Fineness - 31.10m / 33.93m) < 0.0000000001m);
    }

    [Fact]
    public void Peso_fino_uguale_al_lordo_da_titolo_1()
    {
        var product = Krugerrand(gross: 31.1035m, fine: 31.1035m);

        Assert.Equal(1m, product.Fineness);
    }

    [Fact]
    public void Peso_fino_maggiore_del_lordo_viene_rifiutato()
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => Krugerrand(gross: 30m, fine: 31m));

        Assert.Equal("fineWeight", ex.ParamName);
    }

    [Fact]
    public void Peso_fino_zero_viene_rifiutato()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Krugerrand(fine: 0m));
    }

    [Fact]
    public void Nome_vuoto_viene_rifiutato()
    {
        Assert.Throws<ArgumentException>(() =>
            MetalProduct.FromFineWeight(" ", Metal.Gold, ProductForm.Coin, new Weight(10m), new Weight(9m)));
    }

    [Fact]
    public void Due_prodotti_con_gli_stessi_dati_sono_uguali()
    {
        Assert.Equal(Krugerrand(), Krugerrand());
    }

    [Fact]
    public void Il_costruttore_classico_continua_a_calcolare_lordo_per_titolo()
    {
        var product = new MetalProduct("Lingotto", Metal.Gold, ProductForm.Bar, new Weight(100m), 0.9999m);

        Assert.Equal(99.99m, product.FineWeight.Grams);
    }
}