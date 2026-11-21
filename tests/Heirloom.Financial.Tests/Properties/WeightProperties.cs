using FsCheck.Xunit;
using Heirloom.Financial.Domain;

namespace Heirloom.Financial.Tests.Properties;

/// <summary>
/// Regole di Weight che devono valere per qualsiasi peso.
/// I pesi sono generati come milligrammi interi non negativi (uint),
/// così FsCheck non produce mai pesi negativi.
/// </summary>
public class WeightProperties
{
    private static Weight FromMilligrams(uint mg) => new(mg / 1000m);

    [Property]
    public bool Grammi_once_grammi_restituisce_lo_stesso_peso(uint mg)
    {
        var w = FromMilligrams(mg);

        var roundTrip = Weight.FromTroyOunces(w.TroyOunces);

        // La divisione per 31,1034768 non è esatta: ammettiamo uno scarto minuscolo.
        return Math.Abs(roundTrip.Grams - w.Grams) < 0.000000001m;
    }

    [Property]
    public bool La_somma_e_commutativa(uint a, uint b)
    {
        return FromMilligrams(a) + FromMilligrams(b) == FromMilligrams(b) + FromMilligrams(a);
    }

    [Property]
    public bool Aggiungere_e_togliere_lo_stesso_peso_non_cambia_nulla(uint a, uint b)
    {
        var wa = FromMilligrams(a);
        var wb = FromMilligrams(b);

        return (wa + wb) - wb == wa;
    }

    [Property]
    public bool Il_peso_in_once_e_proporzionale_ai_grammi(uint mg, byte factor)
    {
        var w = FromMilligrams(mg);

        return Math.Abs((w * factor).TroyOunces - w.TroyOunces * factor) < 0.000000001m;
    }
}