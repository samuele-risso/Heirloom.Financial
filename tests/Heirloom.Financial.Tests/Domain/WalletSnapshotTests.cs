using Heirloom.Financial.Domain;

namespace Heirloom.Financial.Tests.Domain;

public class PortfolioSnapshotTests
{
    private static readonly DateOnly Day = new(2026, 11, 20);
    private static Money Eur(decimal amount) => new(amount, Currency.EUR);

    [Fact]
    public void La_fotografia_conserva_i_dati_ricevuti()
    {
        var s = new WalletSnapshot(Day, Eur(12000m), Eur(10000m), new Weight(150m), new Weight(1000m));

        Assert.Equal(Day, s.Date);
        Assert.Equal(Eur(12000m), s.Value);
        Assert.Equal(Eur(10000m), s.InvestedCapital);
        Assert.Equal(new Weight(150m), s.GoldFineWeight);
        Assert.Equal(new Weight(1000m), s.SilverFineWeight);
        Assert.Equal(Currency.EUR, s.Currency);
    }

    [Fact]
    public void Il_guadagno_sulla_carta_e_valore_meno_capitale_investito()
    {
        var gain = new WalletSnapshot(Day, Eur(12000m), Eur(10000m), Weight.Zero, Weight.Zero);
        var loss = new WalletSnapshot(Day, Eur(9000m), Eur(10000m), Weight.Zero, Weight.Zero);

        Assert.Equal(Eur(2000m), gain.UnrealizedPnL);
        Assert.Equal(Eur(-1000m), loss.UnrealizedPnL);
    }

    [Fact]
    public void Un_portafoglio_vuoto_ha_valore_zero()
    {
        var s = new WalletSnapshot(Day, Money.Zero(Currency.EUR), Money.Zero(Currency.EUR), Weight.Zero, Weight.Zero);

        Assert.True(s.Value.IsZero);
    }

    [Fact]
    public void Il_capitale_investito_puo_essere_negativo()
    {
        // L'utente ha già incassato con le vendite più di quanto aveva investito.
        var s = new WalletSnapshot(Day, Eur(3000m), Eur(-500m), new Weight(31.1m), Weight.Zero);

        Assert.Equal(Eur(3500m), s.UnrealizedPnL);
    }

    [Fact]
    public void Valore_e_capitale_in_valute_diverse_vengono_rifiutati()
    {
        var ex = Assert.Throws<ArgumentException>(
            () => new WalletSnapshot(Day, Eur(12000m), new Money(10000m, Currency.USD), Weight.Zero, Weight.Zero));

        Assert.Equal("investedCapital", ex.ParamName);
        Assert.Contains("EUR", ex.Message);
        Assert.Contains("USD", ex.Message);
    }

    [Fact]
    public void Un_valore_negativo_viene_rifiutato()
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(
            () => new WalletSnapshot(Day, Eur(-1m), Eur(0m), Weight.Zero, Weight.Zero));

        Assert.Equal("value", ex.ParamName);
    }
}