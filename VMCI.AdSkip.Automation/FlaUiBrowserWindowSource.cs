using System.Diagnostics;
using FlaUI.UIA3;
using VMCI.AdSkip.Core;

namespace VMCI.AdSkip.Automation;

/// <summary>Finds Chromium browser windows through UI Automation (UIA3).</summary>
public sealed class FlaUiBrowserWindowSource : IBrowserWindowSource, IDisposable
{
    // Window class of every Chromium-based app; Electron apps (Slack, Claude, ...) use it too, hence the process filter.
    private const string ChromiumWindowClass = "Chrome_WidgetWin_1";

    private readonly UIA3Automation _automation = new();

    public IReadOnlyList<IBrowserWindow> GetWindows(IReadOnlyCollection<string> processNames)
    {
        var processIds = processNames.SelectMany(GetProcessIds).ToHashSet();
        return _automation.GetDesktop()
            .FindAllChildren(cf => cf.ByClassName(ChromiumWindowClass))
            .Where(w => processIds.Contains(w.Properties.ProcessId.ValueOrDefault))
            .Select(IBrowserWindow (w) => new FlaUiBrowserWindow(w))
            .ToList();
    }

    public void Dispose() => _automation.Dispose();

    private static int[] GetProcessIds(string processName)
    {
        var processes = Process.GetProcessesByName(processName);
        try
        {
            return processes.Select(p => p.Id).ToArray();
        }
        finally
        {
            foreach (var process in processes) process.Dispose();
        }
    }
}
