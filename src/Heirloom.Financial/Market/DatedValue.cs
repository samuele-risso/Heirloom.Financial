namespace Heirloom.Financial.Market;

/// <summary>
/// Un valore con la sua data: per esempio la chiusura giornaliera di una candela o un cambio.
/// </summary>
/// <param name="Date">Il giorno.</param>
/// <param name="Value">Il valore.</param>
public readonly record struct DatedValue(DateOnly Date, decimal Value);

/// <summary>
/// Due serie allineate sulla stessa data.
/// </summary>
/// <param name="Date">Il giorno.</param>
/// <param name="First">Il valore della prima serie quel giorno (o l'ultimo noto).</param>
/// <param name="Second">Il valore della seconda serie quel giorno (o l'ultimo noto).</param>
public readonly record struct AlignedValue(DateOnly Date, decimal First, decimal Second);