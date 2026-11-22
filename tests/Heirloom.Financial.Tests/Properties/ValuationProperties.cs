using FsCheck.Xunit;
using Heirloom.Financial.Domain;
using Heirloom.Financial.Tests.TestData;
using Heirloom.Financial.Valuation;

namespace Heirloom.Financial.Tests.Properties;

/// <summary>
/// Regole della valutazione che devono valere per qualsiasi spot, quantità e percentuale.
/// Spot in centesimi (uint), quantità in pezzi (ushort), percentuali in decimillesimi (ushort).
/// </summary>
public class ValuationProperties
{
    private static readonly DateOnly Day = new(2026, 11, 20);

    [Property]
    public bool Raddoppiare_la_quantita_raddoppia_il_valore(uint spotCents, ushort quantity)
    {
        if (spotCents == 0)
        {
            return true;
        }

        var spot = new SpotPrice(Metal.Gold, new Money(spotCents / 100m, Currency.USD), Day);

        var single = MeltValueCalculator.MeltValue(TestProducts.Krugerrand1Oz, quantity, spot);
        var doubled = MeltValueCalculator.MeltValue(TestProducts.Krugerrand1Oz, quantity * 2m, spot);

        // Tolleranza: la divisione per 31,1034768 arrotonda alla 28ª cifra significativa.
        return Math.Abs((doubled - single * 2m).Amount) < 0.000000001m;
    }

    [Property]
    public bool Riacquisto_non_supera_lo_spot_e_lo_spot_non_supera_il_premio(uint spotCents, ushort premium, ushort discount)
    {
        if (spotCents == 0 || discount >= 10_000)
        {
            return true;
        }

        var spot = new SpotPrice(Metal.Gold, new Money(spotCents / 100m, Currency.USD), Day);
        var forms = new[] { ProductForm.Coin, ProductForm.Bar };

        IValuationStrategy withPremium = new SpotPlusPremiumValuation(forms.ToDictionary(f => f, _ => premium / 10_000m));
        IValuationStrategy buyback = new BuybackValuation(forms.ToDictionary(f => f, _ => discount / 10_000m));

        return TestProducts.All
            .Where(p => p.Metal == Metal.Gold)
            .All(p =>
                buyback.UnitValue(p, spot) <= SpotValuation.Instance.UnitValue(p, spot) &&
                SpotValuation.Instance.UnitValue(p, spot) <= withPremium.UnitValue(p, spot));
    }
}