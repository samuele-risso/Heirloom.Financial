using Heirloom.Financial.Domain;
using Heirloom.Financial.Validation;

namespace Heirloom.Financial.Taxation;

/// <summary>
/// La regola italiana sulle plusvalenze da cessione di oro (e, come applica Heirloom, degli altri metalli).
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item>Costo documentato (fattura): l'aliquota si applica alla plusvalenza, cioè valore − costo, solo se positiva.</item>
/// <item>Costo non documentato o sconosciuto: dal 2024 l'aliquota si applica all'intero valore di vendita
/// (Legge di Bilancio 2024, L. 213/2023, che ha abolito il forfait del 25% del corrispettivo).</item>
/// </list>
/// <para>
/// Il trattamento fiscale dell'argento e degli altri metalli può differire da quello dell'oro da investimento:
/// questa regola li tratta allo stesso modo. È una stima, non una consulenza fiscale.
/// </para>
/// </remarks>
public sealed class ItalianTaxPolicy : ITaxPolicy
{
    /// <summary>L'aliquota ordinaria sui redditi diversi: 26%.</summary>
    public const decimal StandardRate = 0.26m;

    /// <summary>La regola con l'aliquota ordinaria del 26%.</summary>
    public static ItalianTaxPolicy Standard { get; } = new(StandardRate);

    /// <summary>L'aliquota, come frazione (0,26 = 26%).</summary>
    public decimal Rate { get; }

    /// <summary>Crea la regola con un'aliquota.</summary>
    /// <param name="rate">L'aliquota come frazione; da 0 (incluso) a 1 (escluso).</param>
    /// <exception cref="ArgumentOutOfRangeException">L'aliquota è fuori intervallo.</exception>
    public ItalianTaxPolicy(decimal rate)
    {
        if (rate < 0m || rate >= 1m)
        {
            throw new ArgumentOutOfRangeException(nameof(rate), rate, "L'aliquota deve essere tra 0 (incluso) e 1 (escluso).");
        }

        Rate = rate;
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentOutOfRangeException">Il valore è negativo.</exception>
    /// <exception cref="ArgumentException">Costo e valore sono in valute diverse.</exception>
    public Money EstimateTax(Money value, Money? cost, bool hasDocumentedCost)
    {
        DomainGuards.NonNegative(value);

        if (hasDocumentedCost && cost is { } documented)
        {
            DomainGuards.SameCurrency(value, documented);
            var gain = value - documented;
            return gain.IsNegative ? Money.Zero(value.Currency) : gain * Rate;
        }

        return value * Rate;
    }
}