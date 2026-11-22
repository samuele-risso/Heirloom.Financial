using FsCheck.Xunit;
using Heirloom.Financial.Domain;
using Heirloom.Financial.Exchange;

namespace Heirloom.Financial.Tests.Properties;

/// <summary>
/// Regole della conversione che devono valere per qualsiasi importo e cambio.
/// Importo in centesimi (int), cambio in decimillesimi (ushort, es. 10800 = 1,08).
/// </summary>
public class CurrencyConverterProperties
{
    private static readonly DateOnly Day = new(2026, 11, 20);

    [Property]
    public bool Andata_e_ritorno_restituisce_l_importo_iniziale(int cents, ushort tenThousandths)
    {
        if (tenThousandths == 0)
        {
            return true;
        }

        var rate = new FxRate(Currency.EUR, Currency.USD, tenThousandths / 10_000m, Day);
        var eur = new Money(cents / 100m, Currency.EUR);

        var back = CurrencyConverter.Convert(CurrencyConverter.Convert(eur, Currency.USD, rate), Currency.EUR, rate);

        return back.Currency == Currency.EUR && Math.Abs(back.Amount - eur.Amount) < 0.0000001m;
    }

    [Property]
    public bool La_conversione_e_proporzionale(int cents, ushort tenThousandths)
    {
        if (tenThousandths == 0)
        {
            return true;
        }

        var rate = new FxRate(Currency.EUR, Currency.USD, tenThousandths / 10_000m, Day);
        var eur = new Money(cents / 100m, Currency.EUR);

        var single = CurrencyConverter.Convert(eur, Currency.USD, rate);
        var doubled = CurrencyConverter.Convert(eur * 2m, Currency.USD, rate);

        return doubled == single * 2m;
    }
}