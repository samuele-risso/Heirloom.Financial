using Heirloom.Financial.Domain;

namespace Heirloom.Financial.Exchange;

/// <summary>
/// Converte importi da una valuta all'altra con un cambio esplicito.
/// </summary>
/// <remarks>
/// <para>
/// È l'<b>unico</b> punto della libreria in cui si decide se moltiplicare o dividere per il cambio.
/// Con la convenzione di <see cref="FxRate"/> (1 Base = Rate Quote): da Base a Quote si moltiplica,
/// da Quote a Base si divide.
/// </para>
/// <para>
/// Il cambio non viene cercato: lo riceve chi chiama, già scelto per la data giusta
/// (di solito tramite <see cref="FxRateSeries"/>).
/// </para>
/// </remarks>
public static class CurrencyConverter
{
    /// <summary>
    /// Converte un importo nella valuta richiesta.
    /// </summary>
    /// <param name="amount">L'importo da convertire.</param>
    /// <param name="target">La valuta di arrivo.</param>
    /// <param name="rate">Il cambio da usare; deve riguardare le due valute coinvolte.</param>
    /// <returns>L'importo nella valuta di arrivo, senza arrotondamenti. Se la valuta è già quella richiesta,
    /// l'importo invariato.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="rate"/> è null.</exception>
    /// <exception cref="ArgumentException">Il cambio non riguarda le valute dell'importo e di arrivo.</exception>
    public static Money Convert(Money amount, Currency target, FxRate rate)
    {
        ArgumentNullException.ThrowIfNull(rate);

        if (amount.Currency == target)
        {
            return amount;
        }

        if (!rate.Involves(amount.Currency, target))
        {
            throw new ArgumentException(
                $"Il cambio {rate} non permette di convertire da {amount.Currency.ToIsoCode()} a {target.ToIsoCode()}.",
                nameof(rate));
        }

        return amount.Currency == rate.Base
            ? new Money(amount.Amount * rate.Rate, rate.Quote)
            : new Money(amount.Amount / rate.Rate, rate.Base);
    }
}