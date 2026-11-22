using Heirloom.Financial.Domain;

namespace Heirloom.Financial.Exchange;

/// <summary>
/// Converte i lotti in un'altra valuta, ognuno con il cambio <b>della sua data</b>.
/// </summary>
/// <remarks>
/// Serve per vedere il portafoglio in un'altra valuta in modo corretto: un acquisto pagato 1.800 €
/// quando il cambio era 1,10 è costato 1.980 $, anche se oggi il cambio è diverso.
/// Convertire il costo con il cambio di oggi darebbe un guadagno in dollari senza senso.
/// </remarks>
public static class LotCurrencyConverter
{
    /// <summary>
    /// Converte un lotto: prezzo unitario e costi, con il cambio del giorno dell'operazione.
    /// </summary>
    /// <param name="lot">Il lotto.</param>
    /// <param name="target">La valuta di arrivo.</param>
    /// <param name="rates">La serie dei cambi tra la valuta del lotto e quella di arrivo.</param>
    /// <returns>Un nuovo lotto, identico ma con gli importi nella valuta di arrivo.</returns>
    /// <exception cref="ArgumentNullException">Un argomento è null.</exception>
    /// <exception cref="InvalidOperationException">Nessun cambio disponibile per la data del lotto.</exception>
    public static Lot Convert(Lot lot, Currency target, FxRateSeries rates)
    {
        ArgumentNullException.ThrowIfNull(lot);
        ArgumentNullException.ThrowIfNull(rates);

        if (lot.Currency == target)
        {
            return lot;
        }

        return new Lot(
            lot.Product,
            lot.Quantity,
            rates.Convert(lot.UnitPrice, target, lot.Date),
            rates.Convert(lot.Fees, target, lot.Date),
            lot.Date,
            lot.Side);
    }

    /// <summary>
    /// Converte un elenco di lotti, ognuno con il cambio della sua data.
    /// </summary>
    /// <param name="lots">I lotti.</param>
    /// <param name="target">La valuta di arrivo.</param>
    /// <param name="rates">La serie dei cambi.</param>
    /// <returns>I lotti convertiti, nello stesso ordine.</returns>
    public static IReadOnlyList<Lot> Convert(IEnumerable<Lot> lots, Currency target, FxRateSeries rates)
    {
        ArgumentNullException.ThrowIfNull(lots);

        return lots.Select(l => Convert(l, target, rates)).ToList();
    }
}