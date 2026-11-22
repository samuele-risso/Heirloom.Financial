using Heirloom.Financial.Performance;

namespace Heirloom.Financial.Tests.Performance;

/// <summary>
/// Controlla <b>come</b> l'XIRR arriva al risultato, non solo il risultato: Newton deve convergere
/// in poche iterazioni, e la bisezione deve trovare il tasso giusto quando Newton fallisce.
/// Senza questi test un errore nella formula di Newton passerebbe inosservato, perché la bisezione
/// lo "salverebbe" trovando comunque il tasso.
/// </summary>
public class XirrConvergenceTests
{
    private static readonly DateOnly Start = new(2024, 1, 1);

    [Fact]
    public void Un_caso_normale_converge_con_newton_in_poche_iterazioni()
    {
        var result = Xirr.Calculate([new CashFlow(Start, -1000m), new CashFlow(Start.AddDays(365), 1100m)]);

        Assert.True(result.Success);
        Assert.Equal(0.10, result.Rate!.Value, 10);
        Assert.InRange(result.Iterations, 1, 10);
    }

    [Fact]
    public void Con_piu_flussi_newton_resta_veloce_e_preciso()
    {
        var result = Xirr.Calculate(
        [
            new CashFlow(Start, -1000m),
            new CashFlow(Start.AddDays(100), -500m),
            new CashFlow(Start.AddDays(200), 300m),
            new CashFlow(Start.AddDays(400), 1400m),
        ]);

        Assert.True(result.Success);
        Assert.InRange(result.Iterations, 1, 15);
        Assert.True(Math.Abs(NetPresentValue(result.Rate!.Value)) < 1e-6);

        static double NetPresentValue(double rate) =>
            -1000 - 500 / Math.Pow(1 + rate, 100 / 365.0) + 300 / Math.Pow(1 + rate, 200 / 365.0) + 1400 / Math.Pow(1 + rate, 400 / 365.0);
    }

    [Fact]
    public void Una_perdita_quasi_totale_passa_alla_bisezione_e_trova_il_tasso()
    {
        // Da 0,1 Newton salta sotto −100% e si ferma: deve intervenire la bisezione.
        var result = Xirr.Calculate([new CashFlow(Start, -1000m), new CashFlow(Start.AddDays(365), 1m)]);

        Assert.True(result.Success);
        Assert.Equal(-0.999, result.Rate!.Value, 9);
        // La bisezione dimezza l'intervallo a ogni passo: per arrivare a 1e-12 servono qualche decina di iterazioni,
        // non 10 (sarebbe Newton) e non il massimo di 200 (vorrebbe dire che non si è fermata alla tolleranza).
        Assert.InRange(result.Iterations, 11, 100);
    }

    [Fact]
    public void Un_guadagno_enorme_passa_alla_bisezione_e_trova_il_tasso()
    {
        var result = Xirr.Calculate([new CashFlow(Start, -1m), new CashFlow(Start.AddDays(365), 1000m)]);

        Assert.True(result.Success);
        Assert.Equal(999.0, result.Rate!.Value, 6);
    }

    [Fact]
    public void Solo_flussi_negativi_si_ferma_subito()
    {
        var result = Xirr.Calculate([new CashFlow(Start, -100m), new CashFlow(Start.AddDays(30), -50m)]);

        Assert.False(result.Success);
        Assert.Equal(0, result.Iterations);
    }

    [Fact]
    public void Solo_flussi_positivi_si_ferma_subito()
    {
        var result = Xirr.Calculate([new CashFlow(Start, 100m), new CashFlow(Start.AddDays(30), 50m)]);

        Assert.False(result.Success);
        Assert.Equal(0, result.Iterations);
    }

    [Fact]
    public void Un_solo_flusso_si_ferma_subito()
    {
        var result = Xirr.Calculate([new CashFlow(Start, -100m)]);

        Assert.False(result.Success);
        Assert.Equal(0, result.Iterations);
    }

    [Fact]
    public void Un_flusso_a_zero_non_conta_come_positivo()
    {
        var result = Xirr.Calculate([new CashFlow(Start, -100m), new CashFlow(Start.AddDays(30), 0m)]);

        Assert.False(result.Success);
        Assert.Equal(0, result.Iterations);
    }

    [Fact]
    public void L_ordine_dei_flussi_non_cambia_il_risultato()
    {
        var ordered = Xirr.Calculate([new CashFlow(Start, -1000m), new CashFlow(Start.AddDays(365), 1100m)]);
        var reversed = Xirr.Calculate([new CashFlow(Start.AddDays(365), 1100m), new CashFlow(Start, -1000m)]);

        Assert.Equal(ordered.Rate!.Value, reversed.Rate!.Value, 10);
    }
}