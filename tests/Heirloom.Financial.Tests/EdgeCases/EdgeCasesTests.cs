using System.Diagnostics;
using Heirloom.Financial.Domain;
using Heirloom.Financial.Performance;
using Heirloom.Financial.Positions;
using Heirloom.Financial.Results;
using Heirloom.Financial.Risk;
using Heirloom.Financial.Valuation;
using static Heirloom.Financial.Tests.TestData.OneOunce;

namespace Heirloom.Financial.Tests.EdgeCases;

/// <summary>
/// I test che misurano il tempo: non girano in parallelo con gli altri, così la misura non dipende
/// da quanti test stanno usando la CPU nello stesso momento.
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class PerformanceCollection
{
    /// <summary>Il nome della collezione.</summary>
    public const string Name = "Prestazioni";
}

/// <summary>
/// I casi estremi raccolti durante lo sviluppo: tutti devono dare un risultato sensato o un errore chiaro.
/// </summary>
[Collection(PerformanceCollection.Name)]
public class EdgeCaseTests
{
    private static readonly DateOnly Day = new(2024, 3, 1);

    private static CostBasisResult Fifo(params Lot[] lots) => CostBasisCalculator.Calculate(lots, CostBasisMethod.Fifo, Currency.EUR);

    [Fact]
    public void Frazioni_di_pezzo_sono_ammesse()
    {
        var result = Fifo(
            Lot.Buy(GoldCoin, 0.5m, Eur(1000m), Eur(0m), Day),
            Lot.Sell(GoldCoin, 0.25m, Eur(1100m), Eur(0m), Day.AddDays(1)));

        var position = Assert.Single(result.Positions);
        Assert.Equal(0.25m, position.Quantity);
        Assert.Equal(Eur(250m), position.CostBasis);
        Assert.Equal(Eur(25m), result.RealizedPnL);
    }

    [Theory]
    [InlineData(CostBasisMethod.WeightedAverage)]
    [InlineData(CostBasisMethod.Fifo)]
    public void Vendere_tutto_e_ricomprare_riparte_dal_nuovo_costo(CostBasisMethod method)
    {
        var lots = new[]
        {
            Lot.Buy(GoldCoin, 2m, Eur(1500m), Eur(0m), Day),
            Lot.Sell(GoldCoin, 2m, Eur(2000m), Eur(0m), Day.AddDays(10)),
            Lot.Buy(GoldCoin, 1m, Eur(2500m), Eur(0m), Day.AddDays(20)),
        };

        var result = CostBasisCalculator.Calculate(lots, method, Currency.EUR);

        Assert.Equal(Eur(2500m), Assert.Single(result.Positions).AverageCost);
        Assert.Equal(Eur(1000m), result.RealizedPnL);
    }

    [Fact]
    public void Acquisto_e_vendita_nello_stesso_giorno_l_acquisto_viene_prima()
    {
        var sale = Lot.Sell(GoldCoin, 1m, Eur(2100m), Eur(0m), Day);
        var buy = Lot.Buy(GoldCoin, 1m, Eur(2000m), Eur(0m), Day);

        var result = Fifo(sale, buy);

        Assert.Empty(result.Positions);
        Assert.Equal(Eur(100m), result.RealizedPnL);
    }

    [Fact]
    public void Importi_enormi_non_vanno_in_overflow()
    {
        var bar = new MetalProduct("Lingotto 400 oz", Metal.Gold, ProductForm.Bar, Weight.FromTroyOunces(400m), 1m);
        var lot = Lot.Buy(bar, 10_000m, Eur(1_000_000m), Eur(0m), Day);

        var pnl = PnLCalculator.Calculate(Fifo(lot), [GoldSpot(1_000_000m, Day)], SpotValuation.Instance);

        Assert.Equal(Eur(4_000_000_000_000m), pnl.TotalValue);
        Assert.Equal(Eur(3_990_000_000_000m), pnl.UnrealizedPnL);
    }

    [Fact]
    public void Pesi_minuscoli_restano_positivi()
    {
        var flake = new MetalProduct("Scaglia", Metal.Gold, ProductForm.Bar, new Weight(0.0001m), 0.9999m);

        Assert.True(flake.FineWeight.Grams > 0m);
        Assert.Equal(0.0001m, Weight.FromGrams(0.0001m).Grams);
    }

