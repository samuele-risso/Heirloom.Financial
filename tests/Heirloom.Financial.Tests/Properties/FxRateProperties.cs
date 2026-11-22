using FsCheck.Xunit;
using Heirloom.Financial.Domain;

namespace Heirloom.Financial.Tests.Properties;

/// <summary>
/// Regole di FxRate che devono valere per qualsiasi cambio valido.
/// Il cambio è generato in decimillesimi (ushort), per esempio 10800 = 1,08.
/// </summary>
public class FxRateProperties
{
    private static readonly DateOnly Day = new(2026, 11, 20);

    [Property]
    public bool L_inverso_dell_inverso_restituisce_il_cambio_originale(ushort tenThousandths)
    {
        if (tenThousandths == 0)
        {
            return true;
        }

        var fx = new FxRate(Currency.EUR, Currency.USD, tenThousandths / 10_000m, Day);
        var back = fx.Inverse().Inverse();

        // 1 / (1 / x) non è sempre esatto con i decimali: ammettiamo uno scarto minuscolo.
        return back.Base == fx.Base
            && back.Quote == fx.Quote
            && Math.Abs(back.Rate - fx.Rate) < 0.0000000001m;
    }

    [Property]
    public bool Cambio_per_inverso_fa_sempre_circa_1(ushort tenThousandths)
    {
        if (tenThousandths == 0)
        {
            return true;
        }

        var fx = new FxRate(Currency.EUR, Currency.USD, tenThousandths / 10_000m, Day);

        return Math.Abs(fx.Rate * fx.Inverse().Rate - 1m) < 0.0000000001m;
    }
}