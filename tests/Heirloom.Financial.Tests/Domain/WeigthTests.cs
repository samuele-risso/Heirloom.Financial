using Heirloom.Financial.Domain;

namespace Heirloom.Financial.Tests.Domain;

/// <summary>
/// Specifica di Weight (HA-006). Scrivi Weight finché tutti questi test sono verdi.
/// </summary>
public class WeightTests
{
    // ---------- Costruzione ----------

    [Fact]
    public void Il_peso_si_crea_in_grammi()
    {
        var w = new Weight(100m);

        Assert.Equal(100m, w.Grams);
    }

    [Fact]
    public void FromGrams_equivale_al_costruttore()
    {
        Assert.Equal(new Weight(250m), Weight.FromGrams(250m));
    }

    [Fact]
    public void Zero_e_un_peso_valido_e_coincide_con_il_default()
    {
        Assert.Equal(0m, Weight.Zero.Grams);
        Assert.Equal(default(Weight), Weight.Zero);
    }

    [Fact]
    public void Un_peso_negativo_viene_rifiutato()
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => new Weight(-0.01m));

        Assert.Equal("grams", ex.ParamName);
    }

    // ---------- Once troy ----------

    [Fact]
    public void La_costante_e_31_1034768_grammi_per_oncia_troy()
    {
        Assert.Equal(31.1034768m, Weight.GramsPerTroyOunce);
    }

    [Fact]
    public void Un_oncia_troy_vale_31_1034768_grammi()
    {
        Assert.Equal(31.1034768m, Weight.FromTroyOunces(1m).Grams);
    }

    [Fact]
    public void Un_oncia_troy_andata_e_ritorno_restituisce_esattamente_1()
    {
        Assert.Equal(1m, Weight.FromTroyOunces(1m).TroyOunces);
    }

    [Fact]
    public void Cento_grammi_sono_circa_3_215075_once_troy()
    {
        var w = new Weight(100m);

        Assert.Equal(3.215075m, Math.Round(w.TroyOunces, 6));
    }

    [Fact]
    public void Un_chilo_di_argento_a_999_contiene_circa_32_1186_once_di_fino()
    {
        // Anticipo di HA-008: 1000 g lordi x titolo 0,999 = 999 g di argento puro.
        var fine = new Weight(1000m) * 0.999m;

        Assert.Equal(32.1186m, Math.Round(fine.TroyOunces, 4));
    }

    // ---------- Operazioni ----------

    [Fact]
    public void Somma_di_due_pesi()
    {
        Assert.Equal(new Weight(15m), new Weight(10m) + new Weight(5m));
    }

    [Fact]
    public void Sottrazione_di_due_pesi()
    {
        Assert.Equal(new Weight(6m), new Weight(10m) - new Weight(4m));
    }

    [Fact]
    public void Una_sottrazione_che_va_sotto_zero_viene_rifiutata()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Weight(4m) - new Weight(10m));
    }

    [Fact]
    public void Moltiplicazione_per_un_numero_in_entrambi_i_versi()
    {
        var w = new Weight(10m);

        Assert.Equal(new Weight(30m), w * 3m);
        Assert.Equal(new Weight(30m), 3m * w);
    }

    [Fact]
    public void Moltiplicazione_per_un_numero_negativo_viene_rifiutata()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Weight(10m) * -1m);
    }

    [Fact]
    public void Divisione_per_un_numero()
    {
        Assert.Equal(new Weight(2.5m), new Weight(10m) / 4m);
    }

    [Fact]
    public void Divisione_per_zero_viene_rifiutata()
    {
        Assert.Throws<DivideByZeroException>(() => new Weight(10m) / 0m);
    }

    // ---------- Confronto e uguaglianza ----------

    [Fact]
    public void Confronto_tra_pesi()
    {
        var small = new Weight(5m);
        var big = new Weight(10m);

        Assert.True(small < big);
        Assert.True(big > small);
        Assert.True(small <= new Weight(5m));
        Assert.True(big >= new Weight(10m));
        Assert.True(small.CompareTo(big) < 0);
    }

    [Fact]
    public void Due_pesi_con_gli_stessi_grammi_sono_uguali()
    {
        // 10,0 e 10 sono lo stesso valore decimal, quindi lo stesso peso.
        Assert.Equal(new Weight(10.0m), new Weight(10m));
        Assert.True(new Weight(10m) == Weight.FromGrams(10m));
    }

    [Fact]
    public void I_pesi_si_possono_ordinare()
    {
        var weights = new[] { new Weight(3m), new Weight(1m), new Weight(2m) };

        var sorted = weights.Order().ToArray();

        Assert.Equal(new[] { new Weight(1m), new Weight(2m), new Weight(3m) }, sorted);
    }
}