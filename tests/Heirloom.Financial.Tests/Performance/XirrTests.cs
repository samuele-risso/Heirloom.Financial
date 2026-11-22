using System.Globalization;
using Heirloom.Financial.Domain;
using Heirloom.Financial.Performance;
using Heirloom.Financial.Tests.TestData;

namespace Heirloom.Financial.Tests.Performance;

public class XirrTests
{
    // ---------- Casi di riferimento (TestData/Reference/xirr_cases.csv) ----------

    /// <summary>I nomi dei casi presenti nel file di riferimento.</summary>
    public static TheoryData<string> ReferenceCases()
    {
        var data = new TheoryData<string>();
        foreach (var name in LoadReference().Keys)
        {
            data.Add(name);
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(ReferenceCases))]
    public void Coincide_con_il_risultato_di_riferimento(string caseName)
    {
        var (flows, expected) = LoadReference()[caseName];

        var result = Xirr.Calculate(flows);

        Assert.True(result.Success, result.FailureReason ?? string.Empty);
        Assert.Equal(expected, result.Rate!.Value, 6);
    }

    [Fact]
    public void L_esempio_della_documentazione_di_Excel_da_circa_37_34_per_cento()
    {
        var (flows, _) = LoadReference()["excel_doc"];

        Assert.Equal(0.3734, Math.Round(Xirr.Calculate(flows).Rate!.Value, 4));
    }

    // ---------- Casi non calcolabili ----------

    [Fact]
    public void Flussi_tutti_positivi_non_sono_calcolabili_ma_non_lanciano_eccezioni()
    {
        var flows = new[] { new CashFlow(new DateOnly(2024, 1, 1), 100m), new CashFlow(new DateOnly(2025, 1, 1), 200m) };

        var result = Xirr.Calculate(flows);

        Assert.False(result.Success);
        Assert.NotNull(result.FailureReason);
    }

    [Fact]
    public void Un_solo_flusso_non_e_calcolabile()
    {
        Assert.False(Xirr.Calculate([new CashFlow(new DateOnly(2024, 1, 1), -100m)]).Success);
    }

    [Fact]
    public void Quando_Newton_non_converge_interviene_la_bisezione()
    {
        // Perdita quasi totale: Newton, partendo da +10%, esce dall'intervallo valido.
        var (flows, expected) = LoadReference()["near_total_loss"];

        var result = Xirr.Calculate(flows);

        Assert.True(result.Success);
        Assert.Equal(expected, result.Rate!.Value, 6);
    }

    [Fact]
    public void Iterazioni_e_tolleranza_sono_configurabili()
    {
        var (flows, _) = LoadReference()["excel_doc"];

        var rough = Xirr.Calculate(flows, maxIterations: 1000, tolerance: 1e-3);
        var precise = Xirr.Calculate(flows);

        Assert.True(rough.Success);
        Assert.True(rough.Iterations <= precise.Iterations);
        Assert.Equal(200, Xirr.DefaultMaxIterations);
    }

    // ---------- Dai lotti ai flussi ----------

    [Fact]
    public void I_flussi_si_ricavano_dai_lotti_piu_il_valore_attuale()
    {
        var today = new DateOnly(2026, 11, 20);
        var flows = Xirr.CashFlowsFrom(TestLots.WithPartialSale, new Money(7500m, Currency.EUR), today);

        Assert.Equal(
            new[] { -4000m, -4800m, 2590m, 7500m },
            flows.Select(f => f.Amount));
        Assert.Equal(today, flows[^1].Date);
        Assert.True(Xirr.Calculate(flows).Success);
    }

    [Fact]
    public void Lotti_in_un_altra_valuta_vengono_rifiutati()
    {
        Assert.Throws<ArgumentException>(
            () => Xirr.CashFlowsFrom(TestLots.SingleBuy, new Money(2000m, Currency.USD), new DateOnly(2026, 11, 20)));
    }

    // ---------- Lettura del file di riferimento ----------

    private static Dictionary<string, (List<CashFlow> Flows, double Expected)> LoadReference()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "TestData", "Reference", "xirr_cases.csv");
        var cases = new Dictionary<string, (List<CashFlow>, double)>();

        foreach (var line in File.ReadLines(path).Skip(1).Where(l => !string.IsNullOrWhiteSpace(l)))
        {
            var parts = line.Split(',');
            var name = parts[0];
            var flow = new CashFlow(
                DateOnly.ParseExact(parts[1], "yyyy-MM-dd", CultureInfo.InvariantCulture),
                decimal.Parse(parts[2], CultureInfo.InvariantCulture));
            var expected = double.Parse(parts[3], CultureInfo.InvariantCulture);

            if (!cases.TryGetValue(name, out var entry))
            {
                entry = (new List<CashFlow>(), expected);
                cases[name] = entry;
            }

            entry.Item1.Add(flow);
        }

        return cases;
    }
}