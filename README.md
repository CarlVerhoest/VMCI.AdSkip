# VMCI.AdSkip

Console app that presses YouTube's **Skip** button in Chrome for you, so you don't have to walk to the
keyboard during a workout. It does not block ads: it waits until YouTube offers the Skip button, then presses it.

## How it works

Chrome exposes web pages to Windows UI Automation (the API screen readers use). Every 500 ms AdSkip:

1. finds the `chrome.exe` windows (Electron apps such as Slack share Chrome's window class, so they are filtered out);
2. looks for YouTube players (HTML id `movie_player`), also inside embedded players on other sites;
3. presses a button inside the player whose CSS class matches `ytp-ad-skip-button…`. As a fallback, while
   the player has the `ad-showing` class, a button named "Skip" or "Overslaan";
4. presses it through UI Automation (`Invoke`, the mouse does not move), or with a real mouse click if that is not supported.

When an ad shows for 15 seconds without a skip button being found, a yellow warning is printed: either the ad
is unskippable, or YouTube changed its markup and the patterns in `appsettings.json` need an update.

## Usage

```powershell
.\publish.ps1                 # builds publish\AdSkip.exe
.\publish\AdSkip.exe          # runs until Ctrl+C
```

Add the `publish` folder to your `PATH` to run `AdSkip` from any PowerShell window.

Example output:

```
16:24:10 AdSkip watching chrome every 500 ms. Press Ctrl+C to stop.
16:24:12 Ad playing in 'LoL Esports | Vod - Google Chrome', waiting for skip button...
16:24:25 Skipped ad #1 (baloise.be) via invoke
```

If ads are never detected, Chrome is not exposing web content: start Chrome with
`--force-renderer-accessibility` (for example in its shortcut).

## Settings

`appsettings.json` next to `AdSkip.exe`, section `AdSkip`:

| Setting | Default | Meaning |
|---|---|---|
| `PollIntervalMs` | 500 | Delay between scans |
| `CooldownAfterSkipMs` | 1500 | Delay after a skip |
| `ProcessNames` | `chrome` | Browser processes to scan |
| `PlayerAutomationId` | `movie_player` | HTML id of the YouTube player |
| `AdShowingClass` | `ad-showing` | Player CSS class while an ad plays |
| `SkipButtonClassPatterns` | `ytp-ad-skip-button`, `ytp-skip-ad-button` | Regexes on the button's CSS classes |
| `SkipButtonNamePatterns` | `^\s*skip`, `^\s*overslaan` | Fallback regexes on the button's name, only while an ad shows |
| `NoSkipWarningSeconds` | 15 | Warn when no skip button shows up within this time |

## Solution

- **VMCI.AdSkip**: console app, builds `AdSkip.exe`
- **VMCI.AdSkip.Core**: all logic (matching, ad tracking, loop, settings) behind small interfaces
- **VMCI.AdSkip.Automation**: UI Automation implementation of those interfaces (FlaUI, UIA3)
- **VMCI.AdSkip.Tests.Unit**: unit tests for Core (xUnit v3 on Microsoft.Testing.Platform)

```powershell
dotnet build
dotnet test
dotnet test --coverage --coverage-output-format cobertura   # code coverage
```

Requires the .NET 10 SDK. In Visual Studio Enterprise, use Test > Analyze Code Coverage for All Tests.
