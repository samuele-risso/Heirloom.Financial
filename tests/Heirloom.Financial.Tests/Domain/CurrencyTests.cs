using Heirloom.Financial.Domain;

namespace Heirloom.Financial.Tests.Domain;

public class CurrencyTests
{
    [Theory]
    [InlineData(Currency.EUR, "EUR")]
    [InlineData(Currency.USD, "USD")]
    public void ToIsoCode_restituisce_il_codice_corretto(Currency currency, string expected)
    {
        Assert.Equal(expected, currency.ToIsoCode());
    }

    [Fact]
    public void ToIsoCode_e_FromIsoCode_sono_inversi_per_tutte_le_valute()
    {
        // Se aggiungi una valuta all'enum e dimentichi una delle due funzioni, questo test fallisce.
        foreach (var currency in Enum.GetValues<Currency>())
        {
            Assert.Equal(currency, CurrencyExtensions.FromIsoCode(currency.ToIsoCode()));
        }
    }

    [Theory]
    [InlineData("eur", Currency.EUR)]
    [InlineData(" USD ", Currency.USD)]
    public void FromIsoCode_ignora_maiuscole_e_spazi(string code, Currency expected)
    {
        Assert.Equal(expected, CurrencyExtensions.FromIsoCode(code));
    }

    [Theory]
    [InlineData("")]
    [InlineData("GBP")]
    [InlineData("EURO")]
    [InlineData("1")]
    public void FromIsoCode_rifiuta_codici_sconosciuti(string code)
    {
        // "1" verrebbe accettato da Enum.Parse: qui deve essere rifiutato.
        var ex = Assert.Throws<ArgumentException>(() => CurrencyExtensions.FromIsoCode(code));
        Assert.Equal("code", ex.ParamName);
    }

    [Fact]
    public void FromIsoCode_rifiuta_null()
    {
        Assert.Throws<ArgumentNullException>(() => CurrencyExtensions.FromIsoCode(null!));
    }

    [Fact]
    public void ToIsoCode_rifiuta_valori_non_definiti()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ((Currency)0).ToIsoCode());
        Assert.Throws<ArgumentOutOfRangeException>(() => ((Currency)99).ToIsoCode());
    }
}