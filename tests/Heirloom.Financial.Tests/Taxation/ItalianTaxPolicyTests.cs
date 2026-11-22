using Heirloom.Financial.Domain;
using Heirloom.Financial.Taxation;

namespace Heirloom.Financial.Tests.Taxation;

public class ItalianTaxPolicyTests
{
    private static Money Eur(decimal amount) => new(amount, Currency.EUR);
    private static readonly ItalianTaxPolicy Policy = ItalianTaxPolicy.Standard;

    [Fact]
    public void L_aliquota_standard_e_il_26_per_cento()
    {
        Assert.Equal(0.26m, ItalianTaxPolicy.StandardRate);
        Assert.Equal(0.26m, Policy.Rate);
    }

    [Fact]
    public void Con_fattura_si_paga_sulla_plusvalenza()
    {
        Assert.Equal(Eur(130m), Policy.EstimateTax(Eur(2500m), Eur(2000m), hasDocumentedCost: true));
    }

    [Fact]
    public void Con_fattura_e_in_perdita_non_si_paga_nulla()
    {
        Assert.Equal(Eur(0m), Policy.EstimateTax(Eur(1800m), Eur(2000m), hasDocumentedCost: true));
    }

    [Fact]
    public void Con_fattura_e_a_pareggio_non_si_paga_nulla()
    {
        Assert.Equal(Eur(0m), Policy.EstimateTax(Eur(2000m), Eur(2000m), hasDocumentedCost: true));
    }

    [Fact]
    public void Senza_fattura_si_paga_sull_intero_valore()
    {
        Assert.Equal(Eur(650m), Policy.EstimateTax(Eur(2500m), Eur(2000m), hasDocumentedCost: false));
    }

    [Fact]
    public void Costo_sconosciuto_si_paga_sull_intero_valore_anche_con_fattura()
    {
        Assert.Equal(Eur(650m), Policy.EstimateTax(Eur(2500m), null, hasDocumentedCost: true));
        Assert.Equal(Eur(650m), Policy.EstimateTax(Eur(2500m), null, hasDocumentedCost: false));
    }

    [Fact]
    public void Un_aliquota_diversa_si_applica_allo_stesso_modo()
    {
        var policy = new ItalianTaxPolicy(0.125m);

        Assert.Equal(Eur(62.5m), policy.EstimateTax(Eur(2500m), Eur(2000m), hasDocumentedCost: true));
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(1.0)]
    [InlineData(1.5)]
    public void Aliquote_fuori_intervallo_vengono_rifiutate(double rate)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ItalianTaxPolicy((decimal)rate));
    }

    [Fact]
    public void Aliquota_zero_e_ammessa()
    {
        Assert.Equal(Eur(0m), new ItalianTaxPolicy(0m).EstimateTax(Eur(2500m), null, false));
    }

    [Fact]
    public void Valore_negativo_viene_rifiutato()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Policy.EstimateTax(Eur(-1m), null, false));
    }

    [Fact]
    public void Costo_in_altra_valuta_viene_rifiutato()
    {
        Assert.Throws<ArgumentException>(() =>
            Policy.EstimateTax(Eur(2500m), new Money(2000m, Currency.USD), hasDocumentedCost: true));
    }
}