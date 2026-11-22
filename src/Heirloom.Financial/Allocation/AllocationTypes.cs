using Heirloom.Financial.Domain;

namespace Heirloom.Financial.Allocation;

/// <summary>
/// Come raggruppare il portafoglio in fette.
/// </summary>
public enum AllocationGrouping
{
    /// <summary>Per metallo: oro, argento.</summary>
    Metal = 1,

    /// <summary>Per forma: monete, lingotti.</summary>
    Form = 2,

    /// <summary>Per metallo e forma: oro monete, oro lingotti, argento monete, argento lingotti.</summary>
    MetalAndForm = 3,
}

/// <summary>
/// Su cosa calcolare le percentuali delle fette.
/// </summary>
public enum AllocationBasis
{
    /// <summary>Sul valore: quanta parte dei soldi è in ogni fetta.</summary>
    Value = 1,

    /// <summary>Sul peso fino: quanta parte del metallo è in ogni fetta.</summary>
    FineWeight = 2,
}

/// <summary>
/// Una fetta del portafoglio, pronta per uno spicchio del grafico a ciambella.
/// </summary>
/// <param name="Metal">Il metallo della fetta; assente se si raggruppa solo per forma.</param>
/// <param name="Form">La forma della fetta; assente se si raggruppa solo per metallo.</param>
/// <param name="Value">Il valore della fetta.</param>
/// <param name="FineWeight">Il metallo puro contenuto nella fetta.</param>
/// <param name="Percent">La quota della fetta sul totale, come frazione (0,25 = 25%);
/// assente se il portafoglio è vuoto.</param>
public sealed record AllocationSlice(
    Metal? Metal,
    ProductForm? Form,
    Money Value,
    Weight FineWeight,
    decimal? Percent);