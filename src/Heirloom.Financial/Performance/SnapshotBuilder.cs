using Heirloom.Financial.Domain;
using Heirloom.Financial.Exchange;
using Heirloom.Financial.Internal;
using Heirloom.Financial.Validation;
using Heirloom.Financial.Valuation;

namespace Heirloom.Financial.Performance;

/// <summary>
/// Ricostruisce giorno per giorno la storia del portafoglio a partire da operazioni, prezzi e cambi.
/// </summary>
/// <remarks>
/// <para>
/// Serve a riempire lo storico quando mancano snapshot, per esempio quando l'utente inserisce oggi
/// un acquisto fatto un anno fa.
/// </para>
/// <para>
/// Regola fondamentale: <b>per ogni giorno si usano solo i dati disponibili quel giorno</b>
/// (lotti con data fino a quel giorno, ultimo spot e ultimo cambio noti). Mai un dato "dal futuro".
/// </para>
/// <para>
/// Elabora i giorni in sequenza aggiornando le quantità man mano, quindi un anno di dati con
/// decine di lotti si ricostruisce in pochi millisecondi.
/// </para>
/// </remarks>
public static class SnapshotBuilder
{
    /// <summary>
    /// Ricostruisce gli snapshot giornalieri tra due date, estremi inclusi.
    /// </summary>
    /// <param name="lots">Le operazioni, tutte nella valuta richiesta.</param>
    /// <param name="spots">I prezzi spot storici, di uno o più metalli, in qualsiasi ordine.</param>
    /// <param name="strategy">Come valutare le posizioni.</param>
    /// <param name="currency">La valuta degli snapshot.</param>
    /// <param name="from">Il primo giorno.</param>
    /// <param name="to">L'ultimo giorno; non precedente a <paramref name="from"/>.</param>
    /// <param name="rates">I cambi, se gli spot sono in una valuta diversa da <paramref name="currency"/>.</param>
    /// <returns>Uno snapshot per ogni giorno del periodo.</returns>
    /// <exception cref="ArgumentException">Lotti in un'altra valuta, date invertite, o vendite superiori al posseduto.</exception>
    /// <exception cref="InvalidOperationException">In un giorno con metallo posseduto (o nel giorno di arrivo di un lotto a costo sconosciuto) manca uno spot precedente, o manca il cambio.</exception>
    public static IReadOnlyList<WalletSnapshot> Build(
        IEnumerable<Lot> lots,
        IEnumerable<SpotPrice> spots,
        IValuationStrategy strategy,
        Currency currency,
        DateOnly from,
        DateOnly to,
        FxRateSeries? rates = null)
    {
        ArgumentNullException.ThrowIfNull(spots);
        ArgumentNullException.ThrowIfNull(strategy);
        DomainGuards.Defined(currency);

        if (to < from)
        {
            throw new ArgumentException($"La data finale {to:yyyy-MM-dd} precede quella iniziale {from:yyyy-MM-dd}.", nameof(to));
        }

        var orderedLots = DomainGuards.NoOversell(lots)
            .OrderBy(l => l.Date)
            .ThenBy(l => l.Side == LotSide.Buy ? 0 : 1)
            .ToList();

        if (orderedLots.FirstOrDefault(l => l.Currency != currency) is { } wrong)
        {
            throw new ArgumentException(
                $"Tutti i lotti devono essere in {currency.ToIsoCode()}: '{wrong.Product.Name}' del {wrong.Date:yyyy-MM-dd} non lo è.",
                nameof(lots));
        }

        var spotLookup = new SpotLookup(spots);

        var holdings = new Dictionary<MetalProduct, decimal>();
        var invested = Money.Zero(currency);
        var nextLot = 0;
        var snapshots = new List<WalletSnapshot>(to.DayNumber - from.DayNumber + 1);

        for (var day = from; day <= to; day = day.AddDays(1))
        {
            // Aggiunge le operazioni avvenute fino a questo giorno (comprese quelle prima di "from").
            while (nextLot < orderedLots.Count && orderedLots[nextLot].Date <= day)
            {
                var lot = orderedLots[nextLot++];
                holdings.TryGetValue(lot.Product, out var quantity);
                holdings[lot.Product] = lot.Side == LotSide.Buy ? quantity + lot.Quantity : quantity - lot.Quantity;
                invested += lot.HasKnownCost
                    ? -lot.CashFlow
                    : spotLookup.UnitValue(lot.Product, lot.Date, strategy, currency, rates) * lot.Quantity;
            }

            var value = Money.Zero(currency);
            var gold = Weight.Zero;
            var silver = Weight.Zero;

            foreach (var (product, quantity) in holdings)
            {
                if (quantity == 0m)
                {
                    continue;
                }

                var unitValue = spotLookup.UnitValue(product, day, strategy, currency, rates);

                value += unitValue * quantity;

                var fine = product.FineWeight * quantity;
                if (product.Metal == Metal.Gold)
                {
                    gold += fine;
                }
                else
                {
                    silver += fine;
                }
            }

            snapshots.Add(new WalletSnapshot(day, value, invested, gold, silver));
        }

        return snapshots;
    }
}