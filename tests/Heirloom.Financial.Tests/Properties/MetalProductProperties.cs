using FsCheck.Xunit;
using Heirloom.Financial.Domain;

namespace Heirloom.Financial.Tests.Properties;

/// <summary>
/// Regole di MetalProduct che devono valere per qualsiasi peso e titolo validi.
/// Il peso è generato in milligrammi (uint), il titolo in decimillesimi (ushort).
/// </summary>
public class MetalProductProperties
{
    private static MetalProduct? Create(uint milligrams, ushort tenThousandths)
    {
        // Scarta i casi non validi: peso zero, titolo zero o superiore a 1.
        if (milligrams == 0 || tenThousandths == 0 || tenThousandths > 10_000)
        {
            return null;
        }

        return new MetalProduct("Prodotto", Metal.Gold, ProductForm.Bar,
            new Weight(milligrams / 1000m), tenThousandths / 10_000m);
    }

    [Property]
    public bool Il_peso_fino_non_supera_mai_il_peso_lordo(uint milligrams, ushort tenThousandths)
    {
        var p = Create(milligrams, tenThousandths);

        return p is null || p.FineWeight <= p.GrossWeight;
    }

    [Property]
    public bool Il_peso_fino_e_peso_lordo_per_titolo(uint milligrams, ushort tenThousandths)
    {
        var p = Create(milligrams, tenThousandths);

        return p is null || p.FineWeight.Grams == p.GrossWeight.Grams * p.Fineness;
    }
}