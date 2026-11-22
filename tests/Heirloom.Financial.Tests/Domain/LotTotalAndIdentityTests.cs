using Heirloom.Financial.Domain;
using Heirloom.Financial.Exchange;
using Heirloom.Financial.Tests.TestData;

namespace Heirloom.Financial.Tests.Domain;

public class LotTotalAndIdentityTests
{
    private static readonly DateOnly Day = new(2024, 3, 15);
    private static Money Eur(decimal amount) => new(amount, Currency.EUR);
    private static Money Usd(decimal amount) => new(amount, Currency.USD);

    // ---------- Prezzo totale ----------

    [Fact]
    public void Acquisto_dal_totale_conserva_esattamente_il_totale_pagato()
    {
        var lot = Lot.BuyForTotal(TestProducts.Krugerrand1Oz, 3m, Eur(2000m), Eur(15m), Day);

        Assert.Equal(Eur(2000m), lot.GrossAmount);
        Assert.Equal(Eur(2015m), lot.NetAmount);
        Assert.Equal(Eur(-2015m), lot.CashFlow);
        Assert.Equal(LotSide.Buy, lot.Side);
        Assert.True(lot.HasKnownCost);
    }

    [Fact]
    public void Acquisto_dal_totale_ricava_il_prezzo_per_pezzo()
    {
        var lot = Lot.BuyForTotal(TestProducts.Krugerrand1Oz, 3m, Eur(2000m), Eur(0m), Day);

        Assert.True(Math.Abs(lot.UnitPrice.Amount - 666.6666666667m) < 0.000001m);
    }

    [Fact]
    public void Acquisto_dal_totale_e_acquisto_per_pezzo_coincidono_quando_la_divisione_e_esatta()
    {
        var fromTotal = Lot.BuyForTotal(TestProducts.Sovereign, 4m, Eur(2000m), Eur(10m), Day);
        var fromUnit = Lot.Buy(TestProducts.Sovereign, 4m, Eur(500m), Eur(10m), Day);

        Assert.Equal(fromUnit.UnitPrice, fromTotal.UnitPrice);
        Assert.Equal(fromUnit.GrossAmount, fromTotal.GrossAmount);
        Assert.Equal(fromUnit.NetAmount, fromTotal.NetAmount);
    }

    [Fact]
    public void Vendita_dal_totale_toglie_i_costi_dall_incasso()
    {
        var lot = Lot.SellForTotal(TestProducts.Krugerrand1Oz, 2m, Eur(4200m), Eur(20m), Day);

        Assert.Equal(Eur(4200m), lot.GrossAmount);
        Assert.Equal(Eur(4180m), lot.NetAmount);
        Assert.Equal(Eur(4180m), lot.CashFlow);
        Assert.Equal(LotSide.Sell, lot.Side);
    }

