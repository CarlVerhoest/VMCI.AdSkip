using VMCI.AdSkip;
using VMCI.AdSkip.Automation;
using VMCI.AdSkip.Core;

if (args.Any(a => a is "-h" or "--help" or "/?"))
{
    Console.WriteLine("""
        AdSkip - presses YouTube's "Skip" button in Chrome for you.

        Usage: AdSkip [--help]

        Runs until you press Ctrl+C. Settings (process names, matching patterns, timings)
        are read from appsettings.json next to AdSkip.exe.

        If ads are not detected, start Chrome with --force-renderer-accessibility.
        """);
    return 0;
}

var log = new ConsoleLog();

AdSkipSettings settings;
try
{
    settings = AdSkipSettingsLoader.Load(AppContext.BaseDirectory);
}
catch (AdSkipConfigurationException ex)
{
    log.Error(ex.Message);
    return 1;
}

DpiAwareness.EnablePerMonitorV2();

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

using var windowSource = new FlaUiBrowserWindowSource();
var skipper = new AdSkipper(windowSource, settings, log, TimeProvider.System);
await new AdSkipRunner(skipper, settings, log, TimeProvider.System).RunAsync(cts.Token);
return 0;
