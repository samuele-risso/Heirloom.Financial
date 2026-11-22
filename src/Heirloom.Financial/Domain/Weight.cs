using Heirloom.Financial.Constants;

namespace Heirloom.Financial.Domain;

/// <summary>
/// Un peso, salvato sempre in grammi, con la conversione in once troy.
/// </summary>
/// <remarks>
/// È un value object immutabile: una volta creato non cambia, e due pesi con gli
/// stessi grammi sono uguali. Non può essere negativo: ogni operazione passa dal
/// costruttore, quindi anche una sottrazione o una moltiplicazione che porterebbe
/// sotto zero viene rifiutata.
/// </remarks>
public readonly record struct Weight : IComparable<Weight>
{
    /// <summary>
    /// Grammi contenuti in un'oncia troy, l'unità con cui sono quotati oro e argento.
    /// </summary>
    /// <remarks>
    /// Scorciatoia per <see cref="UnitConstants.GramsPerTroyOunce"/>, dove il valore è definito.
    /// </remarks>
    public const decimal GramsPerTroyOunce = UnitConstants.GramsPerTroyOunce;

    /// <summary>
    /// Il peso zero. Coincide con <c>default(Weight)</c>.
    /// </summary>
    public static Weight Zero => default;

    /// <summary>
    /// Il peso in grammi. È sempre maggiore o uguale a zero.
    /// </summary>
    public decimal Grams { get; }

    /// <summary>
    /// Il peso in once troy.
    /// </summary>
    public decimal TroyOunces => Grams / GramsPerTroyOunce;

    /// <summary>
    /// Crea un peso a partire dai grammi.
    /// </summary>
    /// <param name="grams">Il peso in grammi; non può essere negativo.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="grams"/> è negativo.</exception>
    public Weight(decimal grams)
    {
        if (grams < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(grams), grams, "Il peso non può essere negativo.");
        }

        Grams = grams;
    }

    /// <summary>
    /// Crea un peso a partire dai grammi.
    /// </summary>
    /// <param name="grams">Il peso in grammi; non può essere negativo.</param>
    /// <returns>Il peso corrispondente.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="grams"/> è negativo.</exception>
    public static Weight FromGrams(decimal grams) => new(grams);

    /// <summary>
    /// Crea un peso a partire dai chilogrammi.
    /// </summary>
    /// <param name="kilograms">Il peso in chilogrammi; non può essere negativo.</param>
    /// <returns>Il peso corrispondente, salvato in grammi.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="kilograms"/> è negativo.</exception>
    public static Weight FromKilograms(decimal kilograms) => new(kilograms * UnitConstants.GramsPerKilogram);

    /// <summary>
    /// Crea un peso a partire dalle once troy.
    /// </summary>
    /// <param name="troyOunces">Il peso in once troy; non può essere negativo.</param>
    /// <returns>Il peso corrispondente, salvato in grammi.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="troyOunces"/> è negativo.</exception>
    public static Weight FromTroyOunces(decimal troyOunces) => new(troyOunces * GramsPerTroyOunce);

    /// <summary>Somma due pesi.</summary>
    /// <param name="left">Il primo peso.</param>
    /// <param name="right">Il secondo peso.</param>
    /// <returns>La somma dei due pesi.</returns>
    public static Weight operator +(Weight left, Weight right) => new(left.Grams + right.Grams);

    /// <summary>Sottrae un peso da un altro.</summary>
    /// <param name="left">Il peso da cui sottrarre.</param>
    /// <param name="right">Il peso da sottrarre.</param>
    /// <returns>La differenza dei due pesi.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Il risultato sarebbe negativo.</exception>
    public static Weight operator -(Weight left, Weight right) => new(left.Grams - right.Grams);

    /// <summary>Moltiplica un peso per un numero (es. peso unitario per quantità, o peso lordo per titolo).</summary>
    /// <param name="weight">Il peso.</param>
    /// <param name="factor">Il fattore; non può essere negativo.</param>
    /// <returns>Il peso moltiplicato.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="factor"/> è negativo.</exception>
    public static Weight operator *(Weight weight, decimal factor) => new(weight.Grams * factor);

    /// <summary>Moltiplica un numero per un peso.</summary>
    /// <param name="factor">Il fattore; non può essere negativo.</param>
    /// <param name="weight">Il peso.</param>
    /// <returns>Il peso moltiplicato.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="factor"/> è negativo.</exception>
    public static Weight operator *(decimal factor, Weight weight) => weight * factor;

    /// <summary>Divide un peso per un numero.</summary>
    /// <param name="weight">Il peso.</param>
    /// <param name="divisor">Il divisore; deve essere positivo.</param>
    /// <returns>Il peso diviso.</returns>
    /// <exception cref="DivideByZeroException"><paramref name="divisor"/> è zero.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="divisor"/> è negativo.</exception>
    public static Weight operator /(Weight weight, decimal divisor) => new(weight.Grams / divisor);

    /// <summary>Indica se il primo peso è minore del secondo.</summary>
    /// <param name="left">Il primo peso.</param>
    /// <param name="right">Il secondo peso.</param>
    /// <returns><c>true</c> se <paramref name="left"/> è minore di <paramref name="right"/>.</returns>
    public static bool operator <(Weight left, Weight right) => left.Grams < right.Grams;

    /// <summary>Indica se il primo peso è maggiore del secondo.</summary>
    /// <param name="left">Il primo peso.</param>
    /// <param name="right">Il secondo peso.</param>
    /// <returns><c>true</c> se <paramref name="left"/> è maggiore di <paramref name="right"/>.</returns>
    public static bool operator >(Weight left, Weight right) => left.Grams > right.Grams;

    /// <summary>Indica se il primo peso è minore o uguale al secondo.</summary>
    /// <param name="left">Il primo peso.</param>
    /// <param name="right">Il secondo peso.</param>
    /// <returns><c>true</c> se <paramref name="left"/> è minore o uguale a <paramref name="right"/>.</returns>
    public static bool operator <=(Weight left, Weight right) => left.Grams <= right.Grams;

    /// <summary>Indica se il primo peso è maggiore o uguale al secondo.</summary>
    /// <param name="left">Il primo peso.</param>
    /// <param name="right">Il secondo peso.</param>
    /// <returns><c>true</c> se <paramref name="left"/> è maggiore o uguale a <paramref name="right"/>.</returns>
    public static bool operator >=(Weight left, Weight right) => left.Grams >= right.Grams;

    /// <summary>
    /// Confronta questo peso con un altro. Serve per ordinare i pesi.
    /// </summary>
    /// <param name="other">Il peso con cui confrontare.</param>
    /// <returns>Un numero negativo, zero o positivo se questo peso è minore, uguale o maggiore di <paramref name="other"/>.</returns>
    public int CompareTo(Weight other) => Grams.CompareTo(other.Grams);

    /// <summary>
    /// Restituisce il peso in grammi in forma leggibile (es. "100 g").
    /// </summary>
    /// <returns>Il peso seguito dall'unità di misura.</returns>
    public override string ToString() => $"{Grams} g";
}