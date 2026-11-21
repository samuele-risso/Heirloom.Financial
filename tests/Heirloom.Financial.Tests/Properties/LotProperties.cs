using FsCheck.Xunit;
using Heirloom.Financial.Domain;
using Heirloom.Financial.Tests.TestData;

namespace Heirloom.Financial.Tests.Properties;

/// <summary>
/// Regole di Lot che devono valere per qualsiasi quantità, prezzo e costo validi.
/// Quantità in pezzi (ushort), prezzo e costi in centesimi (uint, ushort).
/// </summary>
public class LotProperties
{
    private static readonly DateOnly Day = new(2024, 6, 1);

    private static (Lot Buy, Lot Sell)? Create(ushort quantity, uint priceCents, ushort feeCents)
    {
        if (quantity == 0)
        {
            return null;
        }

        var price = new Money(priceCents / 100m, Currency.EUR);
        var fees = new Money(feeCents / 100m, Currency.EUR);

        return (Lot.Buy(TestProducts.Krugerrand1Oz, quantity, price, fees, Day),
                Lot.Sell(TestProducts.Krugerrand1Oz, quantity, price, fees, Day));
    }

    [Property]
    public bool Acquisto_e_vendita_differiscono_di_due_volte_i_costi(ushort quantity, uint priceCents, ushort feeCents)
    {
        var pair = Create(quantity, priceCents, feeCents);
        if (pair is null)
        {
            return true;
        }

        var (buy, sell) = pair.Value;

        return buy.NetAmount - sell.NetAmount == buy.Fees * 2m;
    }

    [Property]
    public bool Il_flusso_di_cassa_di_un_acquisto_e_l_opposto_del_netto(ushort quantity, uint priceCents, ushort feeCents)
    {
        var pair = Create(quantity, priceCents, feeCents);
        if (pair is null)
        {
            return true;
        }

        var buy = pair.Value.Buy;

        return buy.CashFlow == -buy.NetAmount;
    }

    [Property]
    public bool Il_peso_fino_e_proporzionale_alla_quantita(ushort quantity, uint priceCents, ushort feeCents)
    {
        var pair = Create(quantity, priceCents, feeCents);
        if (pair is null)
        {
            return true;
        }

        var buy = pair.Value.Buy;

        return buy.FineWeight == TestProducts.Krugerrand1Oz.FineWeight * quantity;
    }
}