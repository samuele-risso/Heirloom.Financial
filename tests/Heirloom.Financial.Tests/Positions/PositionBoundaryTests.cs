using Heirloom.Financial.Domain;
using Heirloom.Financial.Positions;
using Heirloom.Financial.Valuation;
using static Heirloom.Financial.Tests.TestData.OneOunce;

namespace Heirloom.Financial.Tests.Positions;

/// <summary>
/// I casi al confine di posizioni e guadagni: proporzioni della media ponderata,
/// ordine delle posizioni, spot ripetuti.
/// </summary>
public class PositionsBoundaryTests
{
    private static readonly DateOnly Day = new(2024, 1, 1);

    [Fact]
    public void Media_ponderata_la_parte_sconosciuta_esce_in_proporzione_ai_pezzi()
    {
        // 2 pezzi ereditati e 2 comprati: vendendone 1, escono mezzo ereditato e mezzo comprato.
        var lots = new[]
        {
            Lot.BuyWithUnknownCost(GoldCoin, 2m, Day, Currency.EUR),
            Lot.Buy(GoldCoin, 2m, Eur(2000m), Eur(0m), Day),
            Lot.Sell(GoldCoin, 1m, Eur(2200m), Eur(0m), Day.AddDays(10)),
        };

        var result = CostBasisCalculator.Calculate(lots, CostBasisMethod.WeightedAverage, Currency.EUR);

        var sale = Assert.Single(result.Sales);
        Assert.Equal(0.5m, sale.UnknownCostQuantity);
        Assert.Equal(Eur(1000m), sale.CostOfSold);
        Assert.Equal(1.5m, Assert.Single(result.Positions).UnknownCostQuantity);
    }

    [Fact]
    public void Media_ponderata_con_tre_quarti_sconosciuti()
    {
        var lots = new[]
        {
            Lot.BuyWithUnknownCost(GoldCoin, 3m, Day, Currency.EUR),
            Lot.Buy(GoldCoin, 1m, Eur(2000m), Eur(0m), Day),
            Lot.Sell(GoldCoin, 2m, Eur(2200m), Eur(0m), Day.AddDays(10)),
        };

        var sale = Assert.Single(CostBasisCalculator.Calculate(lots, CostBasisMethod.WeightedAverage, Currency.EUR).Sales);

        Assert.Equal(1.5m, sale.UnknownCostQuantity);
        Assert.Equal(Eur(1000m), sale.CostOfSold);
    }

    [Fact]
    public void Le_posizioni_dello_stesso_metallo_e_forma_sono_in_ordine_di_nome()
    {
        var alpha = new MetalProduct("Alfa", Metal.Gold, ProductForm.Coin, new Weight(10m), 0.9m);
        var beta = new MetalProduct("Beta", Metal.Gold, ProductForm.Coin, new Weight(10m), 0.9m);
        var lots = new[]
        {
            Lot.Buy(beta, 1m, Eur(500m), Eur(0m), Day),
            Lot.Buy(alpha, 1m, Eur(500m), Eur(0m), Day),
        };

        foreach (var method in Enum.GetValues<CostBasisMethod>().Where(m => m != CostBasisMethod.SpecificLot))
        {
            var names = CostBasisCalculator.Calculate(lots, method, Currency.EUR).Positions.Select(p => p.Product.Name);
            Assert.Equal(new[] { "Alfa", "Beta" }, names);
        }

        Assert.Equal(new[] { "Alfa", "Beta" }, HoldingsAggregator.Aggregate(lots).Select(h => h.Product.Name));
    }

    [Fact]
    public void Due_spot_dello_stesso_metallo_vengono_rifiutati()
    {
        var costBasis = CostBasisCalculator.Calculate([Lot.Buy(GoldCoin, 1m, Eur(2000m), Eur(0m), Day)], CostBasisMethod.Fifo, Currency.EUR);

        Assert.Throws<ArgumentException>(() =>
            PnLCalculator.Calculate(costBasis, [GoldSpot(2000m, Day), GoldSpot(2100m, Day)], SpotValuation.Instance));
    }

    [Fact]
    public void Uno_spot_per_metallo_e_accettato_anche_se_c_e_l_argento()
    {
        var costBasis = CostBasisCalculator.Calculate([Lot.Buy(GoldCoin, 1m, Eur(2000m), Eur(0m), Day)], CostBasisMethod.Fifo, Currency.EUR);

        var pnl = PnLCalculator.Calculate(costBasis, [GoldSpot(2100m, Day), SilverSpot(25m, Day)], SpotValuation.Instance);

        Assert.Equal(Eur(100m), pnl.UnrealizedPnL);
    }
}