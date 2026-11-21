using FsCheck.Xunit;
using Heirloom.Financial.Domain;

namespace Heirloom.Financial.Tests.Properties;

public class MetalProperties
{
    /// <summary>
    /// Per QUALSIASI stringa, FromIsoCode restituisce un metallo valido oppure
    /// rifiuta con ArgumentException. Non deve mai restituire un valore non definito
    /// né lanciare altri tipi di eccezione.
    /// </summary>
    [Property]
    public bool FromIsoCode_restituisce_un_metallo_valido_oppure_rifiuta(string code)
    {
        try
        {
            var metal = MetalExtensions.FromIsoCode(code);
            return Enum.IsDefined(metal);
        }
        catch (ArgumentException)
        {
            return true;
        }
    }
}