    [Fact]
    public void Totale_negativo_viene_rifiutato()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Lot.BuyForTotal(TestProducts.Krugerrand1Oz, 1m, Eur(-1m), Eur(0m), Day));
    }

    [Fact]
    public void Totale_e_costi_in_valute_diverse_vengono_rifiutati()
    {
        Assert.Throws<ArgumentException>(() =>
            Lot.BuyForTotal(TestProducts.Krugerrand1Oz, 1m, Eur(100m), Usd(1m), Day));
    }

    [Fact]
    public void Totale_con_quantita_zero_viene_rifiutato()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Lot.BuyForTotal(TestProducts.Krugerrand1Oz, 0m, Eur(100m), Eur(0m), Day));
    }

    // ---------- Costo sconosciuto ----------

    [Fact]
    public void Acquisto_a_costo_sconosciuto_ha_importi_e_flusso_a_zero()
    {
        var lot = Lot.BuyWithUnknownCost(TestProducts.Krugerrand1Oz, 2m, Day, Currency.EUR);

        Assert.False(lot.HasKnownCost);
        Assert.Equal(LotSide.Buy, lot.Side);
        Assert.Equal(Currency.EUR, lot.Currency);
        Assert.Equal(Eur(0m), lot.UnitPrice);
        Assert.Equal(Eur(0m), lot.NetAmount);
        Assert.Equal(Eur(0m), lot.CashFlow);
    }

    [Fact]
    public void Acquisto_a_costo_sconosciuto_conta_nel_peso_fino()
    {
        var lot = Lot.BuyWithUnknownCost(TestProducts.Krugerrand1Oz, 2m, Day, Currency.EUR);

        Assert.Equal(TestProducts.Krugerrand1Oz.FineWeight * 2m, lot.FineWeight);
    }

    [Fact]
    public void Acquisto_a_costo_sconosciuto_con_valuta_non_definita_viene_rifiutato()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Lot.BuyWithUnknownCost(TestProducts.Krugerrand1Oz, 1m, Day, (Currency)0));
    }

    [Fact]
    public void Acquisto_a_costo_sconosciuto_con_quantita_zero_viene_rifiutato()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Lot.BuyWithUnknownCost(TestProducts.Krugerrand1Oz, 0m, Day, Currency.EUR));
    }

    [Fact]
    public void Un_acquisto_normale_ha_costo_noto()
    {
        Assert.True(Lot.Buy(TestProducts.Krugerrand1Oz, 1m, Eur(2000m), Eur(0m), Day).HasKnownCost);
        Assert.True(Lot.Sell(TestProducts.Krugerrand1Oz, 1m, Eur(2000m), Eur(0m), Day).HasKnownCost);
    }

    // ---------- Identificativi ----------

    [Fact]
    public void L_id_viene_ripulito_dagli_spazi()
    {
        var lot = Lot.Buy(TestProducts.Krugerrand1Oz, 1m, Eur(2000m), Eur(0m), Day) with { Id = "  coin-42 " };

        Assert.Equal("coin-42", lot.Id);
    }

    [Fact]
    public void Un_id_vuoto_viene_rifiutato()
    {
        var lot = Lot.Buy(TestProducts.Krugerrand1Oz, 1m, Eur(2000m), Eur(0m), Day);

        var ex = Assert.Throws<ArgumentException>(() => lot with { Id = " " });
        Assert.Equal("Id", ex.ParamName);
    }

    [Fact]
    public void Senza_id_i_campi_sono_null()
    {
        var lot = Lot.Buy(TestProducts.Krugerrand1Oz, 1m, Eur(2000m), Eur(0m), Day);

        Assert.Null(lot.Id);
        Assert.Null(lot.MatchedLotId);
    }

    [Fact]
    public void Una_vendita_puo_indicare_il_lotto_d_origine()
    {
        var sale = Lot.Sell(TestProducts.Krugerrand1Oz, 1m, Eur(2100m), Eur(0m), Day) with { MatchedLotId = "coin-42" };

        Assert.Equal("coin-42", sale.MatchedLotId);
    }

    [Fact]
    public void Un_acquisto_non_puo_indicare_un_lotto_d_origine()
    {
        var buy = Lot.Buy(TestProducts.Krugerrand1Oz, 1m, Eur(2000m), Eur(0m), Day);

        var ex = Assert.Throws<ArgumentException>(() => buy with { MatchedLotId = "coin-1" });
        Assert.Equal("MatchedLotId", ex.ParamName);
    }

    [Fact]
    public void Due_lotti_uguali_con_id_diversi_sono_diversi()
    {
        var a = Lot.Buy(TestProducts.Krugerrand1Oz, 1m, Eur(2000m), Eur(0m), Day) with { Id = "a" };
        var b = a with { Id = "b" };

        Assert.NotEqual(a, b);
    }

    // ---------- Conversione di valuta ----------

    [Fact]
    public void La_conversione_conserva_id_collegamento_e_totale()
    {
        var rates = new FxRateSeries([new FxRate(Currency.EUR, Currency.USD, 1.10m, Day)]);
        var sale = Lot.SellForTotal(TestProducts.Krugerrand1Oz, 3m, Eur(2000m), Eur(10m), Day)
            with
        { Id = "tx-7", MatchedLotId = "coin-42" };

        var converted = LotCurrencyConverter.Convert(sale, Currency.USD, rates);

        Assert.Equal(Usd(2200m), converted.GrossAmount);
        Assert.Equal(Usd(11m), converted.Fees);
        Assert.Equal("tx-7", converted.Id);
        Assert.Equal("coin-42", converted.MatchedLotId);
        Assert.Equal(LotSide.Sell, converted.Side);
    }

    [Fact]
    public void La_conversione_conserva_il_costo_sconosciuto()
    {
        var rates = new FxRateSeries([new FxRate(Currency.EUR, Currency.USD, 1.10m, Day)]);
        var inherited = Lot.BuyWithUnknownCost(TestProducts.Krugerrand1Oz, 2m, Day, Currency.EUR) with { Id = "coin-9" };

        var converted = LotCurrencyConverter.Convert(inherited, Currency.USD, rates);

        Assert.False(converted.HasKnownCost);
        Assert.Equal(Currency.USD, converted.Currency);
        Assert.Equal(Usd(0m), converted.NetAmount);
        Assert.Equal("coin-9", converted.Id);
    }
}