    [Fact]
    public void Un_acquisto_a_prezzo_zero_noto_non_da_percentuale()
    {
        var gift = Lot.Buy(GoldCoin, 1m, Eur(0m), Eur(0m), Day);

        var pnl = PnLCalculator.Calculate(Fifo(gift), [GoldSpot(2000m, Day)], SpotValuation.Instance);

        Assert.Equal(Eur(2000m), pnl.UnrealizedPnL);
        Assert.Null(pnl.UnrealizedPercent);
    }

    [Fact]
    public void Commissioni_maggiori_dell_incasso_danno_una_perdita()
    {
        var result = Fifo(
            Lot.Buy(GoldCoin, 1m, Eur(10m), Eur(0m), Day),
            Lot.Sell(GoldCoin, 1m, Eur(5m), Eur(20m), Day.AddDays(1)));

        var sale = Assert.Single(result.Sales);
        Assert.Equal(Eur(-15m), sale.Proceeds);
        Assert.Equal(Eur(-25m), sale.RealizedPnL);
    }

    [Fact]
    public void Il_29_febbraio_e_un_giorno_come_gli_altri()
    {
        var leap = new DateOnly(2024, 2, 29);
        var lot = Lot.Buy(GoldCoin, 1m, Eur(2000m), Eur(0m), leap);

        var snapshots = SnapshotBuilder.Build([lot], [GoldSpot(2000m, leap)], SpotValuation.Instance, Currency.EUR,
            new DateOnly(2024, 2, 28), new DateOnly(2024, 3, 1));

        Assert.Equal(3, snapshots.Count);
        Assert.Equal(Eur(0m), snapshots[0].Value);
        Assert.Equal(Eur(2000m), snapshots[1].Value);
    }

    [Fact]
    public void Nomi_con_accenti_e_simboli_sono_conservati()
    {
        var product = new MetalProduct("  Marengo «Napoléon III» – 20 F  ", Metal.Gold, ProductForm.Coin, new Weight(6.45m), 0.9m);

        Assert.Equal("Marengo «Napoléon III» – 20 F", product.Name);
    }

    [Fact]
    public void Prodotti_uguali_creati_due_volte_finiscono_nella_stessa_posizione()
    {
        var first = MetalProduct.FromFineWeight("Moneta oro 1 oz", Metal.Gold, ProductForm.Coin, new Weight(33.93m), Weight.FromTroyOunces(1m));

        var result = Fifo(
            Lot.Buy(first, 1m, Eur(2000m), Eur(0m), Day),
            Lot.Buy(GoldCoin, 1m, Eur(2100m), Eur(0m), Day));

        Assert.Equal(2m, Assert.Single(result.Positions).Quantity);
    }

    [Fact]
    public void Due_anni_di_storico_con_mille_lotti_in_meno_di_due_secondi()
    {
        var start = new DateOnly(2023, 1, 1);
        var lots = Enumerable.Range(0, 1000)
            .Select(i => Lot.Buy(GoldCoin, 1m, Eur(1800m + i), Eur(1m), start.AddDays(i % 700)) with { Id = $"L{i}" })
            .ToList();
        var spots = Enumerable.Range(0, 731).Select(d => GoldSpot(1800m + d, start.AddDays(d))).ToList();

        var options = new WalletAnalysisOptions
        {
            Currency = Currency.EUR,
            ValuationDate = start.AddDays(730),
            Valuation = SpotValuation.Instance,
            CostBasisMethod = CostBasisMethod.SpecificLot,
            AnnualizationBasis = AnnualizationBasis.CalendarDays365,
        };

        // Un primo giro a vuoto: la compilazione JIT non deve finire nella misura.
        WalletAnalyzer.Analyze(lots.Take(10), spots, options);

        var watch = Stopwatch.StartNew();
        var summary = WalletAnalyzer.Analyze(lots, spots, options);
        watch.Stop();

        Assert.Equal(1000m, Assert.Single(summary.Positions).Quantity);
        Assert.True(watch.Elapsed < TimeSpan.FromSeconds(2), $"Ci ha messo {watch.Elapsed}.");
    }

    [Fact]
    public void Xirr_senza_flussi_positivi_non_lancia_eccezioni()
    {
        var result = Xirr.Calculate([new CashFlow(Day, -100m), new CashFlow(Day.AddDays(10), 0m)]);

        Assert.False(result.Success);
        Assert.NotNull(result.FailureReason);
    }

    [Fact]
    public void Volatilita_con_due_soli_valori_e_assente()
    {
        Assert.Null(VolatilityCalculator.Annualized(new[] { 100m, 110m }, AnnualizationBasis.TradingDays252));
    }
}