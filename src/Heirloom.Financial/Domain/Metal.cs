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
    public enum Metal
    {
        /// <summary>
        /// 
        /// </summary>
        Gold = 1,

        /// <summary>
        ///
        /// </summary>
        Silver = 2,
    }

    /// <summary>
    /// 
    /// </summary>
    public static class MetalExtensions
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="metal"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public static string ToIsoCode(this Metal metal) => metal switch
        {
            Metal.Gold => "XAU",

            Metal.Silver => "XAG",

            _ => throw new ArgumentOutOfRangeException(nameof(metal), metal, "Metal not supported."),
        };

        /// <summary>
        /// Converte un codice ISO 4217 nel metallo corrispondente.
        /// Ignora maiuscole/minuscole e spazi iniziali o finali.
        /// </summary>
        /// <param name="code">Il codice ISO 4217 (es. "XAU", "xag").</param>
        /// <returns>Il metallo corrispondente al codice.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="code"/> è null.</exception>
        /// <exception cref="ArgumentException">Il codice non corrisponde a nessun metallo supportato.</exception>
        public static Metal FromIsoCode(string code)
        {
            ArgumentNullException.ThrowIfNull(code);

            return code.Trim().ToUpperInvariant() switch
            {
                "XAU" => Metal.Gold,
                "XAG" => Metal.Silver,
                _ => throw new ArgumentException($"Codice metallo sconosciuto: '{code}'.", nameof(code)),
            };
        }
    }


}
