using System.Diagnostics;
using FlaUI.UIA3;
using VMCI.AdSkip.Core;

namespace VMCI.AdSkip.Automation;

/// <summary>Finds browser windows (Chrome, Firefox, ...) through UI Automation (UIA3).</summary>
public sealed class FlaUiBrowserWindowSource : IBrowserWindowSource, IDisposable
{
    private readonly UIA3Automation _automation = new();

    public IReadOnlyList<IBrowserWindow> GetWindows(IReadOnlyCollection<BrowserSettings> browsers)
    {
        var desktop = _automation.GetDesktop();
        var windows = new List<IBrowserWindow>();
        foreach (var browser in browsers)
        {
            // Window classes are shared (Electron apps such as Slack use Chrome's), hence the process filter.
            // Firefox also keeps hidden top-level windows without a title; those are skipped.
            var processIds = GetProcessIds(browser.ProcessName);
            windows.AddRange(desktop
                .FindAllChildren(cf => cf.ByClassName(browser.WindowClass))
                .Where(w => processIds.Contains(w.Properties.ProcessId.ValueOrDefault)
                            && !string.IsNullOrEmpty(w.Properties.Name.ValueOrDefault))
                .Select(IBrowserWindow (w) => new FlaUiBrowserWindow(w)));
        }
        return windows;
    }

    public void Dispose() => _automation.Dispose();

    private static HashSet<int> GetProcessIds(string processName)
    {
        var processes = Process.GetProcessesByName(processName);
        try
        {
            return processes.Select(p => p.Id).ToHashSet();
        }
        finally
        {
            foreach (var process in processes) process.Dispose();
        }
    }
}
