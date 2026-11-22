# Changelog

Tutte le modifiche rilevanti di Heirloom.Financial. Il formato segue
[Keep a Changelog](https://keepachangelog.com/it-IT/1.1.0/) e le versioni seguono il
[versionamento semantico](https://semver.org/lang/it/).

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
- `PortfolioAnalyzer`: un'unica chiamata che produce un `PortfolioSummary` arrotondato
  (importi a 2 decimali, pesi e percentuali a 4) e serializzabile in JSON con enum e valute come stringhe.

### Qualità
- Test unitari, property test con FsCheck, casi di riferimento verificati con Excel e Python, casi estremi.
- CI su .NET 9 e .NET 10 con copertura minima del 90% delle righe; configurazione di Stryker.NET.

[1.0.0]: https://github.com/samuele-risso/Heirloom.Financial/releases/tag/v1.0.0
