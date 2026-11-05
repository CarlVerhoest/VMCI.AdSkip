---
name: reference-technische-weetjes
description: YouTube-markup, gedrag van Chrome/Firefox onder UI Automation en valkuilen bij het testen
metadata:
  type: reference
---

## YouTube-markup (gezien op 2026-11-03)
- Speler: AutomationId `movie_player` (= HTML-id). Klassen bevatten `ad-showing` tijdens een advertentie, `paused-mode` of `playing-mode`.
- Skip-knop: `[Button] Name='Skip' Class='ytp-ad-skip-button-modern ytp-button'`. Vóór het einde van de aftelling bestaat alleen een leeg slot `ytp-ad-skip-ad-slot` (0×0) en de tekst "You can skip to video in N". **Gepauzeerd stopt de aftelling**, dus dan komt er geen knop.
- Adverteerderslink: klasse `ytp-ad-visit-advertiser-button`, naam bv. "eneco.be This link opens in new tab".
- `InvokePattern.Invoke()` slaat over, ook als de advertentie gepauzeerd is; de muis is niet nodig.
- Werkt ook in ingesloten (cross-origin) YouTube-iframes, bv. op lolesports.com.

## Browsers
- Chrome: vensterklasse `Chrome_WidgetWin_1`, die Electron-apps (Slack, Claude) delen: filter op het chrome.exe-proces. Toegankelijkheid gaat lui aan; als er geen webinhoud zichtbaar is: Chrome starten met `--force-renderer-accessibility`. De oude managed `System.Windows.Automation` (PowerShell 5.1) ziet geen webinhoud, UIA3 wel.
- Firefox 159: vensterklasse `MozillaWindowClass`, native UIA-provider (FrameworkId `Gecko`, xul.dll), geen voorkeuren nodig; ~18 ms per scan (Chrome ~22 ms). Firefox heeft verborgen vensters zonder titel: die worden overgeslagen.

## Valkuilen
- **Een programma dat Claude via de PowerShell- of Bash-tool start (`Start-Process`), wordt na een paar minuten stil gestopt.** Langlopende apps (tray-app, console-AdSkip) starten via de terminal van de gebruiker (`run_in_terminal`) of Carl laten dubbelklikken.
- Draait AdSkip nog, dan verdwijnt elke Skip-knop meteen; zeg duidelijk of het draait voordat Carl een testgeval klaarzet. Een draaiende `AdSkipTray.exe` blokkeert ook `publish.ps1` (exe in gebruik): eerst Exit.
- Lange paden (> 260 tekens, zoals de scratchpad-map) breken GDI+ en Python: werk via `%TEMP%`.
- De configuration binder voegt toe aan niet-lege standaard-arrays: array-standaardwaarden in `AdSkipSettings` blijven leeg (regressietest bestaat).
- Na `git switch`/merge staan bestanden met CRLF in de werkmap (autocrlf); de repo bewaart LF. Tekstvervangingen met `\n` mislukken dan.
- Linker-klik op het tray-icoon opent het menu via reflectie op het private `NotifyIcon.ShowContextMenu` (gecontroleerd op .NET 10.0.14); controleer dat na een .NET-upgrade.

## Hulpmiddel
- De spike-tool `Spike` (FlaUI-dump van een browservenster, modes dump|probe|invoke|mouse|bench, args `[mode] [titelfilter] [chrome|firefox]`) stond op de eerste pc in `%TEMP%\ytskip\spike` en zit niet in de repo. Handig om nieuwe YouTube-markup te bekijken: maak hem opnieuw als dat nodig is (FlaUI.UIA3, `FindAllDescendants` op het browservenster, AutomationId/ClassName/Name per element printen).
