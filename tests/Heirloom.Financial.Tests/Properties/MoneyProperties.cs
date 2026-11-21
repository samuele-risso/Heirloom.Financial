using FsCheck.Xunit;
using Heirloom.Financial.Domain;

namespace Heirloom.Financial.Tests.Properties;

/// <summary>
/// Regole di Money che devono valere per qualsiasi importo.
/// Gli importi sono generati come centesimi interi (int), positivi o negativi.
/// </summary>
public class MoneyProperties
{
    private static Money Eur(int cents) => new(cents / 100m, Currency.EUR);
    private static Money Usd(int cents) => new(cents / 100m, Currency.USD);

    [Property]
    public bool La_somma_e_commutativa(int a, int b)
    {
        return Eur(a) + Eur(b) == Eur(b) + Eur(a);
    }

    [Property]
    public bool Aggiungere_e_togliere_lo_stesso_importo_non_cambia_nulla(int a, int b)
    {
        return (Eur(a) + Eur(b)) - Eur(b) == Eur(a);
    }

    [Property]
    public bool Il_doppio_cambio_di_segno_restituisce_l_importo(int a)
    {
        return -(-Eur(a)) == Eur(a);
    }

    [Property]
    public bool Sommare_valute_diverse_fallisce_sempre(int a, int b)
    {
        try
        {
            _ = Eur(a) + Usd(b);
            return false;
        }
        catch (InvalidOperationException)
        {
            return true;
        }
    }

    [Property]
    public bool La_somma_mantiene_la_valuta(int a, int b)
    {
        return (Usd(a) + Usd(b)).Currency == Currency.USD;
    }
}