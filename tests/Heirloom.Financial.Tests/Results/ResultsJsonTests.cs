using System.Text.Json;
using Heirloom.Financial.Domain;
using Heirloom.Financial.Positions;
using Heirloom.Financial.Results;
using Heirloom.Financial.Risk;
using Heirloom.Financial.Valuation;
using static Heirloom.Financial.Tests.TestData.OneOunce;

namespace Heirloom.Financial.Tests.Results;

/// <summary>
/// Il riepilogo come lo vede il frontend: le opzioni JSON "Web" sono quelle predefinite di ASP.NET Core
/// (nomi in camelCase).
/// </summary>
public class ResultsJsonTests
{
    private static readonly DateOnly Start = new(2024, 1, 1);
    private static readonly DateOnly End = new(2025, 1, 1);

    private static WalletSummary Summary() => WalletAnalyzer.Analyze(
        [
            Lot.Buy(GoldCoin, 1m, Eur(2000m), Eur(0m), Start) with { Id = "A" },
            Lot.Buy(SilverBar, 1m, Eur(250m), Eur(0m), Start) with { Id = "S" },
        ],
        [GoldSpot(2000m, Start), GoldSpot(2500m, End), SilverSpot(25m, Start), SilverSpot(30m, End)],
        new WalletAnalysisOptions
        {
            Currency = Currency.EUR,
            ValuationDate = End,
            Valuation = SpotValuation.Instance,
            CostBasisMethod = CostBasisMethod.SpecificLot,
            AnnualizationBasis = AnnualizationBasis.CalendarDays365,
            IncludeHistory = false,
        });

    private static string Json() => JsonSerializer.Serialize(Summary(), JsonSerializerOptions.Web);

    [Fact]
    public void I_nomi_dei_campi_sono_in_camel_case()
    {
        using var document = JsonDocument.Parse(Json());
        var root = document.RootElement;

        foreach (var name in new[]
        {
            "currency", "valuationDate", "costBasisMethod", "totalValue", "knownCostValue", "unknownCostValue",
            "totalCostBasis", "unrealizedPnL", "unrealizedPercent", "realizedPnL", "unknownCostProceeds", "totalPnL",
            "goldFineWeightGrams", "silverFineWeightGrams", "positions", "allocationByMetal", "allocationByForm",
            "performance", "risk", "notes", "estimatedTax", "valueAfterTax", "unrealizedPnLAfterTax",
            "lots", "byMetal", "cashFlow",
        })
        {
            Assert.True(root.TryGetProperty(name, out _), $"Manca il campo '{name}'.");
        }
    }

    [Fact]
    public void Gli_importi_hanno_importo_e_valuta_come_stringa()
    {
        using var document = JsonDocument.Parse(Json());
        var total = document.RootElement.GetProperty("totalValue");

        Assert.Equal(2800m, total.GetProperty("amount").GetDecimal());
        Assert.Equal("EUR", total.GetProperty("currency").GetString());
        Assert.Equal("EUR", document.RootElement.GetProperty("currency").GetString());
    }

    [Fact]
    public void Gli_enum_sono_stringhe()
    {
        using var document = JsonDocument.Parse(Json());
        var root = document.RootElement;
        var position = root.GetProperty("positions")[0];

        Assert.Equal("SpecificLot", root.GetProperty("costBasisMethod").GetString());
        Assert.Equal("Gold", position.GetProperty("metal").GetString());
        Assert.Equal("Coin", position.GetProperty("form").GetString());
        Assert.Equal("Gold", root.GetProperty("allocationByMetal")[0].GetProperty("metal").GetString());
        Assert.Equal(JsonValueKind.Null, root.GetProperty("allocationByMetal")[0].GetProperty("form").ValueKind);
        Assert.Equal("CalendarDays365", root.GetProperty("risk").GetProperty("annualizationBasis").GetString());
    }

    [Fact]
    public void Le_date_sono_nel_formato_iso()
    {
        using var document = JsonDocument.Parse(Json());

        Assert.Equal("2025-01-01", document.RootElement.GetProperty("valuationDate").GetString());
        Assert.Equal("2024-01-01", document.RootElement.GetProperty("performance").GetProperty("from").GetString());
    }

    [Fact]
    public void Il_riepilogo_si_rilegge_uguale()
    {
        var original = Summary();

        var copy = JsonSerializer.Deserialize<WalletSummary>(JsonSerializer.Serialize(original, JsonSerializerOptions.Web), JsonSerializerOptions.Web)!;

        Assert.Equal(original.TotalValue, copy.TotalValue);
        Assert.Equal(original.CostBasisMethod, copy.CostBasisMethod);
        Assert.Equal(original.Positions, copy.Positions);
        Assert.Equal(original.AllocationByMetal, copy.AllocationByMetal);
        Assert.Equal(original.Risk, copy.Risk);
    }
}