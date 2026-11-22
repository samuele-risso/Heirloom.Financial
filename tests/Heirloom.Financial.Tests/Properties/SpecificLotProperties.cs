using FsCheck.Xunit;
using Heirloom.Financial.Domain;
using Heirloom.Financial.Positions;
using static Heirloom.Financial.Tests.TestData.OneOunce;

namespace Heirloom.Financial.Tests.Properties;

/// <summary>
/// Regole del lotto specifico e dei pezzi a costo sconosciuto, per storie casuali ma valide.
/// </summary>
public class SpecificLotProperties
{
    private const decimal Tolerance = 0.000001m;

    private static (Lot First, Lot Second, decimal Sold) Story(ushort q1, ushort q2, uint p1, uint p2, ushort sold)
    {
        var qty1 = q1 % 50 + 1;
        var qty2 = q2 % 50 + 1;
        var price1 = p1 % 300_000 / 100m + 1m;
        var price2 = p2 % 300_000 / 100m + 1m;

        var first = Lot.Buy(GoldCoin, qty1, Eur(price1), Eur(1m), new DateOnly(2024, 1, 10)) with { Id = "first" };
        var second = Lot.Buy(GoldCoin, qty2, Eur(price2), Eur(1m), new DateOnly(2024, 6, 10)) with { Id = "second" };

        return (first, second, sold % qty1 + 1);
    }

    private static Lot Sale(decimal quantity, string? from) =>
        Lot.Sell(GoldCoin, quantity, Eur(1500m), Eur(2m), new DateOnly(2025, 1, 10)) with { MatchedLotId = from };

    [Property(MaxTest = 500)]
    public bool Vendere_dal_lotto_piu_vecchio_equivale_a_fifo(ushort q1, ushort q2, uint p1, uint p2, ushort sold)
    {
        var (first, second, quantity) = Story(q1, q2, p1, p2, sold);
        var lots = new[] { first, second, Sale(quantity, "first") };

        var specific = CostBasisCalculator.Calculate(lots, CostBasisMethod.SpecificLot, Currency.EUR);
        var fifo = CostBasisCalculator.Calculate(lots, CostBasisMethod.Fifo, Currency.EUR);

        return Math.Abs((specific.RealizedPnL - fifo.RealizedPnL).Amount) < Tolerance
            && Math.Abs((specific.TotalCostBasis - fifo.TotalCostBasis).Amount) < Tolerance;
    }

    [Property(MaxTest = 500)]
    public bool Il_lotto_specifico_conserva_il_costo(ushort q1, ushort q2, uint p1, uint p2, ushort sold, bool fromSecond)
    {
        var (first, second, quantity) = Story(q1, q2, p1, p2, sold);
        var from = fromSecond ? second : first;
        quantity = Math.Min(quantity, from.Quantity);

        var result = CostBasisCalculator.Calculate([first, second, Sale(quantity, from.Id)], CostBasisMethod.SpecificLot, Currency.EUR);

        var bought = first.NetAmount + second.NetAmount;
        var accounted = result.TotalCostBasis + result.Sales[0].CostOfSold;
        return Math.Abs((bought - accounted).Amount) < Tolerance;
    }

    [Property(MaxTest = 500)]
    public bool I_pezzi_a_costo_sconosciuto_si_conservano(ushort q1, ushort q2, uint p1, uint p2, ushort sold, bool weighted)
    {
        var (first, second, _) = Story(q1, q2, p1, p2, sold);
        var inherited = Lot.BuyWithUnknownCost(GoldCoin, first.Quantity, first.Date, Currency.EUR);
        var total = inherited.Quantity + second.Quantity;
        var quantity = sold % total + 1;
        var method = weighted ? CostBasisMethod.WeightedAverage : CostBasisMethod.Fifo;

        var result = CostBasisCalculator.Calculate([inherited, second, Sale(quantity, null)], method, Currency.EUR);

        var unknownLeft = result.Positions.Sum(p => p.UnknownCostQuantity);
        var unknownSold = result.Sales[0].UnknownCostQuantity;
        var knownCost = result.TotalCostBasis + result.Sales[0].CostOfSold;

        return Math.Abs(unknownLeft + unknownSold - inherited.Quantity) < Tolerance
            && Math.Abs((knownCost - second.NetAmount).Amount) < Tolerance
            && result.Sales[0].UnknownCostQuantity >= 0m
            && result.Sales[0].UnknownCostQuantity <= quantity;
    }

    [Property(MaxTest = 500)]
    public bool Media_ponderata_con_eredita_il_prezzo_medio_non_cambia(ushort q1, ushort q2, uint p1, uint p2, ushort sold)
    {
        var (first, second, _) = Story(q1, q2, p1, p2, sold);
        var inherited = Lot.BuyWithUnknownCost(GoldCoin, first.Quantity, first.Date, Currency.EUR);
        var quantity = sold % (inherited.Quantity + second.Quantity) + 1;

        var before = CostBasisCalculator.Calculate([inherited, second], CostBasisMethod.WeightedAverage, Currency.EUR);
        var after = CostBasisCalculator.Calculate([inherited, second, Sale(quantity, null)], CostBasisMethod.WeightedAverage, Currency.EUR);

        if (after.Positions.Count == 0 || after.Positions[0].AverageCost is null)
        {
            return true;
        }

        var difference = after.Positions[0].AverageCost!.Value - before.Positions[0].AverageCost!.Value;
        return Math.Abs(difference.Amount) < Tolerance;
    }
}