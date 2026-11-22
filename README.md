# Heirloom.Financial

Libreria .NET di calcolo finanziario per i portafogli di metalli preziosi (oro e argento, monete e lingotti) di **Heirloom**.

Contiene solo calcoli puri: nessuna dipendenza da database, Entity Framework, ASP.NET o pacchetti esterni.
Riceve operazioni, prezzi e cambi; restituisce valori, guadagni, rendimenti e rischio.

![CI](https://github.com/samuele-risso/Heirloom.Financial/actions/workflows/ci.yml/badge.svg)

## Installazione

La libreria è pubblicata come pacchetto NuGet privato su GitHub Packages.

1. Crea un token GitHub (classic) con il solo permesso `read:packages` e salvalo nella variabile
   d'ambiente `GITHUB_PACKAGES_TOKEN`.
2. Nel progetto che la usa, aggiungi un `nuget.config` con la sorgente
   `https://nuget.pkg.github.com/samuele-risso/index.json` e le credenziali prese da quella variabile.
3. `dotnet add package Heirloom.Financial --version 1.1.0`

## Uso in una riga

```csharp
var summary = WalletAnalyzer.Analyze(lots, spots, new WalletAnalysisOptions
{
    Currency = Currency.EUR,
    ValuationDate = DateOnly.FromDateTime(DateTime.Today),
    Valuation = SpotValuation.Instance,
    CostBasisMethod = CostBasisMethod.SpecificLot,
    AnnualizationBasis = AnnualizationBasis.CalendarDays365,
    RiskFreeRate = 0.025m,   // facoltativo: senza, lo Sharpe è assente
}, rates);                   // facoltativo: serve se lotti o spot sono in un'altra valuta

return Results.Ok(summary);  // JSON pronto per il frontend
```

`WalletSummary` contiene totali, posizioni, ripartizione per metallo e forma, rendimenti (TWR, XIRR,
storico giornaliero) e rischio (volatilità, drawdown, Sharpe). Tutti gli importi sono già arrotondati a
2 decimali con la loro valuta, pesi e percentuali a 4. Se mancano i prezzi storici, il riepilogo arriva
comunque, senza rendimenti, con il motivo in `Notes`.

## Come creare i lotti

| Caso | Come |
|---|---|
| Acquisto con il prezzo per pezzo | `Lot.Buy(prodotto, quantità, prezzoUnitario, costi, data)` |
| Acquisto con il totale dello scontrino | `Lot.BuyForTotal(prodotto, quantità, totale, costi, data)` |
| Vendita | `Lot.Sell(...)` o `Lot.SellForTotal(...)` |
| Pezzi ereditati o regalati, costo sconosciuto | `Lot.BuyWithUnknownCost(prodotto, quantità, data, valuta)` |
| Vendita da un acquisto preciso | `vendita with { MatchedLotId = acquisto.Id }` |

I prodotti si creano da peso lordo e titolo (`new MetalProduct(...)`) oppure da peso lordo e peso fino,
come nel catalogo di Heirloom (`MetalProduct.FromFineWeight(...)`).

**Pezzi a costo sconosciuto.** Contano in quantità, peso fino, valore e ripartizione; non contano nel costo
di carico e nei guadagni. Nei rendimenti entrano come conferimento al valore di mercato del giorno in cui
arrivano, così il loro ingresso non sembra un guadagno.

**Metodi di carico.** `WeightedAverage` (media ponderata), `Fifo` e `SpecificLot`. Con `SpecificLot` ogni
acquisto deve avere un `Id` e ogni vendita il `MatchedLotId` dell'acquisto da cui esce: è il metodo che
segue le fatture una per una.

## Struttura

```
Heirloom.Financial.slnx
Directory.Build.props              impostazioni comuni (nullable, warning come errori)
src/Heirloom.Financial/            la libreria (net10.0)
  Domain/        tipi di base: Metal, Currency, Weight, Money, MetalProduct, Lot, SpotPrice, FxRate, WalletSnapshot
  Constants/     grammi per oncia troy, titoli comuni
  Validation/    controlli sui dati in ingresso
  Exchange/      conversioni di valuta e serie dei cambi
  Valuation/     valore di fusione, premi, strategie di valutazione
  Positions/     posizioni, costo di carico, guadagni
  Allocation/    ripartizione per metallo e forma
  Performance/   rendimenti, TWR, XIRR, scomposizione del cambio, storico giornaliero
  Risk/          volatilità, drawdown, Sharpe
  Market/        serie di prezzi, rapporto oro/argento, SMA ed EMA
  Results/       WalletAnalyzer e i record dei risultati per l'API
  Internal/      arrotondamenti e ricerche (non visibili fuori dalla libreria)
tests/Heirloom.Financial.Tests/    test xUnit, property test FsCheck, casi di riferimento, casi estremi
```

## Comandi

```bash
dotnet build
dotnet test

# Copertura (report HTML in CoverageReport/)
dotnet test --collect:"XPlat Code Coverage" --results-directory ./TestResults
reportgenerator -reports:"TestResults/**/coverage.cobertura.xml" -targetdir:CoverageReport -assemblyfilters:"+Heirloom.Financial"

# Mutation testing (report in tests/Heirloom.Financial.Tests/StrykerOutput/)
cd tests/Heirloom.Financial.Tests
dotnet tool restore
dotnet stryker
```

La CI compila e testa su .NET 10 e fallisce se la copertura delle righe scende sotto il 90%.

## Convenzioni

- Importi sempre in `decimal`; `double` solo per XIRR, logaritmi e volatilità.
- Arrotondamento solo nei risultati, per eccesso dal 5 (`2,345 → 2,35`).
- Pesi salvati internamente in grammi; 1 oncia troy = 31,1034768 g.
- Cambi: `1 Base = Rate Quote`. Ogni lotto si converte con il cambio della sua data.
- Nessun dato "dal futuro": per ogni giorno si usano l'ultimo spot e l'ultimo cambio disponibili.
- Enum con valori espliciti a partire da 1: lo 0 non è mai un valore valido.
- Le scelte che cambiano i numeri (strategia, metodo di carico, base dei giorni, tasso privo di rischio)
  sono sempre parametri: la libreria non ha valori di mercato scritti nel codice.
- Le tasse non sono qui: restano in Heirloom.

## Versioni e rilasci

Le modifiche sono in [CHANGELOG.md](CHANGELOG.md). Si lavora su branch `feature/...` da `develop`;
`main` riceve solo i rilasci. Per pubblicare una versione:

1. aggiorna `CHANGELOG.md` e `<Version>` nel `.csproj`;
2. porta `develop` su `main` con una pull request;
3. crea e pubblica il tag su `main`: `git tag -a vX.Y.Z -m "..."` e `git push origin vX.Y.Z`.

La pipeline `Release` compila, testa e pubblica il pacchetto con la versione del tag.
