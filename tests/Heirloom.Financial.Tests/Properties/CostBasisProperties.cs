using FsCheck.Xunit;
using Heirloom.Financial.Domain;
using Heirloom.Financial.Positions;
using Heirloom.Financial.Tests.TestData;

namespace Heirloom.Financial.Tests.Properties;

/// <summary>
/// Regole del costo di carico che devono valere per qualsiasi storia di acquisti e vendite valida.
/// Ogni caso è: due acquisti (quantità e prezzi casuali, con costi) e una vendita parziale.
/// I valori casuali sono "ripiegati" in intervalli sensati, così ogni storia generata è valida.
/// </summary>
public class CostBasisProperties
{
    private const decimal Tolerance = 0.000001m;

    private static Money Eur(decimal amount) => new(amount, Currency.EUR);

    private static (List<Lot> Buys, Lot Sale) Story(ushort q1, ushort q2, uint p1, uint p2, ushort fees, ushort sold)
    {
        var qty1 = q1 % 50 + 1;             // 1..50 pezzi
        var qty2 = q2 % 50 + 1;
        var price1 = p1 % 300_000 / 100m + 1m;  // 1..3.000 €
        var price2 = p2 % 300_000 / 100m + 1m;
        var fee = fees % 5_000 / 100m;          // 0..50 €
        var sell = sold % (qty1 + qty2) + 1;    // 1..tutti

        var buys = new List<Lot>
        {
            Lot.Buy(TestProducts.Marengo, qty1, Eur(price1), Eur(fee), new DateOnly(2024, 1, 10)),
            Lot.Buy(TestProducts.Marengo, qty2, Eur(price2), Eur(fee), new DateOnly(2024, 6, 10)),
        };
        var sale = Lot.Sell(TestProducts.Marengo, sell, Eur(price2), Eur(fee), new DateOnly(2025, 1, 10));

        return (buys, sale);
    }

    [Property(MaxTest = 500)]
    public bool Media_ponderata_una_vendita_parziale_non_cambia_il_prezzo_medio(
        ushort q1, ushort q2, uint p1, uint p2, ushort fees, ushort sold)
    {
        var (buys, sale) = Story(q1, q2, p1, p2, fees, sold);

        var before = CostBasisCalculator.Calculate(buys, CostBasisMethod.WeightedAverage, Currency.EUR);
        var after = CostBasisCalculator.Calculate([.. buys, sale], CostBasisMethod.WeightedAverage, Currency.EUR);

        if (after.Positions.Count == 0)
        {
            return true; // venduto tutto: non c'è più un prezzo medio
        }

        var difference = after.Positions[0].AverageCost!.Value - before.Positions[0].AverageCost!.Value;
        return Math.Abs(difference.Amount) < Tolerance;
    }

    [Property(MaxTest = 500)]
    public bool Il_costo_si_conserva_tra_venduto_e_rimasto(
        ushort q1, ushort q2, uint p1, uint p2, ushort fees, ushort sold)
    {
        var (buys, sale) = Story(q1, q2, p1, p2, fees, sold);
        var totalBought = buys.Aggregate(Eur(0m), (sum, l) => sum + l.NetAmount);

        foreach (var method in new[] { CostBasisMethod.WeightedAverage, CostBasisMethod.Fifo })
        {
            var result = CostBasisCalculator.Calculate([.. buys, sale], method, Currency.EUR);
            var accounted = result.Sales[0].CostOfSold + result.TotalCostBasis;

            if (Math.Abs((accounted - totalBought).Amount) >= Tolerance)
            {
                return false;
            }
        }

        return true;
    }

    [Property(MaxTest = 500)]
    public bool Comprare_e_rivendere_tutto_allo_stesso_prezzo_costa_solo_le_commissioni(
        ushort quantity, uint price, ushort fees)
    {
        var qty = quantity % 50 + 1;
        var unit = Eur(price % 300_000 / 100m + 1m);
        var fee = Eur(fees % 5_000 / 100m);

        var lots = new[]
        {
            Lot.Buy(TestProducts.Marengo, qty, unit, fee, new DateOnly(2024, 1, 10)),
            Lot.Sell(TestProducts.Marengo, qty, unit, fee, new DateOnly(2025, 1, 10)),
        };

        return new[] { CostBasisMethod.WeightedAverage, CostBasisMethod.Fifo }
            .All(m => CostBasisCalculator.Calculate(lots, m, Currency.EUR).RealizedPnL == -(fee * 2m));
    }

    [Property(MaxTest = 500)]
    public bool Il_peso_fino_rimasto_e_comprato_meno_venduto(
        ushort q1, ushort q2, uint p1, uint p2, ushort fees, ushort sold)
    {
        var (buys, sale) = Story(q1, q2, p1, p2, fees, sold);

        var result = CostBasisCalculator.Calculate([.. buys, sale], CostBasisMethod.Fifo, Currency.EUR);

        var bought = buys.Aggregate(Weight.Zero, (sum, l) => sum + l.FineWeight);
        var remaining = result.Positions.Aggregate(Weight.Zero, (sum, p) => sum + p.FineWeight);

        return remaining == bought - sale.FineWeight;
    }
}