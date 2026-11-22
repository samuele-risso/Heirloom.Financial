using Heirloom.Financial.Validation;

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
/// Prezzo e costi accessori (commissioni, spedizione, IVA) devono essere nella stessa valuta.
/// </para>
/// <para>
/// Il prezzo si può dare per pezzo (<see cref="Buy"/>) oppure come totale pagato per tutti
/// i pezzi (<see cref="BuyForTotal"/>), come su uno scontrino. Con il totale, il controvalore
/// resta esattamente quello pagato, senza errori di arrotondamento.
/// </para>
/// <para>
/// Un acquisto può avere <b>costo sconosciuto</b> (<see cref="BuyWithUnknownCost"/>), per esempio
/// per monete ereditate o regalate: conta nel valore e nel peso del portafoglio, ma non nel
/// costo di carico né nei guadagni.
/// </para>
/// </remarks>
public sealed record Lot
{
    private readonly string? _id;
    private readonly string? _matchedLotId;
    private readonly bool _hasDocumentedCost;

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
    /// Se il lotto è stato creato da un totale, è il totale diviso per la quantità.
    /// </summary>
    public Money UnitPrice { get; private init; }

    /// <summary>
    /// Il controvalore lordo: quanto valgono tutti i pezzi, senza costi accessori.
    /// </summary>
    public Money GrossAmount { get; private init; }

    /// <summary>
    /// I costi accessori dell'operazione: commissioni, spedizione, assicurazione, IVA.
    /// Nella stessa valuta del prezzo. Zero se non ci sono.
    /// </summary>
    public Money Fees { get; private init; }

    /// <summary>
    /// La data dell'operazione.
    /// </summary>
    public DateOnly Date { get; }

    /// <summary>
    /// Il verso dell'operazione: acquisto o vendita.
    /// </summary>
    public LotSide Side { get; }

    /// <summary>
    /// <c>false</c> se il costo d'acquisto non è noto (eredità, regalo, scontrino perso).
    /// Per un lotto così prezzo, controvalore e costi sono zero. Le vendite hanno sempre un prezzo noto.
    /// </summary>
    public bool HasKnownCost { get; }

    /// <summary>
    /// Un identificativo facoltativo del lotto, per esempio l'id della riga nel database di Heirloom.
    /// Serve per collegare una vendita all'acquisto da cui esce (<see cref="MatchedLotId"/>).
    /// </summary>
    /// <exception cref="ArgumentException">Il valore è vuoto o fatto solo di spazi.</exception>
    public string? Id
    {
        get => _id;
        init => _id = NormalizeId(value, nameof(Id));
    }

    /// <summary>
    /// Solo per le vendite: l'<see cref="Id"/> dell'acquisto da cui escono i pezzi venduti.
    /// È obbligatorio con il metodo <c>CostBasisMethod.SpecificLot</c>, ignorato dagli altri.
    /// </summary>
    /// <exception cref="ArgumentException">Il valore è vuoto, oppure il lotto è un acquisto.</exception>
    public string? MatchedLotId
    {
        get => _matchedLotId;
        init
        {
            var id = NormalizeId(value, nameof(MatchedLotId));

            if (id is not null && Side != LotSide.Sell)
            {
                throw new ArgumentException("Solo una vendita può indicare il lotto da cui esce.", nameof(MatchedLotId));
            }

            _matchedLotId = id;
        }
    }

    /// <summary>
    /// Solo per gli acquisti: se il costo è documentato (in Heirloom: c'è la fattura).
    /// Serve alla stima delle tasse, che senza documentazione si calcolano sull'intero valore.
    /// Predefinito: <c>false</c>, cioè la stima più prudente.
    /// </summary>
    /// <exception cref="ArgumentException">Il lotto è una vendita.</exception>
    public bool HasDocumentedCost
    {
        get => _hasDocumentedCost;
        init
        {
            if (value && Side != LotSide.Buy)
            {
                throw new ArgumentException("Solo un acquisto può avere il costo documentato.", nameof(HasDocumentedCost));
            }

            _hasDocumentedCost = value;
        }
    }

    /// <summary>
    /// La valuta dell'operazione (quella del prezzo e dei costi).
    /// </summary>
    public Currency Currency => UnitPrice.Currency;

    /// <summary>
    /// Il controvalore netto, con i costi accessori.
    /// Per un acquisto è quanto si è speso in tutto (lordo + costi);
    /// per una vendita è quanto si è incassato davvero (lordo − costi).
    /// Zero per un acquisto a costo sconosciuto.
    /// </summary>
    public Money NetAmount => Side == LotSide.Buy ? GrossAmount + Fees : GrossAmount - Fees;

    /// <summary>
    /// Il flusso di cassa dal punto di vista dell'utente: negativo per un acquisto
    /// (soldi che escono), positivo per una vendita (soldi che entrano).
    /// Zero per un acquisto a costo sconosciuto: non è uscito denaro.
    /// </summary>
    public Money CashFlow => Side == LotSide.Buy ? -NetAmount : NetAmount;

    /// <summary>
    /// Il peso fino totale dell'operazione: peso fino del prodotto × quantità.
    /// </summary>
    public Weight FineWeight => Product.FineWeight * Quantity;

    /// <summary>
    /// Crea un'operazione con il prezzo per pezzo.
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
        : this(product, quantity, date, side, hasKnownCost: true)
    {
        UnitPrice = DomainGuards.NonNegative(unitPrice);
        DomainGuards.NonNegative(fees);
        Fees = DomainGuards.SameCurrency(unitPrice, fees);
        GrossAmount = UnitPrice * Quantity;
    }

