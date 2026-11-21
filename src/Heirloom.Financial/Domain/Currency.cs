using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Heirloom.Financial.Domain
{
    /// <summary>
    /// 
    /// </summary>
    public enum Currency
    {
        /// <summary>
        /// 
        /// </summary>
        EUR = 1,
    
        /// <summary>
        /// 
        /// </summary>
        USD = 2,
    }

    /// <summary>
    /// 
    /// </summary>
    public static class CurrencyExtensions
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="currency"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public static string ToIsoCode(this Currency currency) => currency switch
        {
            Currency.EUR => "EUR",

            Currency.USD => "USD",

            _ => throw new ArgumentOutOfRangeException(nameof(currency), currency, "Currency not supported."),
        };

        /// <summary>
        /// Converte un codice ISO 4217 nella valuta corrispondente.
        /// Ignora maiuscole/minuscole e spazi iniziali o finali.
        /// </summary>
        /// <param name="code">Il codice ISO 4217 (es. "EUR", "eur").</param>
        /// <returns>La valuta corrispondente al codice.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="code"/> è null.</exception>
        /// <exception cref="ArgumentException">Il codice non corrisponde a nessuna valuta supportata.</exception>
        public static Currency FromIsoCode(string code)
        {
            ArgumentNullException.ThrowIfNull(code);

            return code.Trim().ToUpperInvariant() switch
            {
                "EUR" => Currency.EUR,
                "USD" => Currency.USD,
                _ => throw new ArgumentException($"Currency code unknown: '{code}'.", nameof(code)),
            };
        }
    }
}
