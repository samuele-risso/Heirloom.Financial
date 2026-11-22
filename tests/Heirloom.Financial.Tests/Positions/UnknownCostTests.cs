using Heirloom.Financial.Domain;
using Heirloom.Financial.Positions;
using Heirloom.Financial.Valuation;
using static Heirloom.Financial.Tests.TestData.OneOunce;

namespace Heirloom.Financial.Tests.Positions;

/// <summary>
/// Pezzi ereditati o regalati: contano nel valore e nelle quantità, non nel costo né nei guadagni.
/// </summary>
public class UnknownCostTests
{
    private static readonly DateOnly InheritedOn = new(2020, 6, 1);
    private static readonly DateOnly BoughtOn = new(2024, 2, 1);
    private static readonly DateOnly SoldOn = new(2025, 3, 1);

    private static readonly Lot Inherited =
        Lot.BuyWithUnknownCost(GoldCoin, 2m, InheritedOn, Currency.EUR) with { Id = "H" };

    private static readonly Lot Bought =
        Lot.Buy(GoldCoin, 1m, Eur(2000m), Eur(0m), BoughtOn) with { Id = "K" };

    private static Lot Sale(decimal quantity, string? from = null) =>
        Lot.Sell(GoldCoin, quantity, Eur(2100m), Eur(0m), SoldOn) with { MatchedLotId = from };

    private static CostBasisResult Calculate(CostBasisMethod method, params Lot[] lots) =>
        CostBasisCalculator.Calculate(lots, method, Currency.EUR);

    // ---------- Posizioni ----------

    [Theory]
    [InlineData(CostBasisMethod.WeightedAverage)]
    [InlineData(CostBasisMethod.Fifo)]
    [InlineData(CostBasisMethod.SpecificLot)]
    public void La_posizione_conta_tutti_i_pezzi_ma_solo_il_costo_noto(CostBasisMethod method)
    {
        var position = Assert.Single(Calculate(method, Inherited, Bought).Positions);

        Assert.Equal(3m, position.Quantity);
        Assert.Equal(2m, position.UnknownCostQuantity);
        Assert.Equal(1m, position.KnownCostQuantity);
        Assert.True(position.HasUnknownCost);
        Assert.Equal(Eur(2000m), position.CostBasis);
        Assert.Equal(Eur(2000m), position.AverageCost);
    }

    [Fact]
    public void Solo_pezzi_ereditati_niente_prezzo_medio()
    {
        var position = Assert.Single(Calculate(CostBasisMethod.Fifo, Inherited).Positions);

        Assert.Equal(Eur(0m), position.CostBasis);
        Assert.Null(position.AverageCost);
    }

    [Fact]
    public void Una_posizione_tutta_nota_non_ha_costo_sconosciuto()
    {
        var position = Assert.Single(Calculate(CostBasisMethod.Fifo, Bought).Positions);

        Assert.False(position.HasUnknownCost);
        Assert.Equal(0m, position.UnknownCostQuantity);
    }

    [Fact]
    public void Le_quantita_aggregate_comprendono_i_pezzi_ereditati()
    {
        var holding = Assert.Single(HoldingsAggregator.Aggregate([Inherited, Bought]));

        Assert.Equal(3m, holding.Quantity);
    }

    // ---------- Vendite ----------

    [Fact]
    public void Fifo_vendere_pezzi_ereditati_non_produce_guadagno()
    {
        var result = Calculate(CostBasisMethod.Fifo, Inherited, Bought, Sale(2m));

        var sale = Assert.Single(result.Sales);
        Assert.Equal(2m, sale.UnknownCostQuantity);
        Assert.False(sale.HasKnownCost);
        Assert.Equal(Eur(0m), sale.CostOfSold);
        Assert.Equal(Eur(0m), sale.RealizedPnL);
        Assert.Equal(Eur(4200m), sale.Proceeds);
        Assert.Equal(Eur(4200m), sale.UnknownCostProceeds);
        Assert.Equal(Eur(4200m), result.UnknownCostProceeds);

        var position = Assert.Single(result.Positions);
        Assert.False(position.HasUnknownCost);
        Assert.Equal(Eur(2000m), position.CostBasis);
    }

    [Fact]
    public void Fifo_una_vendita_a_cavallo_divide_l_incasso_in_proporzione()
    {
        var result = Calculate(CostBasisMethod.Fifo, Inherited, Bought, Sale(3m));

        var sale = Assert.Single(result.Sales);
        Assert.Equal(2m, sale.UnknownCostQuantity);
        Assert.Equal(1m, sale.KnownCostQuantity);
        Assert.Equal(Eur(2000m), sale.CostOfSold);
        Assert.Equal(Eur(2100m), sale.KnownCostProceeds);
        Assert.Equal(Eur(4200m), sale.UnknownCostProceeds);
        Assert.Equal(Eur(100m), sale.RealizedPnL);
        Assert.Empty(result.Positions);
    }

