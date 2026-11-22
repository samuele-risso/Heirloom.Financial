using Heirloom.Financial.Domain;
using Heirloom.Financial.Exchange;
using Heirloom.Financial.Valuation;

namespace Heirloom.Financial.Internal;

/// <summary>
/// Trova lo spot di un metallo valido in un giorno (l'ultimo disponibile fino a quel giorno)
/// e valuta un prodotto in una valuta, convertendo se serve.
/// </summary>
/// <remarks>
/// Usato da chi ricostruisce lo storico (<c>SnapshotBuilder</c>) e da chi deve dare un valore
/// di mercato ai lotti a costo sconosciuto (<c>Xirr</c>). Mai dati "dal futuro".
/// </remarks>
internal sealed class SpotLookup
{
    private readonly Dictionary<Metal, SpotPrice[]> _byMetal;

    /// <summary>Prepara la ricerca; gli spot possono arrivare in qualsiasi ordine.</summary>
    /// <param name="spots">I prezzi spot storici, di uno o più metalli.</param>
    public SpotLookup(IEnumerable<SpotPrice> spots)
    {
        ArgumentNullException.ThrowIfNull(spots);

        _byMetal = spots
            .GroupBy(s => s.Metal)
            .ToDictionary(g => g.Key, g => g.OrderBy(s => s.Date).ToArray());
    }

    /// <summary>L'ultimo spot del metallo disponibile in quel giorno (ricerca binaria).</summary>
    /// <exception cref="InvalidOperationException">Nessuno spot del metallo fino a quel giorno.</exception>
    public SpotPrice On(Metal metal, DateOnly day)
    {
        if (_byMetal.TryGetValue(metal, out var series))
        {
            int low = 0, high = series.Length - 1, found = -1;

            while (low <= high)
            {
                var middle = (low + high) / 2;

                if (series[middle].Date <= day)
                {
                    found = middle;
                    low = middle + 1;
                }
                else
                {
                    high = middle - 1;
                }
            }

            if (found >= 0)
            {
                return series[found];
            }
        }

        throw new InvalidOperationException($"Nessun prezzo spot di {metal} disponibile per il {day:yyyy-MM-dd}.");
    }

    /// <summary>Il valore di un pezzo del prodotto in quel giorno, nella valuta richiesta.</summary>
    /// <exception cref="InvalidOperationException">Manca lo spot, oppure serve un cambio e non c'è.</exception>
    public Money UnitValue(MetalProduct product, DateOnly day, IValuationStrategy strategy, Currency currency, FxRateSeries? rates)
    {
        var unitValue = strategy.UnitValue(product, On(product.Metal, day));

        if (unitValue.Currency == currency)
        {
            return unitValue;
        }

        if (rates is null)
        {
            throw new InvalidOperationException(
                $"Lo spot di {product.Metal} è in {unitValue.Currency.ToIsoCode()}: serve una serie di cambi verso {currency.ToIsoCode()}.");
        }

        return rates.Convert(unitValue, currency, day);
    }
}