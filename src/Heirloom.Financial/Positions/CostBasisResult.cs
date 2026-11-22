using Heirloom.Financial.Domain;

namespace Heirloom.Financial.Positions;

/// <summary>
/// Il risultato del calcolo del costo di carico: le posizioni ancora aperte e l'esito di ogni vendita.
/// </summary>
/// <param name="Method">Il metodo usato.</param>
/// <param name="Currency">La valuta di tutti gli importi.</param>
/// <param name="Positions">Le posizioni aperte, ordinate per metallo, forma e nome.</param>
/// <param name="Sales">L'esito di ogni vendita, in ordine cronologico.</param>
public sealed record CostBasisResult(
    CostBasisMethod Method,
    Currency Currency,
    IReadOnlyList<Position> Positions,
    IReadOnlyList<SaleResult> Sales)
{
    /// <summary>
    /// Il costo di carico totale delle posizioni aperte.
    /// </summary>
    public Money TotalCostBasis => Positions.Aggregate(Money.Zero(Currency), (sum, p) => sum + p.CostBasis);

    /// <summary>
    /// Il guadagno realizzato totale, sommando tutte le vendite (solo sui pezzi a costo noto).
    /// </summary>
    public Money RealizedPnL => Sales.Aggregate(Money.Zero(Currency), (sum, s) => sum + s.RealizedPnL);

    /// <summary>
    /// L'incasso delle vendite di pezzi a costo sconosciuto, su cui non si può calcolare un guadagno.
    /// </summary>
    public Money UnknownCostProceeds => Sales.Aggregate(Money.Zero(Currency), (sum, s) => sum + s.UnknownCostProceeds);

    /// <summary>
    /// Gli acquisti di cui restano dei pezzi, in ordine di data. Vuoto con la media ponderata,
    /// dove gli acquisti si fondono in un'unica posizione.
    /// </summary>
    public IReadOnlyList<OpenLot> OpenLots { get; init; } = [];
}