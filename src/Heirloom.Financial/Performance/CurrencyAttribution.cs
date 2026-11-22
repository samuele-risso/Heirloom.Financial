using Heirloom.Financial.Domain;
using Heirloom.Financial.Exchange;

namespace Heirloom.Financial.Performance;

/// <summary>
/// La scomposizione del rendimento in valuta di casa: quanto viene dal metallo e quanto dal cambio.
/// </summary>
/// <param name="AssetReturn">Il rendimento del metallo nella valuta in cui è quotato (di solito USD).</param>
/// <param name="CurrencyEffect">L'effetto del cambio: quanto è cambiato il valore di 1 unità di quella valuta, in valuta di casa.</param>
/// <param name="TotalReturn">Il rendimento in valuta di casa: (1 + metallo) × (1 + cambio) − 1.</param>
public sealed record CurrencyAttributionResult(decimal AssetReturn, decimal CurrencyEffect, decimal TotalReturn);

/// <summary>
/// Spiega all'utente perché "l'oro è salito ma io ho guadagnato poco" (o il contrario):
/// scompone il rendimento in euro in effetto metallo ed effetto cambio.
/// </summary>
/// <remarks>
/// Esempio: l'oro passa da 1.980 $ a 2.200 $ (+11,1%) mentre EUR/USD passa da 1,10 a 1,20.
/// Un dollaro vale meno euro di prima (−8,3%), quindi in euro il guadagno è solo
/// 1,111 × 0,917 − 1 = +1,85%.
/// </remarks>
public static class CurrencyAttribution
{
    /// <summary>
    /// Scompone il rendimento.
    /// </summary>
    /// <param name="assetReturn">Il rendimento del metallo nella sua valuta di quotazione, come frazione.</param>
    /// <param name="assetCurrency">La valuta di quotazione del metallo (es. USD).</param>
    /// <param name="homeCurrency">La valuta di casa dell'utente (es. EUR).</param>
    /// <param name="rateAtStart">Il cambio all'inizio del periodo, tra le due valute.</param>
    /// <param name="rateAtEnd">Il cambio alla fine del periodo, tra le due valute.</param>
    /// <returns>Effetto metallo, effetto cambio e rendimento totale in valuta di casa.</returns>
    /// <exception cref="ArgumentException">Un cambio non riguarda le due valute.</exception>
    public static CurrencyAttributionResult Calculate(
        decimal assetReturn,
        Currency assetCurrency,
        Currency homeCurrency,
        FxRate rateAtStart,
        FxRate rateAtEnd)
    {
        var currencyEffect = 0m;

        if (assetCurrency != homeCurrency)
        {
            // Quanto vale 1 unità della valuta del metallo, in valuta di casa, all'inizio e alla fine.
            var one = new Money(1m, assetCurrency);
            var startValue = CurrencyConverter.Convert(one, homeCurrency, rateAtStart).Amount;
            var endValue = CurrencyConverter.Convert(one, homeCurrency, rateAtEnd).Amount;

            currencyEffect = endValue / startValue - 1m;
        }

        var total = (1m + assetReturn) * (1m + currencyEffect) - 1m;
        return new CurrencyAttributionResult(assetReturn, currencyEffect, total);
    }
}