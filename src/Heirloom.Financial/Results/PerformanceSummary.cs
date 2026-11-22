using System.Text.Json.Serialization;
using Heirloom.Financial.Domain;

namespace Heirloom.Financial.Results;

/// <summary>
/// Un metallo in un giorno dello storico.
/// </summary>
/// <param name="Metal">Il metallo.</param>
/// <param name="Value">Il valore posseduto quel giorno, a 2 decimali.</param>
/// <param name="CostBasis">Il costo di carico dei pezzi a costo noto posseduti quel giorno, a 2 decimali.</param>
/// <param name="SpotPerFineGram">Lo spot di quel giorno per grammo di metallo puro, a 4 decimali.</param>
public sealed record MetalHistoryValue(
    [property: JsonConverter(typeof(JsonStringEnumConverter<Metal>))] Metal Metal,
    decimal Value,
    decimal CostBasis,
    decimal SpotPerFineGram);

/// <summary>
/// Un giorno dello storico, pronto per i grafici dell'andamento. Gli importi sono nella valuta del riepilogo.
/// </summary>
/// <param name="Date">Il giorno.</param>
/// <param name="Value">Il valore del portafoglio, a 2 decimali.</param>
/// <param name="InvestedCapital">Il capitale netto investito fino a quel giorno (acquisti − vendite, eredità al valore
/// del giorno d'arrivo), a 2 decimali: è la base del TWR.</param>
/// <param name="Index">L'indice TWR (1 = punto di partenza), a 4 decimali: mostra il rendimento senza l'effetto di acquisti e vendite.</param>
/// <param name="CostBasis">Il costo di carico dei pezzi a costo noto posseduti quel giorno, a 2 decimali (come nella panoramica).</param>
/// <param name="UnrealizedPnL">Il guadagno sulla carta di quel giorno sui pezzi a costo noto, a 2 decimali.</param>
/// <param name="UnrealizedPercent">Lo stesso come frazione del costo, a 4 decimali; assente se il costo è zero.</param>
/// <param name="Metals">Valore, costo e spot di ogni metallo posseduto quel giorno.</param>
public sealed record HistoryPoint(
    DateOnly Date,
    decimal Value,
    decimal InvestedCapital,
    decimal Index,
    decimal CostBasis,
    decimal UnrealizedPnL,
    decimal? UnrealizedPercent,
    IReadOnlyList<MetalHistoryValue> Metals);

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