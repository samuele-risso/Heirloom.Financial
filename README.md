# FinancialLibrary

Libreria .NET di calcolo finanziario per i portafogli di metalli preziosi (oro e argento, monete e lingotti) di Heirloom.

Contiene solo calcoli puri: nessuna dipendenza da database, Entity Framework o ASP.NET.

## Struttura

```
FinancialLibrary.slnx
Directory.Build.props        impostazioni comuni (nullable, warning come errori)
src/FinancialLibrary/        la libreria (net9.0 e net10.0)
tests/FinancialLibrary.Tests/ i test xUnit
```

## Comandi

```
dotnet build
dotnet test
```

## Convenzioni

- Importi sempre in `decimal`; `double` solo per XIRR e volatilità.
- Pesi salvati internamente in grammi; 1 oncia troy = 31,1034768 g.
- Enum con valori espliciti a partire da 1: lo 0 non è mai un valore valido.
