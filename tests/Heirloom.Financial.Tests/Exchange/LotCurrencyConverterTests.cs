using Heirloom.Financial.Domain;
using Heirloom.Financial.Exchange;
using Heirloom.Financial.Tests.TestData;

namespace Heirloom.Financial.Tests.Exchange;

public class LotCurrencyConverterTests
{
    private static readonly FxRateSeries Rates = new(
    [
        new FxRate(Currency.EUR, Currency.USD, 1.10m, new DateOnly(2024, 1, 2)),
        new FxRate(Currency.EUR, Currency.USD, 1.20m, new DateOnly(2025, 1, 2)),
    ]);

    [Fact]
    public void Ogni_lotto_usa_il_cambio_della_sua_data()
    {
        var early = Lot.Buy(TestProducts.Marengo, 1m, new Money(500m, Currency.EUR), new Money(10m, Currency.EUR), new DateOnly(2024, 6, 1));
        var late = Lot.Buy(TestProducts.Marengo, 1m, new Money(500m, Currency.EUR), new Money(10m, Currency.EUR), new DateOnly(2025, 6, 1));

        var converted = LotCurrencyConverter.Convert([early, late], Currency.USD, Rates);

        Assert.Equal(new Money(550m, Currency.USD), converted[0].UnitPrice);
        Assert.Equal(new Money(11m, Currency.USD), converted[0].Fees);
        Assert.Equal(new Money(600m, Currency.USD), converted[1].UnitPrice);
    }

    [Fact]
    public void Quantita_data_prodotto_e_verso_restano_invariati()
    {
        var lot = Lot.Sell(TestProducts.Marengo, 3m, new Money(500m, Currency.EUR), new Money(0m, Currency.EUR), new DateOnly(2024, 6, 1));

        var converted = LotCurrencyConverter.Convert(lot, Currency.USD, Rates);

        Assert.Equal(lot.Product, converted.Product);
        Assert.Equal(lot.Quantity, converted.Quantity);
        Assert.Equal(lot.Date, converted.Date);
        Assert.Equal(lot.Side, converted.Side);
    }

    [Fact]
    public void Un_lotto_gia_nella_valuta_richiesta_non_cambia()
    {
        var lot = TestLots.SingleBuy[0];

        Assert.Same(lot, LotCurrencyConverter.Convert(lot, Currency.EUR, Rates));
    }
}