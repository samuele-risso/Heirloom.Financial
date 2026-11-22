using FsCheck.Xunit;
using Heirloom.Financial.Allocation;
using Heirloom.Financial.Domain;
using Heirloom.Financial.Positions;
using Heirloom.Financial.Tests.TestData;
using Heirloom.Financial.Valuation;

namespace Heirloom.Financial.Tests.Properties;

/// <summary>
/// Regole dell'allocazione che devono valere per qualsiasi portafoglio.
/// Il portafoglio è generato come quantità casuali (0..20) di quattro prodotti diversi.
/// </summary>
public class AllocationProperties
{
    private static readonly DateOnly Day = new(2024, 1, 10);
    private static readonly SpotPrice Gold = new(Metal.Gold, new Money(2500m, Currency.EUR), Day);
    private static readonly SpotPrice Silver = new(Metal.Silver, new Money(30m, Currency.EUR), Day);

    private static PnLSummary Portfolio(byte krugerrand, byte goldBar, byte maple, byte silverBar)
    {
        var price = new Money(100m, Currency.EUR);
        var fees = Money.Zero(Currency.EUR);

        var lots = new (MetalProduct Product, int Quantity)[]
            {
                (TestProducts.Krugerrand1Oz, krugerrand % 21),
                (TestProducts.GoldBar100g, goldBar % 21),
                (TestProducts.SilverMapleLeaf1Oz, maple % 21),
                (TestProducts.SilverBar1kg, silverBar % 21),
            }
            .Where(x => x.Quantity > 0)
            .Select(x => Lot.Buy(x.Product, x.Quantity, price, fees, Day));

        var basis = CostBasisCalculator.Calculate(lots, CostBasisMethod.Fifo, Currency.EUR);
        return PnLCalculator.Calculate(basis, [Gold, Silver], SpotValuation.Instance);
    }

    [Property(MaxTest = 300)]
    public bool Le_percentuali_sommano_a_100_sia_sul_valore_sia_sul_peso(byte a, byte b, byte c, byte d)
    {
        var summary = Portfolio(a, b, c, d);
        if (summary.Positions.Count == 0)
        {
            return true;
        }

        return new[] { AllocationBasis.Value, AllocationBasis.FineWeight }.All(basis =>
        {
            var slices = AllocationCalculator.Calculate(summary, AllocationGrouping.MetalAndForm, basis);
            return Math.Abs(slices.Sum(s => s.Percent!.Value) - 1m) < 0.0000001m;
        });
    }

    [Property(MaxTest = 300)]
    public bool Dopo_l_arrotondamento_la_somma_e_esattamente_100(byte a, byte b, byte c, byte d)
    {
        var summary = Portfolio(a, b, c, d);
        if (summary.Positions.Count == 0)
        {
            return true;
        }

        var slices = AllocationCalculator.Calculate(summary, AllocationGrouping.MetalAndForm, AllocationBasis.Value);
        var rounded = AllocationCalculator.RoundPercentages(slices);

        return rounded.Sum(s => s.Percent!.Value) == 1m;
    }

    [Property(MaxTest = 300)]
    public bool Il_peso_delle_fette_somma_al_peso_del_portafoglio(byte a, byte b, byte c, byte d)
    {
        var summary = Portfolio(a, b, c, d);
        var total = summary.Positions.Aggregate(Weight.Zero, (sum, p) => sum + p.Position.FineWeight);

        return new[] { AllocationGrouping.Metal, AllocationGrouping.Form, AllocationGrouping.MetalAndForm }.All(grouping =>
            AllocationCalculator.Calculate(summary, grouping, AllocationBasis.FineWeight)
                .Aggregate(Weight.Zero, (sum, s) => sum + s.FineWeight) == total);
    }
}