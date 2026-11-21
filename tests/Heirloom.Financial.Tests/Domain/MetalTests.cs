using Heirloom.Financial.Domain;

namespace Heirloom.Financial.Tests.Domain;

public class MetalTests
{
    [Fact]
    public void Nessun_metallo_ha_valore_zero()
    {
        Assert.False(Enum.IsDefined(typeof(Metal), 0));
    }

    [Theory]
    [InlineData(Metal.Gold, "XAU")]
    [InlineData(Metal.Silver, "XAG")]
    public void ToIsoCode_restituisce_il_codice_corretto(Metal metal, string expected)
    {
        Assert.Equal(expected, metal.ToIsoCode());
    }

    [Fact]
    public void ToIsoCode_e_FromIsoCode_sono_inversi_per_tutti_i_metalli()
    {
        // Se aggiungi un metallo all'enum e dimentichi una delle due funzioni, questo test fallisce.
        foreach (var metal in Enum.GetValues<Metal>())
        {
            Assert.Equal(metal, MetalExtensions.FromIsoCode(metal.ToIsoCode()));
        }
    }

    [Theory]
    [InlineData("xau", Metal.Gold)]
    [InlineData(" XAG ", Metal.Silver)]
    public void FromIsoCode_ignora_maiuscole_e_spazi(string code, Metal expected)
    {
        Assert.Equal(expected, MetalExtensions.FromIsoCode(code));
    }

    [Theory]
    [InlineData("")]
    [InlineData("XPT")]
    [InlineData("ORO")]
    public void FromIsoCode_rifiuta_codici_sconosciuti(string code)
    {
        var ex = Assert.Throws<ArgumentException>(() => MetalExtensions.FromIsoCode(code));
        Assert.Equal("code", ex.ParamName);
    }

    [Fact]
    public void FromIsoCode_rifiuta_null()
    {
        Assert.Throws<ArgumentNullException>(() => MetalExtensions.FromIsoCode(null!));
    }

    [Fact]
    public void ToIsoCode_rifiuta_valori_non_definiti()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ((Metal)0).ToIsoCode());
        Assert.Throws<ArgumentOutOfRangeException>(() => ((Metal)99).ToIsoCode());
    }
}
