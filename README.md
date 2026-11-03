# VMCI.AdSkip

Windows app that presses YouTube's **Skip** button in Chrome or Firefox for you, so you don't have to walk to the
keyboard during a workout. It does not block ads: it waits until YouTube offers the Skip button, then presses it.

## How it works

Chrome and Firefox expose web pages to Windows UI Automation (the API screen readers use). Every 500 ms AdSkip:

1. finds the `chrome.exe` and `firefox.exe` windows (by window class and process: Electron apps such as Slack share Chrome's window class, so they are filtered out);
2. looks for YouTube players (HTML id `movie_player`), also inside embedded players on other sites;
3. presses a button inside the player whose CSS class matches `ytp-ad-skip-button…`. As a fallback, while
   the player has the `ad-showing` class, a button named "Skip" or "Overslaan";
4. presses it through UI Automation (`Invoke`, the mouse does not move), or with a real mouse click if that is not supported.

When an ad plays for 15 seconds without a skip button being found (paused time does not count, since YouTube's
skip countdown stops while paused), a warning is logged (and shown as a notification by the tray app): either the ad
is unskippable, or YouTube changed its markup and the patterns in `appsettings.json` need an update.

## Usage

`.\publish.ps1` builds two apps into `publish\`, sharing one `appsettings.json`:

| App | What | Needs |
|---|---|---|
| `AdSkipTray.exe` | Runs in the notification area (bottom right). The everyday app. | Nothing: the .NET runtime is included |
| `AdSkip.exe` | Console version, runs until Ctrl+C. Handy for testing. | .NET 10 Desktop Runtime |

### Tray app

Click or right-click the icon (red: active, grey: paused):

- **Pause / Resume**: Resume also reloads `appsettings.json`
- **Open today's log** / **Open log folder**
- **Open settings**: opens `appsettings.json`; Pause and Resume to apply changes
- **Start with Windows**: starts AdSkip when you log in (current user's `Run` registry key, no admin rights needed)
- **Exit**

Warnings, such as an ad without a skip button, also pop up as a notification (at most one per minute).
Only one AdSkip runs per user session.

AdSkip runs at **login, not at system boot**, and is not a Windows service on purpose: services run in a separate
session without a desktop, where they cannot see your browser windows or show a tray icon.

### Install on another machine

1. Copy `AdSkipTray.exe` and `appsettings.json` to a folder of your choice, e.g. `%LOCALAPPDATA%\Programs\AdSkip`.
2. Start `AdSkipTray.exe` and tick **Start with Windows**.

To update, Exit AdSkip, overwrite the files and start it again. If you move the folder, the
**Start with Windows** entry follows the new location the next time AdSkip is started from there.

### Logs

The tray app logs to `%LOCALAPPDATA%\VMCI\AdSkip\logs\adskip-yyyy-MM-dd.log` and keeps 7 days.

### Console app

```powershell
.\publish\AdSkip.exe          # runs until Ctrl+C
```

Add the `publish` folder to your `PATH` to run `AdSkip` from any PowerShell window.

Example output:

```
16:24:10 AdSkip watching chrome, firefox every 500 ms.
16:24:10 Press Ctrl+C to stop.
16:24:12 Ad playing in 'LoL Esports | Vod - Google Chrome', waiting for skip button...
16:24:25 Skipped ad #1 (baloise.be) via invoke
```

If ads are never detected, Chrome is not exposing web content: start Chrome with
`--force-renderer-accessibility` (for example in its shortcut). Firefox needs no extra settings.

## Settings

`appsettings.json` next to `AdSkip.exe`, section `AdSkip`:

| Setting | Default | Meaning |
|---|---|---|
| `PollIntervalMs` | 500 | Delay between scans |
| `CooldownAfterSkipMs` | 1500 | Delay after a skip |
| `Browsers` | `chrome` (`Chrome_WidgetWin_1`), `firefox` (`MozillaWindowClass`) | Browsers to scan: `ProcessName` (without .exe) and the `WindowClass` of its top-level windows |
| `PlayerAutomationId` | `movie_player` | HTML id of the YouTube player |
| `AdShowingClass` | `ad-showing` | Player CSS class while an ad plays |
| `PausedClass` | `paused-mode` | Player CSS class while paused; a paused ad does not count towards the warning |
| `SkipButtonClassPatterns` | `ytp-ad-skip-button`, `ytp-skip-ad-button` | Regexes on the button's CSS classes |
| `SkipButtonNamePatterns` | `^\s*skip`, `^\s*overslaan` | Fallback regexes on the button's name, only while an ad shows |
| `NoSkipWarningSeconds` | 15 | Warn when no skip button shows up within this time |

## Solution

- **VMCI.AdSkip**: console app, builds `AdSkip.exe`
- **VMCI.AdSkip.Tray**: tray app (Windows Forms), builds `AdSkipTray.exe`
- **VMCI.AdSkip.Core**: all logic (matching, ad tracking, loop, settings, file log) behind small interfaces
- **VMCI.AdSkip.Automation**: UI Automation implementation of those interfaces (FlaUI, UIA3)
- **VMCI.AdSkip.Tests.Unit**: unit tests for Core (xUnit v3 on Microsoft.Testing.Platform)

```powershell
dotnet build
dotnet test
dotnet test --coverage --coverage-output-format cobertura   # code coverage
```

Requires the .NET 10 SDK. In Visual Studio Enterprise, use Test > Analyze Code Coverage for All Tests.
