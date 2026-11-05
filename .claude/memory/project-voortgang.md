---
name: project-voortgang
description: Logboek van voortgang, open punten en nog niet geteste zaken in VMCI.AdSkip
metadata:
  type: project
---

## Gedaan
- 2026-11-03: Haalbaarheidsstudie en spike met UI Automation in Chrome: Skip-knop gevonden via CSS-klasse en ingedrukt met Invoke.
- 2026-11-03: Solution opgezet (Core, Automation, console `AdSkip.exe`, unit tests) en gepusht (`63f4158`).
- 2026-11-03: Firefox-ondersteuning (`Browsers`-instelling met procesnaam en vensterklasse, `8a99fef`). Een echte Firefox-advertentie werd door AdSkip zelf overgeslagen.
- 2026-11-03: Gepauzeerde advertenties tellen niet mee voor de waarschuwing (`PausedClass`, `6a1814e`).
- 2026-11-03: Tray-app `AdSkipTray.exe` met dagelijks logbestand en "Start with Windows" (`f31cd35`). Op de eerste pc is de Run-sleutel `VMCI.AdSkip` op vraag van Carl rechtstreeks gezet.
- 2026-11-05: Claude-geheugen verhuisd naar `.claude/memory/` in de repo, met `CLAUDE.md` en `scripts/setup-claude-memory.ps1`.
- 2026-11-05: Oude branches `feature/initial-solution` en `feature/firefox-support` verwijderd (lokaal en op GitHub); alleen `main` blijft.
- 2026-11-05: Tweede pc ingericht: geheugen-junction aangemaakt, .NET SDK 10.0.403 geïnstalleerd (naast 11.0 RC 3; `global.json` pint geen SDK), build + 98 tests groen, `publish.ps1` uitgevoerd en `publish\AdSkipTray.exe` gestart (draait vanuit de publish-map, op vraag van Carl geen Program Files).

## Nog niet getest
- Automatisch starten bij het aanmelden (Run-sleutel bestaat, maar nog niet gezien na afmelden/aanmelden).
- Tray-menu door Carl zelf (Pause/Resume, logs openen, Start with Windows-vinkje).
- Nederlandse YouTube-interface ("Overslaan"), schermvullend, venster op de achtergrond of geminimaliseerd, verse Chrome-start zonder `--force-renderer-accessibility`, de muis-terugval.
- De tray-app op de tweede pc (2026-11-05): Carl meldt "it runs fine"; "Start with Windows" was bij de commit nog niet aangevinkt (geen Run-sleutel).

## Open / mogelijk later
- Automatische updates (Velopack + GitHub Releases) als handmatig kopiëren lastig wordt.
