using Heirloom.Financial.Domain;
using Heirloom.Financial.Validation;

namespace Heirloom.Financial.Taxation;

/// <summary>
/// La regola italiana sulle plusvalenze da cessione di oro (e, come applica Heirloom, degli altri metalli).
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item>Costo documentato (fattura): l'aliquota si applica alla plusvalenza, cioè valore − costo, solo se positiva.</item>
/// <item>Costo non documentato o sconosciuto, vendite dal 1° gennaio 2024: l'aliquota si applica all'intero
/// valore di vendita (Legge di Bilancio 2024, L. 213/2023).</item>
/// <item>Costo non documentato o sconosciuto, vendite fino al 31 dicembre 2023: l'aliquota si applicava al 25%
/// del valore di vendita (art. 68, c. 7, lett. d del TUIR nella versione precedente).</item>
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

    /// <summary>Il primo giorno in cui, senza documento, si paga sull'intero valore di vendita.</summary>
    public static readonly DateOnly FullValueRuleFrom = new(2024, 1, 1);

    /// <summary>La quota del valore tassata senza documento prima del 2024: 25%.</summary>
    public const decimal UndocumentedBaseBefore2024 = 0.25m;

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
    /// <remarks>Stima per una vendita fatta oggi: vale la regola in vigore dal 2024.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">Il valore è negativo.</exception>
    /// <exception cref="ArgumentException">Costo e valore sono in valute diverse.</exception>
    public Money EstimateTax(Money value, Money? cost, bool hasDocumentedCost) =>
        EstimateTax(value, cost, hasDocumentedCost, FullValueRuleFrom);

    /// <inheritdoc />
    /// <exception cref="ArgumentOutOfRangeException">Il valore è negativo.</exception>
    /// <exception cref="ArgumentException">Costo e valore sono in valute diverse.</exception>
    public Money EstimateTax(Money value, Money? cost, bool hasDocumentedCost, DateOnly saleDate)
    {
        DomainGuards.NonNegative(value);

        if (hasDocumentedCost && cost is { } documented)
        {
            DomainGuards.SameCurrency(value, documented);
            var gain = value - documented;
            return gain.IsNegative ? Money.Zero(value.Currency) : gain * Rate;
        }

        return saleDate < FullValueRuleFrom
            ? value * UndocumentedBaseBefore2024 * Rate
            : value * Rate;
    }
}