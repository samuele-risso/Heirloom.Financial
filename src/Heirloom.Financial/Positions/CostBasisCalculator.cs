using Heirloom.Financial.Domain;
using Heirloom.Financial.Validation;

namespace Heirloom.Financial.Positions;

/// <summary>
/// Calcola il costo di carico delle posizioni e il costo dei pezzi venduti,
/// con media ponderata, FIFO o lotto specifico.
/// </summary>
/// <remarks>
/// <para>
/// Il costo di un acquisto è il suo importo netto: prezzo × quantità <b>più</b> i costi accessori
/// (commissioni, spedizione, IVA).
/// </para>
/// <para>
/// I pezzi a costo sconosciuto (<c>Lot.BuyWithUnknownCost</c>) restano nelle quantità ma non nel costo.
/// Quando escono con una vendita, la loro parte di incasso non produce guadagno realizzato.
/// Con la media ponderata, una vendita prende pezzi noti e sconosciuti in proporzione a quanti se ne possiedono.
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
    /// <exception cref="ArgumentException">
    /// Un lotto è in un'altra valuta, o si vende più di quanto si possiede. Con <see cref="CostBasisMethod.SpecificLot"/>:
    /// un acquisto senza id o con id ripetuto, una vendita senza lotto collegato, o un lotto collegato
    /// che non esiste, è di un altro prodotto o non ha abbastanza pezzi.
    /// </exception>
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

        if (method == CostBasisMethod.SpecificLot)
        {
            ValidateSpecificLots(list);
        }

        // Per ogni prodotto, i "pacchetti" di pezzi ancora posseduti con il loro costo.
        // Media ponderata: al massimo un pacchetto a costo noto e uno a costo sconosciuto.
        // FIFO e lotto specifico: un pacchetto per acquisto, in ordine di data.
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
                continue;
            }

            var taken = new Taken(Money.Zero(currency), 0m, 0m, Money.Zero(currency));
            taken = method switch
            {
                CostBasisMethod.WeightedAverage => RemoveWeightedAverage(parcels, lot.Quantity, taken),
                CostBasisMethod.Fifo => RemoveFifo(parcels, lot.Quantity, taken),
                _ => RemoveSpecific(parcels, lot, taken),
            };

            sales.Add(new SaleResult(lot, taken.Cost, taken.Unknown)
            {
                DocumentedCostQuantity = taken.Documented,
                DocumentedCostOfSold = taken.Documented > 0m ? taken.DocumentedCost : null,
            });
        }

        var positions = books
            .Select(b => new Position(
                b.Key,
                b.Value.Sum(p => p.Quantity),
                b.Value.Where(p => p.KnownCost).Aggregate(Money.Zero(currency), (sum, p) => sum + p.Cost),
                b.Value.Where(p => !p.KnownCost).Sum(p => p.Quantity)))
            .Where(p => p.Quantity > 0m)
            .OrderBy(p => p.Product.Metal)
            .ThenBy(p => p.Product.Form)
            .ThenBy(p => p.Product.Name, StringComparer.Ordinal)
            .ToList();

        // Con FIFO e lotto specifico ogni pacchetto è un acquisto: sono i lotti aperti.
        var openLots = books.Values
            .SelectMany(parcels => parcels)
            .Where(p => p.Source is not null && p.Quantity > 0m)
            .Select(p => new OpenLot(p.Source!, p.Quantity, p.KnownCost ? p.Cost : null))
            .OrderBy(o => o.Purchase.Date)
            .ThenBy(o => o.Product.Name, StringComparer.Ordinal)
            .ToList();

        return new CostBasisResult(method, currency, positions, sales) { OpenLots = openLots };
    }

    private static void ValidateSpecificLots(IReadOnlyList<Lot> lots)
    {
        var ids = new HashSet<string>(StringComparer.Ordinal);

        foreach (var buy in lots.Where(l => l.Side == LotSide.Buy))
        {
            if (buy.Id is null)
            {
                throw new ArgumentException(
                    $"Con il metodo per lotto specifico ogni acquisto deve avere un Id: manca a '{buy.Product.Name}' del {buy.Date:yyyy-MM-dd}.",
                    nameof(lots));
            }

            if (!ids.Add(buy.Id))
            {
                throw new ArgumentException($"L'Id '{buy.Id}' è usato da più di un acquisto.", nameof(lots));
            }
        }

        var sale = lots.FirstOrDefault(l => l.Side == LotSide.Sell && l.MatchedLotId is null);
        if (sale is not null)
        {
            throw new ArgumentException(
                $"Con il metodo per lotto specifico ogni vendita deve indicare il lotto d'origine (MatchedLotId): " +
                $"manca alla vendita di '{sale.Product.Name}' del {sale.Date:yyyy-MM-dd}.",
                nameof(lots));
        }
    }

    private static void AddPurchase(LinkedList<Parcel> parcels, Lot lot, CostBasisMethod method)
    {
        // Con la media ponderata i pacchetti si fondono: non hanno un acquisto d'origine.
        var source = method == CostBasisMethod.WeightedAverage ? null : lot;
        var parcel = new Parcel(lot.Quantity, lot.NetAmount, lot.HasKnownCost, lot.Id, source);

        if (method == CostBasisMethod.WeightedAverage && Find(parcels, p => p.KnownCost == lot.HasKnownCost) is { } existing)
        {
            // Media ponderata: si fonde nel pacchetto dello stesso tipo (noto o sconosciuto).
            existing.Value = existing.Value with
            {
                Quantity = existing.Value.Quantity + parcel.Quantity,
                Cost = existing.Value.Cost + parcel.Cost,
            };
            return;
        }

        parcels.AddLast(parcel);
    }

    private static Taken RemoveFifo(LinkedList<Parcel> parcels, decimal quantity, Taken taken)
    {
        var remaining = quantity;

        // Si consumano i pacchetti dal più vecchio.
        while (remaining > 0m)
        {
            var node = parcels.First
                ?? throw new InvalidOperationException("Vendita superiore al posseduto: dovrebbe essere già stata rifiutata.");

            var part = Math.Min(remaining, node.Value.Quantity);
            taken = Take(parcels, node, part, taken);
            remaining -= part;
        }

        return taken;
    }

    private static Taken RemoveWeightedAverage(LinkedList<Parcel> parcels, decimal quantity, Taken taken)
    {
        var known = Find(parcels, p => p.KnownCost);
        var unknownNode = Find(parcels, p => !p.KnownCost);

        var knownQuantity = known?.Value.Quantity ?? 0m;
        var unknownQuantity = unknownNode?.Value.Quantity ?? 0m;

        // I pezzi a costo sconosciuto escono in proporzione a quanti se ne possiedono.
        var unknownOut = unknownQuantity == 0m ? 0m
            : knownQuantity == 0m ? quantity
            : Math.Min(unknownQuantity, quantity * unknownQuantity / (knownQuantity + unknownQuantity));
        var knownOut = quantity - unknownOut;

        if (knownOut > knownQuantity)
        {
            // Solo per arrotondamenti all'ultima cifra: si riequilibra.
            knownOut = knownQuantity;
            unknownOut = quantity - knownOut;
        }

        if (known is not null && knownOut > 0m)
        {
            taken = Take(parcels, known, knownOut, taken);
        }

        if (unknownNode is not null && unknownOut > 0m)
        {
            taken = Take(parcels, unknownNode, unknownOut, taken);
        }

        return taken;
    }

    private static Taken RemoveSpecific(LinkedList<Parcel> parcels, Lot sale, Taken taken)
    {
        var node = Find(parcels, p => string.Equals(p.LotId, sale.MatchedLotId, StringComparison.Ordinal))
            ?? throw new ArgumentException(
                $"La vendita di '{sale.Product.Name}' del {sale.Date:yyyy-MM-dd} indica il lotto '{sale.MatchedLotId}', " +
                "che non esiste, è di un altro prodotto, è successivo alla vendita o è già stato venduto del tutto.",
                "lots");

        if (sale.Quantity > node.Value.Quantity)
        {
            throw new ArgumentException(
                $"La vendita di {sale.Quantity} pezzi di '{sale.Product.Name}' del {sale.Date:yyyy-MM-dd} esce dal lotto " +
                $"'{sale.MatchedLotId}', che in quella data ne ha solo {node.Value.Quantity}.",
                "lots");
        }

        return Take(parcels, node, sale.Quantity, taken);
    }

    /// <summary>Toglie dei pezzi da un pacchetto e ne somma il costo (o la quantità, se il costo è sconosciuto).</summary>
    private static Taken Take(LinkedList<Parcel> parcels, LinkedListNode<Parcel> node, decimal quantity, Taken taken)
    {
        var parcel = node.Value;

        // Con la media ponderata il pacchetto non ha un acquisto d'origine: il documento non si conosce.
        var documented = parcel.KnownCost && parcel.Source is { HasDocumentedCost: true };

        // Il pacchetto esce tutto: si prende il suo costo esatto, senza divisioni.
        // Esce solo una parte: costo proporzionale ai pezzi venduti.
        var whole = quantity >= parcel.Quantity;
        var partCost = whole ? parcel.Cost : parcel.Cost / parcel.Quantity * quantity;

        if (!parcel.KnownCost)
        {
            taken = taken with { Unknown = taken.Unknown + quantity };
        }
        else
        {
            taken = taken with { Cost = taken.Cost + partCost };
        }

        if (documented)
        {
            taken = taken with { Documented = taken.Documented + quantity, DocumentedCost = taken.DocumentedCost + partCost };
        }

        if (whole)
        {
            parcels.Remove(node);
        }
        else
        {
            node.Value = parcel with { Quantity = parcel.Quantity - quantity, Cost = parcel.Cost - partCost };
        }

        return taken;
    }

    private static LinkedListNode<Parcel>? Find(LinkedList<Parcel> parcels, Func<Parcel, bool> match)
    {
        for (var node = parcels.First; node is not null; node = node.Next)
        {
            if (match(node.Value))
            {
                return node;
            }
        }

        return null;
    }

    /// <summary>Un gruppo di pezzi dello stesso prodotto con il loro costo totale.</summary>
    /// <param name="Quantity">I pezzi.</param>
    /// <param name="Cost">Il costo totale (zero se sconosciuto).</param>
    /// <param name="KnownCost">Se il costo è noto.</param>
    /// <param name="LotId">L'id dell'acquisto d'origine; usato dal metodo per lotto specifico.</param>
    /// <param name="Source">L'acquisto d'origine; null con la media ponderata, dove i pacchetti si fondono.</param>
    private readonly record struct Parcel(decimal Quantity, Money Cost, bool KnownCost, string? LotId, Lot? Source);

    /// <summary>Quello che esce dai pacchetti per una vendita.</summary>
    /// <param name="Cost">Il costo dei pezzi a costo noto.</param>
    /// <param name="Unknown">I pezzi a costo sconosciuto.</param>
    /// <param name="Documented">I pezzi venuti da acquisti con il costo documentato.</param>
    /// <param name="DocumentedCost">Il costo di quei pezzi.</param>
    private readonly record struct Taken(Money Cost, decimal Unknown, decimal Documented, Money DocumentedCost);
}