    [Fact]
    public void Media_ponderata_i_pezzi_escono_in_proporzione()
    {
        var oneEach = Lot.BuyWithUnknownCost(GoldCoin, 1m, InheritedOn, Currency.EUR);

        var result = Calculate(CostBasisMethod.WeightedAverage, oneEach, Bought, Sale(1m));

        var sale = Assert.Single(result.Sales);
        Assert.Equal(0.5m, sale.UnknownCostQuantity);
        Assert.Equal(Eur(1000m), sale.CostOfSold);
        Assert.Equal(Eur(50m), sale.RealizedPnL);

        var position = Assert.Single(result.Positions);
        Assert.Equal(1m, position.Quantity);
        Assert.Equal(0.5m, position.UnknownCostQuantity);
        Assert.Equal(Eur(1000m), position.CostBasis);
        Assert.Equal(Eur(2000m), position.AverageCost);
    }

    [Fact]
    public void Media_ponderata_vendere_tutto_svuota_entrambe_le_parti()
    {
        var result = Calculate(CostBasisMethod.WeightedAverage, Inherited, Bought, Sale(3m));

        var sale = Assert.Single(result.Sales);
        Assert.Equal(2m, sale.UnknownCostQuantity);
        Assert.Equal(Eur(2000m), sale.CostOfSold);
        Assert.Empty(result.Positions);
    }

    [Fact]
    public void Lotto_specifico_vendere_dal_lotto_ereditato()
    {
        var result = Calculate(CostBasisMethod.SpecificLot, Inherited, Bought, Sale(1m, from: "H"));

        var sale = Assert.Single(result.Sales);
        Assert.Equal(1m, sale.UnknownCostQuantity);
        Assert.Equal(Eur(0m), sale.RealizedPnL);

        var position = Assert.Single(result.Positions);
        Assert.Equal(1m, position.UnknownCostQuantity);
        Assert.Equal(Eur(2000m), position.CostBasis);
    }

    [Fact]
    public void Lotto_specifico_vendere_dal_lotto_comprato()
    {
        var result = Calculate(CostBasisMethod.SpecificLot, Inherited, Bought, Sale(1m, from: "K"));

        var sale = Assert.Single(result.Sales);
        Assert.True(sale.HasKnownCost);
        Assert.Equal(Eur(100m), sale.RealizedPnL);
        Assert.Equal(2m, Assert.Single(result.Positions).UnknownCostQuantity);
    }

    // ---------- Guadagni ----------

    [Fact]
    public void Il_guadagno_si_calcola_solo_sui_pezzi_a_costo_noto()
    {
        var costBasis = Calculate(CostBasisMethod.Fifo, Inherited, Bought);

        var pnl = PnLCalculator.Calculate(costBasis, [GoldSpot(2500m, SoldOn)], SpotValuation.Instance);

        var position = Assert.Single(pnl.Positions);
        Assert.Equal(Eur(7500m), position.Value);
        Assert.Equal(Eur(2500m), position.KnownCostValue);
        Assert.Equal(Eur(5000m), position.UnknownCostValue);
        Assert.Equal(Eur(500m), position.UnrealizedPnL);
        Assert.Equal(0.25m, position.UnrealizedPercent);

        Assert.Equal(Eur(7500m), pnl.TotalValue);
        Assert.Equal(Eur(2500m), pnl.KnownCostValue);
        Assert.Equal(Eur(5000m), pnl.UnknownCostValue);
        Assert.Equal(Eur(2000m), pnl.TotalCostBasis);
        Assert.Equal(Eur(500m), pnl.UnrealizedPnL);
        Assert.Equal(0.25m, pnl.UnrealizedPercent);
    }

    [Fact]
    public void Solo_pezzi_ereditati_valore_pieno_e_nessun_guadagno()
    {
        var costBasis = Calculate(CostBasisMethod.Fifo, Inherited);

        var pnl = PnLCalculator.Calculate(costBasis, [GoldSpot(2500m, SoldOn)], SpotValuation.Instance);

        Assert.Equal(Eur(5000m), pnl.TotalValue);
        Assert.Equal(Eur(5000m), pnl.UnknownCostValue);
        Assert.Equal(Eur(0m), pnl.UnrealizedPnL);
        Assert.Null(pnl.UnrealizedPercent);
        Assert.Null(Assert.Single(pnl.Positions).UnrealizedPercent);
    }

    [Fact]
    public void Senza_pezzi_ereditati_i_conti_non_cambiano()
    {
        var costBasis = Calculate(CostBasisMethod.Fifo, Bought);

        var pnl = PnLCalculator.Calculate(costBasis, [GoldSpot(2500m, SoldOn)], SpotValuation.Instance);

        Assert.Equal(pnl.TotalValue, pnl.KnownCostValue);
        Assert.Equal(Eur(0m), pnl.UnknownCostValue);
        Assert.Equal(pnl.TotalValue - pnl.TotalCostBasis, pnl.UnrealizedPnL);
    }
}