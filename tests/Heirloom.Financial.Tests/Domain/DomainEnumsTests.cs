using Heirloom.Financial.Domain;

namespace Heirloom.Financial.Tests.Domain;

/// <summary>
/// Regole comuni a tutti gli enum del dominio.
/// Quando aggiungi un nuovo enum in Domain, aggiungi qui una riga [InlineData].
/// </summary>
public class DomainEnumsTests
{
    [Theory]
    [InlineData(typeof(Metal))]
    [InlineData(typeof(ProductForm))]
    [InlineData(typeof(Currency))]
    [InlineData(typeof(LotSide))]
    public void Nessun_enum_del_dominio_ha_valore_zero(Type enumType)
    {
        // Lo 0 è il valore di default di ogni enum: se fosse valido,
        // un campo dimenticato diventerebbe quel valore in silenzio.
        Assert.False(Enum.IsDefined(enumType, 0));
    }
}