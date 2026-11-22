using Heirloom.Financial.Validation;

namespace Heirloom.Financial.Domain;

/// <summary>
/// Un prodotto in metallo prezioso, come compare nel catalogo
/// (es. "Krugerrand 1 oz", "Lingotto oro 100 g").
/// </summary>
/// <remarks>
/// <para>
/// Il valore di un prodotto dipende dal suo <b>peso fino</b>, cioè dalla quantità di
/// metallo puro che contiene: peso lordo × titolo. Un Krugerrand pesa 33,93 g, ma con
/// titolo 0,9167 contiene esattamente un'oncia troy d'oro.
/// </para>
/// <para>
/// È un record immutabile: due prodotti con gli stessi dati sono uguali. Il catalogo
/// dei prodotti vive nel database di Heirloom, non in questa libreria: qui arriva già
/// costruito.
/// </para>
/// </remarks>
public sealed record MetalProduct
{
    /// <summary>
    /// Il nome del prodotto (es. "Krugerrand 1 oz").
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Il metallo contenuto.
    /// </summary>
    public Metal Metal { get; }

    /// <summary>
    /// La forma: moneta o lingotto.
    /// </summary>
    public ProductForm Form { get; }

    /// <summary>
    /// Il peso lordo, cioè il peso totale del pezzo, lega compresa.
    /// </summary>
    public Weight GrossWeight { get; }

    /// <summary>
    /// Il titolo: la frazione di metallo puro, tra 0 (escluso) e 1 (incluso).
    /// Per esempio 0,9999 per i lingotti, 0,9167 per le monete a 22 carati.
    /// </summary>
    public decimal Fineness { get; }

    /// <summary>
    /// Il peso fino: la quantità di metallo puro contenuta (peso lordo × titolo).
    /// È il peso su cui si calcola il valore.
    /// </summary>
    /// <remarks>
    /// Di solito è peso lordo × titolo. Se il prodotto è stato creato con
    /// <see cref="FromFineWeight"/>, è il peso fino dichiarato, senza arrotondamenti.
    /// </remarks>
    public Weight FineWeight { get; }

    /// <summary>
    /// Crea un prodotto.
    /// </summary>
    /// <param name="name">Il nome; non può essere vuoto.</param>
    /// <param name="metal">Il metallo; deve essere un valore definito di <see cref="Domain.Metal"/>.</param>
    /// <param name="form">La forma; deve essere un valore definito di <see cref="ProductForm"/>.</param>
    /// <param name="grossWeight">Il peso lordo; deve essere maggiore di zero.</param>
    /// <param name="fineness">Il titolo; deve essere maggiore di 0 e al massimo 1.</param>
    /// <exception cref="ArgumentException"><paramref name="name"/> è null, vuoto o fatto solo di spazi.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Metallo o forma non definiti, peso zero, o titolo fuori dall'intervallo (0, 1].</exception>
    public MetalProduct(string name, Metal metal, ProductForm form, Weight grossWeight, decimal fineness)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Name = name.Trim();
        Metal = DomainGuards.Defined(metal);
        Form = DomainGuards.Defined(form);
        GrossWeight = DomainGuards.Positive(grossWeight);
        Fineness = DomainGuards.Fineness(fineness);
        FineWeight = GrossWeight * Fineness;
    }

    private MetalProduct(string name, Metal metal, ProductForm form, Weight grossWeight, Weight fineWeight)
        : this(name, metal, form, grossWeight, FinenessOf(grossWeight, fineWeight))
    {
        FineWeight = fineWeight;
    }

    /// <summary>
    /// Crea un prodotto a partire dal peso lordo e dal peso fino dichiarati,
    /// come li riporta un catalogo (es. Krugerrand: 33,93 g lordi, 31,10 g fini).
    /// </summary>
    /// <remarks>
    /// Il titolo viene ricavato (peso fino ÷ peso lordo), ma il peso fino resta
    /// esattamente quello passato: nessun errore di arrotondamento sul valore.
    /// </remarks>
    /// <param name="name">Il nome; non può essere vuoto.</param>
    /// <param name="metal">Il metallo.</param>
    /// <param name="form">La forma.</param>
    /// <param name="grossWeight">Il peso lordo; maggiore di zero.</param>
    /// <param name="fineWeight">Il peso fino; maggiore di zero e non superiore al peso lordo.</param>
    /// <returns>Il prodotto.</returns>
    /// <exception cref="ArgumentException"><paramref name="name"/> è null, vuoto o fatto solo di spazi.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Metallo o forma non definiti, pesi a zero, o peso fino maggiore del lordo.</exception>
    public static MetalProduct FromFineWeight(string name, Metal metal, ProductForm form, Weight grossWeight, Weight fineWeight)
        => new(name, metal, form, grossWeight, fineWeight);

    private static decimal FinenessOf(Weight grossWeight, Weight fineWeight)
    {
        DomainGuards.Positive(grossWeight);
        DomainGuards.Positive(fineWeight);

        if (fineWeight > grossWeight)
        {
            throw new ArgumentOutOfRangeException(
                nameof(fineWeight),
                fineWeight,
                $"Il peso fino ({fineWeight}) non può superare il peso lordo ({grossWeight}).");
        }

        return fineWeight.Grams / grossWeight.Grams;
    }
}