namespace Heirloom.Financial.Domain;

/// <summary>
/// Un'operazione dell'utente su un prodotto: un acquisto o una vendita, in una data.
/// </summary>
/// <remarks>
/// <para>
/// Tutto il portafoglio si ricostruisce a partire dai lotti: posizioni, costo di carico,
/// guadagni e storico derivano da questo elenco di operazioni.
/// </para>
/// <para>
/// La quantità è sempre positiva: il verso dell'operazione è in <see cref="Side"/>.
/// Prezzo unitario e costi accessori (commissioni, spedizione, IVA) devono essere
/// nella stessa valuta.
/// </para>
/// </remarks>
public sealed record Lot
{
    /// <summary>
    /// Il prodotto acquistato o venduto.
    /// </summary>
    public MetalProduct Product { get; }

    /// <summary>
    /// Il numero di pezzi. È sempre maggiore di zero.
    /// </summary>
    public decimal Quantity { get; }

    /// <summary>
    /// Il prezzo di un singolo pezzo, costi accessori esclusi.
    /// </summary>
    public Money UnitPrice { get; }

    /// <summary>
    /// I costi accessori dell'operazione: commissioni, spedizione, assicurazione, IVA.
    /// Nella stessa valuta del prezzo. Zero se non ci sono.
    /// </summary>
    public Money Fees { get; }

    /// <summary>
    /// La data dell'operazione.
    /// </summary>
    public DateOnly Date { get; }

    /// <summary>
    /// Il verso dell'operazione: acquisto o vendita.
    /// </summary>
    public LotSide Side { get; }

    /// <summary>
    /// La valuta dell'operazione (quella del prezzo e dei costi).
    /// </summary>
    public Currency Currency => UnitPrice.Currency;

    /// <summary>
    /// Il controvalore lordo: quantità × prezzo unitario, senza costi accessori.
    /// </summary>
    public Money GrossAmount => UnitPrice * Quantity;

    /// <summary>
    /// Il controvalore netto, con i costi accessori.
    /// Per un acquisto è quanto si è speso in tutto (lordo + costi);
    /// per una vendita è quanto si è incassato davvero (lordo − costi).
    /// </summary>
    public Money NetAmount => Side == LotSide.Buy ? GrossAmount + Fees : GrossAmount - Fees;

    /// <summary>
    /// Il flusso di cassa dal punto di vista dell'utente: negativo per un acquisto
    /// (soldi che escono), positivo per una vendita (soldi che entrano).
    /// Servirà per il calcolo del rendimento personale (XIRR).
    /// </summary>
    public Money CashFlow => Side == LotSide.Buy ? -NetAmount : NetAmount;

    /// <summary>
    /// Il peso fino totale dell'operazione: peso fino del prodotto × quantità.
    /// </summary>
    public Weight FineWeight => Product.FineWeight * Quantity;

    /// <summary>
    /// Crea un'operazione.
    /// </summary>
    /// <param name="product">Il prodotto; non può essere null.</param>
    /// <param name="quantity">Il numero di pezzi; deve essere maggiore di zero.</param>
    /// <param name="unitPrice">Il prezzo di un pezzo; non può essere negativo.</param>
    /// <param name="fees">I costi accessori; non negativi e nella stessa valuta del prezzo.</param>
    /// <param name="date">La data dell'operazione.</param>
    /// <param name="side">Il verso: acquisto o vendita.</param>
    /// <exception cref="ArgumentNullException"><paramref name="product"/> è null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Quantità non positiva, prezzo o costi negativi, verso non definito.</exception>
    /// <exception cref="ArgumentException">Prezzo e costi sono in valute diverse.</exception>
    public Lot(MetalProduct product, decimal quantity, Money unitPrice, Money fees, DateOnly date, LotSide side)
    {
        ArgumentNullException.ThrowIfNull(product);

        if (quantity <= 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), quantity, "La quantità deve essere maggiore di zero.");
        }

        if (unitPrice.IsNegative)
        {
            throw new ArgumentOutOfRangeException(nameof(unitPrice), unitPrice, "Il prezzo non può essere negativo.");
        }

        if (fees.IsNegative)
        {
            throw new ArgumentOutOfRangeException(nameof(fees), fees, "I costi accessori non possono essere negativi.");
        }

        if (fees.Currency != unitPrice.Currency)
        {
            throw new ArgumentException(
                $"Prezzo e costi devono essere nella stessa valuta: {unitPrice.Currency.ToIsoCode()} e {fees.Currency.ToIsoCode()}.",
                nameof(fees));
        }

        if (!Enum.IsDefined(side))
        {
            throw new ArgumentOutOfRangeException(nameof(side), side, "Verso dell'operazione non supportato.");
        }

        Product = product;
        Quantity = quantity;
        UnitPrice = unitPrice;
        Fees = fees;
        Date = date;
        Side = side;
    }

    /// <summary>
    /// Crea un acquisto.
    /// </summary>
    /// <param name="product">Il prodotto acquistato.</param>
    /// <param name="quantity">Il numero di pezzi.</param>
    /// <param name="unitPrice">Il prezzo di un pezzo.</param>
    /// <param name="fees">I costi accessori.</param>
    /// <param name="date">La data dell'acquisto.</param>
    /// <returns>Il lotto di acquisto.</returns>
    public static Lot Buy(MetalProduct product, decimal quantity, Money unitPrice, Money fees, DateOnly date)
        => new(product, quantity, unitPrice, fees, date, LotSide.Buy);

    /// <summary>
    /// Crea una vendita.
    /// </summary>
    /// <param name="product">Il prodotto venduto.</param>
    /// <param name="quantity">Il numero di pezzi.</param>
    /// <param name="unitPrice">Il prezzo di un pezzo.</param>
    /// <param name="fees">I costi accessori.</param>
    /// <param name="date">La data della vendita.</param>
    /// <returns>Il lotto di vendita.</returns>
    public static Lot Sell(MetalProduct product, decimal quantity, Money unitPrice, Money fees, DateOnly date)
        => new(product, quantity, unitPrice, fees, date, LotSide.Sell);
}