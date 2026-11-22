using Heirloom.Financial.Domain;
using Heirloom.Financial.Valuation;

namespace Heirloom.Financial.Positions;

/// <summary>
/// Il guadagno (o la perdita) di una singola posizione aperta.
/// </summary>
/// <remarks>
/// Se una parte dei pezzi ha costo sconosciuto, il valore viene diviso in proporzione ai pezzi:
/// il guadagno si calcola solo sulla parte a costo noto, mentre <see cref="Value"/> resta il valore di tutto.
/// </remarks>
/// <param name="Position">La posizione, con il suo costo di carico.</param>
/// <param name="Value">Il valore attuale di tutti i pezzi, secondo la strategia di valutazione scelta.</param>
public sealed record PositionPnL(Position Position, Money Value)
{
    /// <summary>
    /// Il valore dei soli pezzi a costo noto.
    /// </summary>
    public Money KnownCostValue => Position.HasUnknownCost
        ? Value * Position.KnownCostQuantity / Position.Quantity
        : Value;

    /// <summary>
    /// Il valore dei pezzi a costo sconosciuto (eredità, regali).
    /// </summary>
    public Money UnknownCostValue => Value - KnownCostValue;

    /// <summary>
    /// Il guadagno sulla carta dei pezzi a costo noto: il loro valore meno il loro costo.
    /// </summary>
    public Money UnrealizedPnL => KnownCostValue - Position.CostBasis;

    /// <summary>
    /// Il guadagno sulla carta in percentuale (frazione) del costo; assente se il costo è zero o sconosciuto.
    /// </summary>
    public decimal? UnrealizedPercent =>
        Position.CostBasis.IsZero ? null : UnrealizedPnL / Position.CostBasis;
}

/// <summary>
/// Il riepilogo dei guadagni del portafoglio.
/// </summary>
/// <param name="Currency">La valuta di tutti gli importi.</param>
/// <param name="Positions">Il guadagno di ogni posizione aperta.</param>
/// <param name="RealizedPnL">Il guadagno già incassato con le vendite (solo sui pezzi a costo noto).</param>
public sealed record PnLSummary(Currency Currency, IReadOnlyList<PositionPnL> Positions, Money RealizedPnL)
{
    /// <summary>Il costo di carico totale delle posizioni aperte (solo pezzi a costo noto).</summary>
    public Money TotalCostBasis => Positions.Aggregate(Money.Zero(Currency), (sum, p) => sum + p.Position.CostBasis);

    /// <summary>Il valore attuale totale delle posizioni aperte, compresi i pezzi a costo sconosciuto.</summary>
    public Money TotalValue => Positions.Aggregate(Money.Zero(Currency), (sum, p) => sum + p.Value);

    /// <summary>Il valore dei pezzi a costo noto: è la base su cui si calcola il guadagno.</summary>
    public Money KnownCostValue => Positions.Aggregate(Money.Zero(Currency), (sum, p) => sum + p.KnownCostValue);

    /// <summary>Il valore dei pezzi a costo sconosciuto.</summary>
    public Money UnknownCostValue => TotalValue - KnownCostValue;

    /// <summary>Il guadagno sulla carta totale, sui pezzi a costo noto.</summary>
    public Money UnrealizedPnL => KnownCostValue - TotalCostBasis;

    /// <summary>Il guadagno sulla carta in percentuale (frazione) del costo; assente se il costo è zero.</summary>
    public decimal? UnrealizedPercent => TotalCostBasis.IsZero ? null : UnrealizedPnL / TotalCostBasis;

    /// <summary>Il guadagno complessivo: realizzato più sulla carta.</summary>
    public Money TotalPnL => RealizedPnL + UnrealizedPnL;
}

/// <summary>
/// Calcola guadagni realizzati e non realizzati, tenendoli separati come l'utente deve vederli.
/// </summary>
public static class PnLCalculator
{
    /// <summary>
    /// Calcola il riepilogo dei guadagni.
    /// </summary>
    /// <param name="costBasis">Il risultato del calcolo del costo di carico.</param>
    /// <param name="spots">Un prezzo spot per ogni metallo posseduto, nella valuta del costo di carico.</param>
    /// <param name="strategy">Come valutare le posizioni (spot, spot più premio, riacquisto).</param>
    /// <returns>Il riepilogo, con il dettaglio per posizione.</returns>
    /// <exception cref="ArgumentNullException">Un argomento è null.</exception>
    /// <exception cref="ArgumentException">Spot mancanti, ripetuti o in una valuta diversa dal costo di carico.</exception>
    public static PnLSummary Calculate(CostBasisResult costBasis, IEnumerable<SpotPrice> spots, IValuationStrategy strategy)
    {
        ArgumentNullException.ThrowIfNull(costBasis);
        ArgumentNullException.ThrowIfNull(spots);
        ArgumentNullException.ThrowIfNull(strategy);

        var spotList = spots.ToList();

        var wrongCurrency = spotList.FirstOrDefault(s => s.Currency != costBasis.Currency);
        if (wrongCurrency is not null)
        {
            throw new ArgumentException(
                $"Gli spot devono essere in {costBasis.Currency.ToIsoCode()} come il costo di carico: " +
                $"quello di {wrongCurrency.Metal} è in {wrongCurrency.Currency.ToIsoCode()}. Convertilo prima.",
                nameof(spots));
        }

        if (spotList.GroupBy(s => s.Metal).Any(g => g.Count() > 1))
        {
            throw new ArgumentException("C'è più di un prezzo spot per lo stesso metallo.", nameof(spots));
        }

        var byMetal = spotList.ToDictionary(s => s.Metal);

        var positions = costBasis.Positions
            .Select(p =>
            {
                if (!byMetal.TryGetValue(p.Product.Metal, out var spot))
                {
                    throw new ArgumentException($"Manca il prezzo spot per {p.Product.Metal}.", nameof(spots));
                }

                return new PositionPnL(p, strategy.Value(p.Product, p.Quantity, spot));
            })
            .ToList();

        return new PnLSummary(costBasis.Currency, positions, costBasis.RealizedPnL);
    }
}