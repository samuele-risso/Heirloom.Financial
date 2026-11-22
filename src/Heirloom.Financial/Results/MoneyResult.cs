using Heirloom.Financial.Domain;
using Heirloom.Financial.Internal;

namespace Heirloom.Financial.Results;

/// <summary>
/// Un importo pronto da mostrare: arrotondato a 2 decimali, con la valuta come codice ISO.
/// </summary>
/// <remarks>
/// I calcoli usano <see cref="Money"/>; i risultati usano questo tipo, che in JSON diventa
/// <c>{ "amount": 1234.56, "currency": "EUR" }</c> senza bisogno di convertitori.
/// </remarks>
/// <param name="Amount">L'importo, arrotondato a 2 decimali (per eccesso dal 5).</param>
/// <param name="Currency">Il codice ISO 4217 della valuta (es. "EUR").</param>
public sealed record MoneyResult(decimal Amount, string Currency)
{
    /// <summary>Arrotonda un importo e ne prende la valuta.</summary>
    /// <param name="money">L'importo esatto.</param>
    /// <returns>L'importo arrotondato.</returns>
    public static MoneyResult From(Money money) =>
        new(Rounding.Amount(money.Amount), money.Currency.ToIsoCode());

    /// <summary>Come <see cref="From"/>, ma accetta un importo assente.</summary>
    /// <param name="money">L'importo esatto, o null.</param>
    /// <returns>L'importo arrotondato, o null.</returns>
    public static MoneyResult? FromOptional(Money? money) => money is { } m ? From(m) : null;
}