    private Lot(MetalProduct product, decimal quantity, DateOnly date, LotSide side, bool hasKnownCost)
    {
        ArgumentNullException.ThrowIfNull(product);

        Product = product;
        Quantity = DomainGuards.Positive(quantity);
        Date = date;
        Side = DomainGuards.Defined(side);
        HasKnownCost = hasKnownCost;
    }

    /// <summary>
    /// Crea un acquisto con il prezzo per pezzo.
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
    /// Crea una vendita con il prezzo per pezzo.
    /// </summary>
    /// <param name="product">Il prodotto venduto.</param>
    /// <param name="quantity">Il numero di pezzi.</param>
    /// <param name="unitPrice">Il prezzo di un pezzo.</param>
    /// <param name="fees">I costi accessori.</param>
    /// <param name="date">La data della vendita.</param>
    /// <returns>Il lotto di vendita.</returns>
    public static Lot Sell(MetalProduct product, decimal quantity, Money unitPrice, Money fees, DateOnly date)
        => new(product, quantity, unitPrice, fees, date, LotSide.Sell);

    /// <summary>
    /// Crea un acquisto dal totale pagato per tutti i pezzi, costi accessori esclusi
    /// (es. 3 Krugerrand pagati 6.000 € in tutto).
    /// </summary>
    /// <param name="product">Il prodotto acquistato.</param>
    /// <param name="quantity">Il numero di pezzi.</param>
    /// <param name="totalPrice">Il totale pagato per i pezzi; non negativo.</param>
    /// <param name="fees">I costi accessori, nella stessa valuta.</param>
    /// <param name="date">La data dell'acquisto.</param>
    /// <returns>Il lotto di acquisto, con <see cref="GrossAmount"/> uguale al totale.</returns>
    public static Lot BuyForTotal(MetalProduct product, decimal quantity, Money totalPrice, Money fees, DateOnly date)
        => FromTotal(product, quantity, totalPrice, fees, date, LotSide.Buy);

    /// <summary>
    /// Crea una vendita dal totale incassato per tutti i pezzi, costi accessori esclusi.
    /// </summary>
    /// <param name="product">Il prodotto venduto.</param>
    /// <param name="quantity">Il numero di pezzi.</param>
    /// <param name="totalPrice">Il totale incassato per i pezzi; non negativo.</param>
    /// <param name="fees">I costi accessori, nella stessa valuta.</param>
    /// <param name="date">La data della vendita.</param>
    /// <returns>Il lotto di vendita, con <see cref="GrossAmount"/> uguale al totale.</returns>
    public static Lot SellForTotal(MetalProduct product, decimal quantity, Money totalPrice, Money fees, DateOnly date)
        => FromTotal(product, quantity, totalPrice, fees, date, LotSide.Sell);

    /// <summary>
    /// Crea un acquisto di cui non si conosce il costo: pezzi ereditati, regalati,
    /// o comprati senza conservare il prezzo.
    /// </summary>
    /// <remarks>
    /// <para>Questi pezzi <b>contano</b> nelle quantità, nel peso fino, nel valore e nella ripartizione per valore.</para>
    /// <para>
    /// <b>Non contano</b> nel costo di carico e nei guadagni (realizzati e non): un guadagno
    /// senza costo sarebbe inventato.
    /// </para>
    /// <para>
    /// Nei rendimenti (TWR, XIRR) entrano come un conferimento al valore di mercato del giorno
    /// in cui arrivano: si misura come sono andati da quel giorno in poi.
    /// </para>
    /// </remarks>
    /// <param name="product">Il prodotto ricevuto.</param>
    /// <param name="quantity">Il numero di pezzi.</param>
    /// <param name="date">La data in cui i pezzi sono entrati nel portafoglio.</param>
    /// <param name="currency">La valuta del portafoglio in cui entra il lotto.</param>
    /// <returns>Il lotto di acquisto con <see cref="HasKnownCost"/> a <c>false</c>.</returns>
    public static Lot BuyWithUnknownCost(MetalProduct product, decimal quantity, DateOnly date, Currency currency)
    {
        DomainGuards.Defined(currency);
        var zero = Money.Zero(currency);

        return new Lot(product, quantity, date, LotSide.Buy, hasKnownCost: false)
        {
            UnitPrice = zero,
            GrossAmount = zero,
            Fees = zero,
        };
    }

    /// <summary>
    /// Lo stesso lotto con gli importi trasformati (es. convertiti in un'altra valuta).
    /// Id, collegamento alla vendita e costo sconosciuto restano invariati.
    /// </summary>
    internal Lot WithAmounts(Func<Money, Money> convert) => this with
    {
        UnitPrice = convert(UnitPrice),
        GrossAmount = convert(GrossAmount),
        Fees = convert(Fees),
    };

    private static Lot FromTotal(MetalProduct product, decimal quantity, Money totalPrice, Money fees, DateOnly date, LotSide side)
    {
        DomainGuards.NonNegative(totalPrice);
        DomainGuards.NonNegative(fees);
        DomainGuards.SameCurrency(totalPrice, fees);

        return new Lot(product, quantity, date, side, hasKnownCost: true)
        {
            UnitPrice = totalPrice / DomainGuards.Positive(quantity),
            GrossAmount = totalPrice,
            Fees = fees,
        };
    }

    private static string? NormalizeId(string? value, string paramName)
    {
        if (value is null)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("L'identificativo non può essere vuoto: usa null se non c'è.", paramName);
        }

        return value.Trim();
    }
}