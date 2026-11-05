---
name: feedback-werkwijze
description: Hoe Carl wil dat er in deze repo gewerkt wordt (branches, commits, geheugen)
metadata:
  type: feedback
---

- **Werk rechtstreeks op `main`.** Op de vraag om een nieuwe branch koos Carl telkens "huidige branch"; een feature-branch werd zonder PR rechtstreeks gemerged ("no need for a pr, just merge it").
- **Commit en push alleen als Carl erom vraagt.** Hij vraagt het meestal zelf meteen na een geslaagde test ("commit en push").
- **Laat Carl eerst zelf testen** wat hij moet zien of aanklikken (tray-menu, echte advertentie) voordat je iets als klaar meldt; zeg duidelijk wat nog niet getest is.
- **Alle Claude-kennis staat in `.claude/memory/` van de repo** (2026-11-05, op vraag van Carl, omdat het project op meerdere pc's gebruikt wordt). Schrijf nieuwe herinneringen daar en houd [[project-voortgang]] bij.

**Waarom:** Carl werkt alleen aan deze repo en op meerdere pc's; PR's voegen niets toe, en een geheugen dat alleen lokaal staat gaat op de andere pc verloren.
**Hoe toepassen:** geen `propose_branch`-discussie forceren als hij main kiest; na een taak de voortgang bijwerken en committen samen met het werk.
