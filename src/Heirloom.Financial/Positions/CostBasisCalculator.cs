using Heirloom.Financial.Domain;
using Heirloom.Financial.Validation;

namespace Heirloom.Financial.Positions;

/// <summary>
/// Calcola il costo di carico delle posizioni e il costo dei pezzi venduti, con media ponderata o FIFO.
/// </summary>
/// <remarks>
/// <para>
/// Il costo di un acquisto è il suo importo netto: prezzo × quantità <b>più</b> i costi accessori
/// (commissioni, spedizione, IVA).
/// </para>
/// <para>
/// Tutti i lotti devono essere nella stessa valuta. Per un portafoglio con acquisti in valute diverse,
/// si convertono prima i lotti, ognuno con il cambio della sua data (<c>LotCurrencyConverter</c>).
/// </para>
/// </remarks>
public static class CostBasisCalculator
{
    /// <summary>
    /// Calcola posizioni aperte e vendite.
    /// </summary>
    /// <param name="lots">Le operazioni, in qualsiasi ordine.</param>
    /// <param name="method">Il metodo per le vendite parziali.</param>
    /// <param name="currency">La valuta di tutti i lotti e dei risultati.</param>
    /// <returns>Le posizioni aperte e l'esito di ogni vendita.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="lots"/> è null.</exception>
    /// <exception cref="ArgumentException">Un lotto è in un'altra valuta, o si vende più di quanto si possiede.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Metodo o valuta non definiti.</exception>
    public static CostBasisResult Calculate(IEnumerable<Lot> lots, CostBasisMethod method, Currency currency)
    {
        DomainGuards.Defined(method);
        DomainGuards.Defined(currency);
        var list = DomainGuards.NoOversell(lots);

        var wrong = list.FirstOrDefault(l => l.Currency != currency);
        if (wrong is not null)
        {
            throw new ArgumentException(
                $"Tutti i lotti devono essere in {currency.ToIsoCode()}: '{wrong.Product.Name}' del {wrong.Date:yyyy-MM-dd} è in {wrong.Currency.ToIsoCode()}.",
                nameof(lots));
        }

        // Per ogni prodotto, i "pacchetti" di pezzi ancora posseduti con il loro costo.
        // Media ponderata: un solo pacchetto che riassume tutto. FIFO: un pacchetto per acquisto, in ordine.
        var books = new Dictionary<MetalProduct, LinkedList<Parcel>>();
        var sales = new List<SaleResult>();

        foreach (var lot in LotOrdering.Chronological(list))
        {
            if (!books.TryGetValue(lot.Product, out var parcels))
            {
                parcels = new LinkedList<Parcel>();
                books[lot.Product] = parcels;
            }

            if (lot.Side == LotSide.Buy)
            {
                AddPurchase(parcels, lot, method);
            }
            else
            {
                sales.Add(new SaleResult(lot, RemoveSale(parcels, lot.Quantity, currency)));
            }
        }

        var positions = books
            .Select(b => new Position(
                b.Key,
                b.Value.Sum(p => p.Quantity),
                b.Value.Aggregate(Money.Zero(currency), (sum, p) => sum + p.Cost)))
            .Where(p => p.Quantity > 0m)
            .OrderBy(p => p.Product.Metal)
            .ThenBy(p => p.Product.Form)
            .ThenBy(p => p.Product.Name, StringComparer.Ordinal)
            .ToList();

        return new CostBasisResult(method, currency, positions, sales);
    }

    private static void AddPurchase(LinkedList<Parcel> parcels, Lot lot, CostBasisMethod method)
    {
        var parcel = new Parcel(lot.Quantity, lot.NetAmount);

        if (method == CostBasisMethod.WeightedAverage && parcels.First is { } existing)
        {
            // Media ponderata: si fonde tutto in un unico pacchetto.
            existing.Value = new Parcel(existing.Value.Quantity + parcel.Quantity, existing.Value.Cost + parcel.Cost);
            return;
        }

        parcels.AddLast(parcel);
    }

    private static Money RemoveSale(LinkedList<Parcel> parcels, decimal quantity, Currency currency)
    {
        var remaining = quantity;
        var costOut = Money.Zero(currency);

        // Si consumano i pacchetti dal primo. Con la media ponderata ce n'è uno solo.
        while (remaining > 0m)
        {
            var node = parcels.First
                ?? throw new InvalidOperationException("Vendita superiore al posseduto: dovrebbe essere già stata rifiutata.");

            var parcel = node.Value;

            if (parcel.Quantity <= remaining)
            {
                // Il pacchetto esce tutto: si prende il suo costo esatto, senza divisioni.
                costOut += parcel.Cost;
                remaining -= parcel.Quantity;
                parcels.RemoveFirst();
            }
            else
            {
                // Esce solo una parte: costo proporzionale ai pezzi venduti.
                var partCost = parcel.Cost / parcel.Quantity * remaining;
                costOut += partCost;
                node.Value = new Parcel(parcel.Quantity - remaining, parcel.Cost - partCost);
                remaining = 0m;
            }
        }

        return costOut;
    }

    /// <summary>Un gruppo di pezzi dello stesso prodotto con il loro costo totale.</summary>
    private readonly record struct Parcel(decimal Quantity, Money Cost);
}