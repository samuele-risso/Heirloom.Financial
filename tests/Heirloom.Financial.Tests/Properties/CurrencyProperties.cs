using FsCheck.Xunit;
using Heirloom.Financial.Domain;

namespace Heirloom.Financial.Tests.Properties;

public class CurrencyProperties
{
    /// <summary>
    /// Per QUALSIASI stringa, FromIsoCode restituisce una valuta valida oppure
    /// rifiuta con ArgumentException. Non deve mai restituire un valore non definito
    /// né lanciare altri tipi di eccezione.
    /// </summary>
    [Property]
    public bool FromIsoCode_restituisce_una_valuta_valida_oppure_rifiuta(string code)
    {
        try
        {
            var currency = CurrencyExtensions.FromIsoCode(code);
            return Enum.IsDefined(currency);
        }
        catch (ArgumentException)
        {
            return true;
        }
    }
}