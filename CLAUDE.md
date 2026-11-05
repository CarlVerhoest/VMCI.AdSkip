# CLAUDE.md: VMCI.AdSkip

Windows-app die de **Skip**-knop van YouTube-advertenties in Chrome of Firefox indrukt via Windows UI Automation (FlaUI, UIA3), zodat Carl tijdens het sporten niet naar het toetsenbord moet. Geen adblocker: hij wacht tot YouTube de knop aanbiedt.
Zie [README.md](README.md) voor de werking, de instellingen en het gebruik.

## Gedeeld geheugen (belangrijk)

Dit project wordt op **meerdere pc's** gebruikt. Alle kennis en voortgang van Claude staat daarom **in de repo**:

- Het auto-memory staat in **`.claude/memory/`** (index: `MEMORY.md`). Op elke pc is de lokale Claude-geheugenmap via `scripts/setup-claude-memory.ps1` een junction naar die map.
- Schrijf nieuwe herinneringen **altijd** in `.claude/memory/`, ook als de junction ontbreekt, en houd `MEMORY.md` bij.
- Werk na afgeronde taken [`.claude/memory/project-voortgang.md`](.claude/memory/project-voortgang.md) bij: wat is gedaan, wat nog niet getest is en wat open staat (met datum). Commit dat samen met het werk.
- Zet niets in het geheugen wat al in code, git-historie, README of deze file staat.

@.claude/memory/MEMORY.md

## Nieuwe pc (eenmalig)

Instructies voor Claude als dit de eerste sessie op een pc is (of als het geheugen hierboven leeg lijkt):

1. De repo moet op **hetzelfde pad** staan als op de andere pc's: `C:\DATA\Cave\MyRepos\VMCI.AdSkip`. De naam van Claudes geheugenmap wordt afgeleid van dat pad. Staat de repo elders, meld dat aan Carl en stel voor hem te verplaatsen.
2. Controleer of de junction bestaat: `%USERPROFILE%\.claude\projects\C--DATA-Cave-MyRepos-VMCI-AdSkip\memory` moet naar `<repo>\.claude\memory` wijzen. Zo niet, voer uit (veilig om te herhalen; bestaande lokale herinneringen worden eerst naar de repo gekopieerd):
   ```powershell
   powershell -ExecutionPolicy Bypass -File scripts/setup-claude-memory.ps1
   ```
   Het geheugen wordt pas vanaf de volgende sessie automatisch geladen; deze sessie heeft het al via de `@`-import hierboven.
3. Vereisten voor bouwen: .NET 10 SDK (`dotnet --list-sdks`). Visual Studio Enterprise is optioneel.
4. AdSkip op die pc gebruiken: `.\publish.ps1`, daarna `publish\AdSkipTray.exe` starten (laat Carl dubbelklikken of start via zijn terminal, zie de valkuilen in het geheugen) en in het tray-menu **Start with Windows** aanvinken. Alleen gebruiken: kopieer `AdSkipTray.exe` en `appsettings.json` van een andere pc, er is geen .NET nodig.

## Commando's

| Doel | Commando |
| ---- | -------- |
| Bouwen | `dotnet build` |
| Tests | `dotnet test` (xUnit v3 op Microsoft.Testing.Platform, via `global.json`) |
| Coverage | `dotnet test --coverage --coverage-output-format cobertura` |
| Publiceren | `.\publish.ps1` → `publish\AdSkipTray.exe` (self-contained), `publish\AdSkip.exe` (console), `appsettings.json` |
| Console testen | `publish\AdSkip.exe` (loopt tot Ctrl+C) |

## Werkafspraken

- Werk op `main`, zonder PR's; commit en push alleen als Carl erom vraagt (zie [[feedback-werkwijze]] in het geheugen).
- Taal van communicatie: die van Carl (Nederlands of Engels). Code, identifiers, commit-messages en README: Engels. `CLAUDE.md` en het geheugen: Nederlands.
- Alle logica zit in **VMCI.AdSkip.Core** achter kleine interfaces en is unit-getest; **VMCI.AdSkip.Automation** (FlaUI) en **VMCI.AdSkip.Tray** (WinForms) blijven dunne lagen. Nieuwe logica krijgt tests in **VMCI.AdSkip.Tests.Unit**.
- Patronen die YouTube kan wijzigen (klassen, namen, vensterklassen) horen in `appsettings.json`, niet in code. Array-standaardwaarden in `AdSkipSettings` blijven leeg (de binder voegt anders toe).
- Nooit Edge ondersteunen. Geen Windows-service (zie de ontwerpbeslissingen).
- Controleer gedrag in een echte browser met een echte advertentie voordat je iets als werkend meldt, en zeg wat niet getest is.
