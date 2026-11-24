# Changelog

Tutte le modifiche rilevanti di Heirloom.Financial. Il formato segue
[Keep a Changelog](https://keepachangelog.com/it-IT/1.1.0/) e le versioni seguono il
[versionamento semantico](https://semver.org/lang/it/).

## [1.5.0] - 2026-11-23

### Aggiunto
- `Results.HoldingsAnalyzer.Summarize`: riepilogo di ciò che si possiede senza prezzi di mercato (`HoldingsSummary`):
  lotti aperti, prodotti, pezzi (anche a costo sconosciuto), peso lordo e fino, costo di carico, anzianità media
  pesata sul peso lordo, flusso di cassa; lo stesso per metallo (`HoldingsMetal`, con costo medio per grammo fino).
  Gli stessi lotti aperti e lo stesso flusso di cassa di `WalletAnalyzer`.

## [1.4.0] - 2026-11-22

### Aggiunto
- `Market.Candle`: candela di mercato (apertura, massimo, minimo, chiusura con valuta, volume, scambi), con
  `ConvertTo` (i quattro prezzi con il cambio del giorno della candela) e `ToSpot` (la chiusura come spot del giorno).
- `Market.CandleAggregator.Aggregate`: unisce più candele in una (es. le orarie di un giorno nella giornaliera).

## [1.3.0] - 2026-11-22

Lo storico per i grafici si calcola dalla libreria a partire da transazioni e prezzi giornalieri,
senza fotografie salvate.

### Aggiunto
- `HistoryPoint`: ogni giorno anche costo di carico, guadagno sulla carta (valore e percentuale, solo pezzi a costo noto)
  e `Metals`, con valore, costo e spot per grammo fino di ogni metallo.
- Pipeline `Release`: si ferma se il `CHANGELOG.md` del commit taggato non ha la sezione della versione del tag.

## [1.2.1] - 2026-11-22

### Corretto
- Ripubblicazione della 1.2.0: il pacchetto 1.2.0 era stato creato da un `main` senza le modifiche della versione.
  Non usare la 1.2.0.

## [1.2.0] - 2026-11-22

Tutti i calcoli finanziari di Heirloom passano alla libreria: Heirloom.Core legge e scrive il database e non calcola più nulla.

### Aggiunto
- `Lot.HasDocumentedCost`: se il costo d'acquisto è documentato (fattura).
- `CostBasisResult.OpenLots`: gli acquisti di cui restano dei pezzi, con il costo di quelli rimasti (FIFO e lotto specifico).
- Modulo `Taxation`: interfaccia `ITaxPolicy` e regola `ItalianTaxPolicy` (26% sulla plusvalenza con costo documentato,
  sull'intero valore senza documentazione).
- `WalletAnalysisOptions.TaxPolicy` e nel riepilogo `EstimatedTax`, `ValueAfterTax`, `UnrealizedPnLAfterTax`.
- Nel riepilogo: `Lots` (un lotto per acquisto, con valore, costo, guadagno, quota sul totale e tasse),
  `ByMetal` (pesi, valore, costo, costo medio e spot per grammo fino, quota monete/lingotti) e `CashFlow` (acquisti e vendite).

## [1.1.0] - 2026-11-22

Prima versione pubblicata come pacchetto NuGet.

### Modificato
- Solo .NET 10: rimosso `net9.0` ora che Heirloom è migrato (HA-044).
- `Weight.ToString()` usa sempre il punto decimale, come `Money` e `FxRate`, indipendentemente dalla lingua del sistema.

### Aggiunto
- Pacchetto NuGet `Heirloom.Financial` con README, documentazione XML e simboli di debug inclusi.
- Pipeline `Release`: a ogni tag `vX.Y.Z` su `main` compila, testa e pubblica su GitHub Packages.
- Test sui casi al confine emersi dal mutation testing (punteggio Stryker 81%).

## [1.0.0] - 2026-11-22

Prima versione stabile, pronta per l'integrazione in Heirloom.

### Domain
- `Metal`, `Currency`, `ProductForm`, `LotSide` con codici ISO 4217 (`XAU`, `XAG`, `EUR`, `USD`).
- `Weight` e `Money` come valori immutabili; importi sempre in `decimal`.
- `MetalProduct` da peso lordo e titolo, oppure da peso lordo e peso fino (`FromFineWeight`).
- `Lot` con prezzo per pezzo (`Buy`, `Sell`) o totale dello scontrino (`BuyForTotal`, `SellForTotal`),
  identificativo (`Id`), collegamento vendita → acquisto (`MatchedLotId`) e acquisti a costo sconosciuto
  (`BuyWithUnknownCost`) per pezzi ereditati o regalati.
- `SpotPrice`, `FxRate`, `WalletSnapshot`, costanti di unità e titoli comuni, controlli in `DomainGuards`.

### Cambi
- `CurrencyConverter`, `FxRateSeries` (ultimo cambio disponibile, ricerca binaria) e
  `LotCurrencyConverter` (ogni lotto con il cambio della sua data).

### Valutazione
- Valore di fusione, premio, e tre strategie: spot, spot più premio, riacquisto.

### Posizioni e guadagni
- Posizioni, costo di carico con media ponderata, FIFO o lotto specifico (`SpecificLot`).
- Guadagno realizzato e non realizzato; i pezzi a costo sconosciuto contano nel valore ma non nel guadagno.
- Ripartizione per metallo e per forma, con percentuali che sommano a 1.

### Rendimenti e rischio
- Rendimenti semplici, logaritmici, cumulati e annualizzati; TWR con indice; XIRR (Newton e bisezione).
- Scomposizione metallo/cambio, ricostruzione dello storico giornaliero (`SnapshotBuilder`).
- Volatilità annualizzata, drawdown, Sharpe; rapporto oro/argento, SMA ed EMA.

### Risultati
- `WalletAnalyzer`: un'unica chiamata che produce un `WalletSummary` arrotondato
  (importi a 2 decimali, pesi e percentuali a 4) e serializzabile in JSON con enum e valute come stringhe.

### Qualità
- Test unitari, property test con FsCheck, casi di riferimento verificati con Excel e Python, casi estremi.
- CI su .NET 9 e .NET 10 con copertura minima del 90% delle righe; configurazione di Stryker.NET.

[1.4.0]: https://github.com/samuele-risso/Heirloom.Financial/releases/tag/v1.4.0
[1.3.0]: https://github.com/samuele-risso/Heirloom.Financial/releases/tag/v1.3.0
[1.2.1]: https://github.com/samuele-risso/Heirloom.Financial/releases/tag/v1.2.1
[1.2.0]: https://github.com/samuele-risso/Heirloom.Financial/releases/tag/v1.2.0
[1.1.0]: https://github.com/samuele-risso/Heirloom.Financial/releases/tag/v1.1.0
[1.0.0]: https://github.com/samuele-risso/Heirloom.Financial/releases/tag/v1.0.0
