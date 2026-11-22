using System.Text.Json.Serialization;
using Heirloom.Financial.Risk;

namespace Heirloom.Financial.Results;

/// <summary>
/// Il peggior calo del portafoglio.
/// </summary>
/// <param name="Depth">La profondità del calo, come frazione negativa a 4 decimali (−0,25 = −25%).</param>
/// <param name="PeakDate">Il giorno del massimo prima del calo.</param>
/// <param name="TroughDate">Il giorno del minimo.</param>
/// <param name="RecoveryDate">Il giorno in cui si è tornati al massimo; assente se non è ancora successo.</param>
public sealed record DrawdownResult(decimal Depth, DateOnly PeakDate, DateOnly TroughDate, DateOnly? RecoveryDate);

/// <summary>
/// Le misure di rischio, calcolate sulla serie TWR. È assente nel riepilogo quando lo storico è troppo corto.
/// </summary>
/// <param name="AnnualizationBasis">La convenzione sui giorni usata per annualizzare.</param>
/// <param name="Volatility">La volatilità annualizzata, come frazione a 4 decimali.</param>
/// <param name="MaxDrawdown">Il peggior calo; assente se non ci sono stati cali.</param>
/// <param name="Sharpe">L'indice di Sharpe, a 4 decimali; assente senza tasso privo di rischio, con meno di un anno di storico o con volatilità zero.</param>
public sealed record RiskSummary(
    [property: JsonConverter(typeof(JsonStringEnumConverter<AnnualizationBasis>))] AnnualizationBasis AnnualizationBasis,
    decimal Volatility,
    DrawdownResult? MaxDrawdown,
    decimal? Sharpe);