using Heirloom.Financial.Domain;

namespace Heirloom.Financial.Taxation;

/// <summary>
/// Una regola fiscale che stima le tasse dovute se si vendessero oggi dei pezzi.
/// </summary>
/// <remarks>
/// Le regole cambiano per legge e da paese a paese: per questo sono separate dal resto della libreria
/// e si scelgono nelle opzioni dell'analisi. Il risultato è una stima per l'utente, non un calcolo fiscale.
/// </remarks>
public interface ITaxPolicy
{
    /// <summary>
    /// Stima le tasse sulla vendita di un gruppo di pezzi acquistati insieme.
    /// </summary>
    /// <param name="value">Il valore di vendita dei pezzi; non negativo.</param>
    /// <param name="cost">Il loro costo d'acquisto; null se sconosciuto. Nella stessa valuta del valore.</param>
    /// <param name="hasDocumentedCost">Se il costo è documentato (es. fattura).</param>
    /// <returns>Le tasse stimate, nella valuta del valore; mai negative.</returns>
    Money EstimateTax(Money value, Money? cost, bool hasDocumentedCost);

    /// <summary>
    /// Stima le tasse su una vendita fatta in un giorno preciso, con la regola in vigore quel giorno.
    /// </summary>
    /// <param name="value">L'incasso della vendita; non negativo.</param>
    /// <param name="cost">Il costo d'acquisto dei pezzi venduti; null se sconosciuto.</param>
    /// <param name="hasDocumentedCost">Se il costo è documentato (es. fattura).</param>
    /// <param name="saleDate">Il giorno della vendita.</param>
    /// <returns>Le tasse stimate, nella valuta del valore; mai negative.</returns>
    /// <remarks>Senza un'implementazione propria vale la regola di oggi, come la versione senza data.</remarks>
    Money EstimateTax(Money value, Money? cost, bool hasDocumentedCost, DateOnly saleDate) =>
        EstimateTax(value, cost, hasDocumentedCost);
}