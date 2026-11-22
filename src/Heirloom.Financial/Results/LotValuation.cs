using System.Text.Json.Serialization;
using Heirloom.Financial.Domain;

namespace Heirloom.Financial.Results;

/// <summary>
/// Un acquisto di cui restano dei pezzi, valutato oggi: una riga della tabella dei lotti.
/// </summary>
/// <param name="LotId">L'id del lotto d'acquisto (<c>Lot.Id</c>); null se non era stato assegnato.</param>
/// <param name="Product">Il nome del prodotto.</param>
/// <param name="Metal">Il metallo.</param>
/// <param name="Form">Moneta o lingotto.</param>
/// <param name="PurchaseDate">La data dell'acquisto.</param>
/// <param name="Quantity">I pezzi ancora posseduti.</param>
/// <param name="FineWeightGrams">Il peso fino dei pezzi rimasti, a 4 decimali.</param>
/// <param name="Value">Il valore attuale dei pezzi rimasti.</param>
/// <param name="ValueShare">La quota sul valore totale del portafoglio, come frazione a 4 decimali; null se il totale è zero.</param>
/// <param name="Cost">Il costo dei pezzi rimasti; null se sconosciuto.</param>
/// <param name="UnrealizedPnL">Valore − costo; null se il costo è sconosciuto.</param>
/// <param name="UnrealizedPercent">Lo stesso come frazione del costo, a 4 decimali; null se il costo è sconosciuto o zero.</param>
/// <param name="HasDocumentedCost">Se il costo è documentato (fattura).</param>
/// <param name="EstimatedTax">Le tasse stimate se si vendesse oggi; null se l'analisi non ha una regola fiscale.</param>
public sealed record LotValuation(
    string? LotId,
    string Product,
    [property: JsonConverter(typeof(JsonStringEnumConverter<Metal>))] Metal Metal,
    [property: JsonConverter(typeof(JsonStringEnumConverter<ProductForm>))] ProductForm Form,
    DateOnly PurchaseDate,
    decimal Quantity,
    decimal FineWeightGrams,
    MoneyResult Value,
    decimal? ValueShare,
    MoneyResult? Cost,
    MoneyResult? UnrealizedPnL,
    decimal? UnrealizedPercent,
    bool HasDocumentedCost,
    MoneyResult? EstimatedTax);