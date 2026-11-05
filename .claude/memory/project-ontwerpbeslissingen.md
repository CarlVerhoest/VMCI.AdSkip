---
name: project-ontwerpbeslissingen
description: Genomen ontwerpbeslissingen voor VMCI.AdSkip en waarom
metadata:
  type: project
---

- **Windows UI Automation (UIA3 via FlaUI), niet het netwerk.** Carl stelde voor de API-call van YouTube te onderscheppen; afgewezen omdat die call geen stabiel contract is en een extensie/proxy nodig zou hebben. In de plaats: gelaagde matching met de patronen in `appsettings.json` (CSS-klasse eerst, naam als terugval, alleen terwijl er een advertentie speelt) en een waarschuwing als er na 15 s speeltijd geen knop gevonden wordt, zodat breuk door een YouTube-wijziging opvalt.
- **Chrome en Firefox, nooit Edge** (wens van Carl). Firefox: native UIA-provider (Gecko), geen instellingen nodig. Edge niet toevoegen, ook niet als "gratis" Chromium-variant.
- **Geen Windows-service.** Services draaien in sessie 0 zonder bureaublad: geen toegang tot de browservensters van de gebruiker en geen tray-icoon. AdSkip start daarom **bij het aanmelden** (HKCU Run-sleutel, waarde `VMCI.AdSkip`), niet bij het opstarten van Windows.
- **Twee apps, één `appsettings.json`:** `AdSkipTray.exe` (tray, self-contained, de dagelijkse app) en `AdSkip.exe` (console, framework-dependent, om te testen).
- **Distributie = optie 1: handmatig kopiëren** (`AdSkipTray.exe` + `appsettings.json`) naar elke pc. Automatische updates (bv. Velopack met GitHub Releases) is afgewogen en uitgesteld (2026-11-03); pas voorstellen als het handmatig bijwerken lastig wordt.
- **.NET 10** (laatste uitgebrachte versie op 2026-11-03; .NET 11 was nog RC). Tests: xUnit v3 op Microsoft.Testing.Platform, coverage met Microsoft.Testing.Extensions.CodeCoverage (de VS Enterprise-engine).
