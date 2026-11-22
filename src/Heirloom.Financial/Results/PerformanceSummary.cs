namespace Heirloom.Financial.Results;

/// <summary>
/// Un giorno dello storico, pronto per il grafico dell'andamento.
/// </summary>
/// <param name="Date">Il giorno.</param>
/// <param name="Value">Il valore del portafoglio, a 2 decimali, nella valuta del riepilogo.</param>
/// <param name="InvestedCapital">Il capitale netto investito fino a quel giorno, a 2 decimali.</param>
/// <param name="Index">L'indice TWR (1 = punto di partenza), a 4 decimali: mostra il rendimento senza l'effetto di acquisti e vendite.</param>
public sealed record HistoryPoint(DateOnly Date, decimal Value, decimal InvestedCapital, decimal Index);

/// <summary>
/// I rendimenti del portafoglio nel periodo analizzato.
/// </summary>
/// <param name="From">Il primo giorno dello storico.</param>
/// <param name="To">L'ultimo giorno (la data di valutazione).</param>
/// <param name="Currency">Il codice ISO della valuta degli importi dello storico.</param>
/// <param name="TimeWeightedReturn">Il rendimento del metallo nel periodo (TWR), come frazione a 4 decimali.</param>
/// <param name="AnnualizedReturn">Il TWR annualizzato; assente se il periodo è più breve di un anno, perché annualizzare pochi mesi gonfia il dato.</param>
/// <param name="MoneyWeightedReturn">Il rendimento personale annuo (XIRR) dall'inizio; assente se non calcolabile.</param>
/// <param name="History">I punti giornalieri; vuoto se non richiesti.</param>
public sealed record PerformanceSummary(
    DateOnly From,
    DateOnly To,
    string Currency,
    decimal? TimeWeightedReturn,
    decimal? AnnualizedReturn,
    decimal? MoneyWeightedReturn,
    IReadOnlyList<HistoryPoint> History);