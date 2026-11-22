using Heirloom.Financial.Domain;
using Heirloom.Financial.Positions;
using Heirloom.Financial.Risk;
using Heirloom.Financial.Valuation;

namespace Heirloom.Financial.Results;

/// <summary>
/// Le scelte per <see cref="WalletAnalyzer"/>. Le scelte che cambiano i numeri sono obbligatorie:
/// la libreria non decide al posto di chi la usa.
/// </summary>
public sealed record WalletAnalysisOptions
{
    /// <summary>La valuta del riepilogo.</summary>
    public required Currency Currency { get; init; }

    /// <summary>Il giorno della valutazione; nessun lotto può essere successivo.</summary>
    public required DateOnly ValuationDate { get; init; }

    /// <summary>Come valutare le posizioni (spot, spot più premio, riacquisto).</summary>
    public required IValuationStrategy Valuation { get; init; }

    /// <summary>Il metodo per il costo di carico.</summary>
    public required CostBasisMethod CostBasisMethod { get; init; }

    /// <summary>La convenzione sui giorni per annualizzare la volatilità.</summary>
    public required AnnualizationBasis AnnualizationBasis { get; init; }

    /// <summary>Il tasso privo di rischio annuo (frazione) per lo Sharpe; senza, lo Sharpe è assente.</summary>
    public decimal? RiskFreeRate { get; init; }

    /// <summary>Il primo giorno dello storico; se assente, il giorno del primo lotto.</summary>
    public DateOnly? HistoryFrom { get; init; }

    /// <summary>Se includere i punti giornalieri nel riepilogo (per i grafici). Predefinito: sì.</summary>
    public bool IncludeHistory { get; init; } = true;
}