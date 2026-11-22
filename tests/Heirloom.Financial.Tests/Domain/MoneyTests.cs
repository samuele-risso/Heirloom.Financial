using Heirloom.Financial.Domain;

namespace Heirloom.Financial.Tests.Domain;

public class MoneyTests
{
    private static Money Eur(decimal amount) => new(amount, Currency.EUR);
    private static Money Usd(decimal amount) => new(amount, Currency.USD);

    // ---------- Costruzione ----------

    [Fact]
    public void Un_importo_ha_valore_e_valuta()
    {
        var m = new Money(100.50m, Currency.EUR);

        Assert.Equal(100.50m, m.Amount);
        Assert.Equal(Currency.EUR, m.Currency);
    }

    [Fact]
    public void Un_importo_puo_essere_negativo()
    {
        var loss = Eur(-250m);

        Assert.Equal(-250m, loss.Amount);
        Assert.True(loss.IsNegative);
    }

    [Fact]
    public void Una_valuta_non_definita_viene_rifiutata()
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => new Money(10m, (Currency)0));

        Assert.Equal("currency", ex.ParamName);
    }

    [Fact]
    public void Zero_restituisce_zero_nella_valuta_richiesta()
    {
        var zero = Money.Zero(Currency.USD);

        Assert.Equal(0m, zero.Amount);
        Assert.Equal(Currency.USD, zero.Currency);
        Assert.True(zero.IsZero);
    }

    // ---------- Operazioni nella stessa valuta ----------

    [Fact]
    public void Somma_nella_stessa_valuta()
    {
        Assert.Equal(Eur(150m), Eur(100m) + Eur(50m));
    }

    [Fact]
    public void Sottrazione_nella_stessa_valuta_anche_con_risultato_negativo()
    {
        Assert.Equal(Eur(30m), Eur(100m) - Eur(70m));
        Assert.Equal(Eur(-20m), Eur(50m) - Eur(70m));
    }

    [Fact]
    public void Cambio_di_segno()
    {
        Assert.Equal(Eur(-10m), -Eur(10m));
    }

    [Fact]
    public void Moltiplicazione_per_un_numero_in_entrambi_i_versi()
    {
        // Esempio tipico: prezzo unitario per quantità.
        Assert.Equal(Eur(6000m), Eur(2000m) * 3m);
        Assert.Equal(Eur(6000m), 3m * Eur(2000m));
    }

    [Fact]
    public void Divisione_per_un_numero()
    {
        // Esempio tipico: costo totale diviso quantità = prezzo medio.
        Assert.Equal(Eur(1100m), Eur(4400m) / 4m);
    }

    [Fact]
    public void Divisione_per_zero_viene_rifiutata()
    {
        Assert.Throws<DivideByZeroException>(() => Eur(100m) / 0m);
    }

    [Fact]
    public void Divisione_tra_importi_restituisce_il_rapporto()
    {
        // Guadagno 150 su costo 1000 = rendimento 0,15 (15%).
        decimal ratio = Eur(150m) / Eur(1000m);

        Assert.Equal(0.15m, ratio);
    }

    [Fact]
    public void Money_non_arrotonda_mai()
    {
        // 100 / 3 deve restare con tutti i decimali: l'arrotondamento si fa solo in uscita.
        var third = Eur(100m) / 3m;

        Assert.Equal(100m / 3m, third.Amount);
        Assert.NotEqual(33.33m, third.Amount);
    }

    // ---------- Valute diverse ----------

    [Fact]
    public void Sommare_euro_e_dollari_viene_rifiutato_con_un_messaggio_chiaro()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => Eur(10m) + Usd(10m));

        Assert.Contains("EUR", ex.Message);
        Assert.Contains("USD", ex.Message);
    }

    [Fact]
    public void Sottrarre_valute_diverse_viene_rifiutato()
    {
        Assert.Throws<InvalidOperationException>(() => Eur(10m) - Usd(10m));
    }

    [Fact]
    public void Dividere_valute_diverse_viene_rifiutato()
    {
        Assert.Throws<InvalidOperationException>(() => Eur(10m) / Usd(10m));
    }

    [Fact]
    public void Confrontare_valute_diverse_viene_rifiutato()
    {
        Assert.Throws<InvalidOperationException>(() => Eur(10m) < Usd(20m));
    }

    // ---------- Confronto e uguaglianza ----------

    [Fact]
    public void Confronto_nella_stessa_valuta()
    {
        Assert.True(Eur(5m) < Eur(10m));
        Assert.True(Eur(10m) > Eur(5m));
        Assert.True(Eur(5m) <= Eur(5m));
        Assert.True(Eur(5m) >= Eur(5m));
    }

    [Fact]
    public void Stesso_importo_in_valute_diverse_non_e_uguale()
    {
        Assert.NotEqual(Eur(10m), Usd(10m));
    }

    [Fact]
    public void Stesso_importo_nella_stessa_valuta_e_uguale()
    {
        Assert.Equal(Eur(10.0m), Eur(10m));
    }

    // ---------- Testo ----------

    [Fact]
    public void ToString_usa_il_punto_decimale_e_il_codice_ISO()
    {
        Assert.Equal("1234.56 EUR", Eur(1234.56m).ToString());
        Assert.Equal("-5 USD", Usd(-5m).ToString());
    }
}