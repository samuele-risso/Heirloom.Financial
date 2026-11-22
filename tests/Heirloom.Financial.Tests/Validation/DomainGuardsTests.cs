using Heirloom.Financial.Domain;
using Heirloom.Financial.Tests.TestData;
using Heirloom.Financial.Validation;

namespace Heirloom.Financial.Tests.Validation;

public class DomainGuardsTests
{
    private static Money Eur(decimal amount) => new(amount, Currency.EUR);
    private static Money Usd(decimal amount) => new(amount, Currency.USD);

    // ---------- Il nome del parametro arriva in automatico ----------

    [Fact]
    public void Il_nome_del_parametro_e_quello_della_variabile_passata()
    {
        decimal quantity = 0m;

        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => DomainGuards.Positive(quantity));

        Assert.Equal("quantity", ex.ParamName);
    }

    // ---------- Enum ----------

    [Theory]
    [InlineData(0)]
    [InlineData(99)]
    public void Defined_rifiuta_valori_non_definiti(int value)
    {
        var metal = (Metal)value;

        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => DomainGuards.Defined(metal));

        Assert.Equal("metal", ex.ParamName);
    }

    [Fact]
    public void Defined_restituisce_il_valore_se_valido()
    {
        Assert.Equal(Metal.Silver, DomainGuards.Defined(Metal.Silver));
    }

    // ---------- Numeri, importi, pesi ----------

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Positive_rifiuta_zero_e_negativi(int value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => DomainGuards.Positive((decimal)value));
        Assert.Throws<ArgumentOutOfRangeException>(() => DomainGuards.Positive(Eur(value)));
    }

    [Fact]
    public void Positive_rifiuta_il_peso_zero()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => DomainGuards.Positive(Weight.Zero));
        Assert.Equal(new Weight(1m), DomainGuards.Positive(new Weight(1m)));
    }

    [Fact]
    public void NonNegative_accetta_lo_zero_e_rifiuta_i_negativi()
    {
        Assert.Equal(Eur(0m), DomainGuards.NonNegative(Eur(0m)));
        Assert.Throws<ArgumentOutOfRangeException>(() => DomainGuards.NonNegative(Eur(-0.01m)));
    }

    // ---------- Titolo ----------

    [Theory]
    [InlineData("1.5")]
    [InlineData("0")]
    [InlineData("-0.1")]
    public void Fineness_rifiuta_i_titoli_fuori_intervallo(string text)
    {
        var fineness = decimal.Parse(text, System.Globalization.CultureInfo.InvariantCulture);

        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => DomainGuards.Fineness(fineness));

        Assert.Equal("fineness", ex.ParamName);
    }

    [Fact]
    public void Fineness_accetta_1()
    {
        Assert.Equal(1m, DomainGuards.Fineness(1m));
    }

    // ---------- Valute ----------

    [Fact]
    public void SameCurrency_rifiuta_valute_diverse_e_le_nomina()
    {
        var fees = Usd(5m);

        var ex = Assert.Throws<ArgumentException>(() => DomainGuards.SameCurrency(Eur(100m), fees));

        Assert.Equal("fees", ex.ParamName);
        Assert.Contains("EUR", ex.Message);
        Assert.Contains("USD", ex.Message);
    }

    // ---------- Date ----------

    [Fact]
    public void NotInFuture_rifiuta_una_data_successiva_a_oggi()
    {
        var today = new DateOnly(2026, 11, 22);

        Assert.Equal(today, DomainGuards.NotInFuture(today, today));
        Assert.Throws<ArgumentOutOfRangeException>(() => DomainGuards.NotInFuture(today.AddDays(1), today));
    }

    // ---------- Vendite ----------

    [Fact]
    public void NoOversell_accetta_le_storie_di_prova()
    {
        DomainGuards.NoOversell(TestLots.WithPartialSale);
        DomainGuards.NoOversell(TestLots.MixedMultiYear);
    }

    [Fact]
    public void NoOversell_rifiuta_una_vendita_superiore_al_posseduto_e_nomina_prodotto_e_data()
    {
        var lots = new[]
        {
            Lot.Buy(TestProducts.Marengo, 2m, Eur(480m), Eur(0m), new DateOnly(2024, 1, 10)),
            Lot.Sell(TestProducts.Marengo, 3m, Eur(520m), Eur(0m), new DateOnly(2024, 6, 1)),
        };

        var ex = Assert.Throws<ArgumentException>(() => DomainGuards.NoOversell(lots));

        Assert.Equal("lots", ex.ParamName);
        Assert.Contains("Marengo", ex.Message);
        Assert.Contains("2024-06-01", ex.Message);
    }

    [Fact]
    public void NoOversell_controlla_l_ordine_cronologico_non_quello_dell_elenco()
    {
        // La vendita è prima nell'elenco ma dopo nel tempo: è valida.
        var lots = new[]
        {
            Lot.Sell(TestProducts.Marengo, 1m, Eur(520m), Eur(0m), new DateOnly(2024, 6, 1)),
            Lot.Buy(TestProducts.Marengo, 1m, Eur(480m), Eur(0m), new DateOnly(2024, 1, 10)),
        };

        DomainGuards.NoOversell(lots);
    }

    [Fact]
    public void NoOversell_rifiuta_una_vendita_prima_dell_acquisto()
    {
        var lots = new[]
        {
            Lot.Buy(TestProducts.Marengo, 1m, Eur(480m), Eur(0m), new DateOnly(2024, 6, 1)),
            Lot.Sell(TestProducts.Marengo, 1m, Eur(520m), Eur(0m), new DateOnly(2024, 1, 10)),
        };

        Assert.Throws<ArgumentException>(() => DomainGuards.NoOversell(lots));
    }

    [Fact]
    public void NoOversell_conta_gli_acquisti_dello_stesso_giorno_prima_delle_vendite()
    {
        var day = new DateOnly(2024, 6, 1);
        var lots = new[]
        {
            Lot.Sell(TestProducts.Marengo, 1m, Eur(520m), Eur(0m), day),
            Lot.Buy(TestProducts.Marengo, 1m, Eur(480m), Eur(0m), day),
        };

        DomainGuards.NoOversell(lots);
    }

    [Fact]
    public void NoOversell_tiene_separati_i_prodotti()
    {
        // Possiedo 2 Marenghi, ma provo a vendere 1 Sterlina.
        var lots = new[]
        {
            Lot.Buy(TestProducts.Marengo, 2m, Eur(480m), Eur(0m), new DateOnly(2024, 1, 10)),
            Lot.Sell(TestProducts.Sovereign, 1m, Eur(600m), Eur(0m), new DateOnly(2024, 6, 1)),
        };

        Assert.Throws<ArgumentException>(() => DomainGuards.NoOversell(lots));
    }
}