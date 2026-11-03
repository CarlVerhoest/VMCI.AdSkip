namespace VMCI.AdSkip.Core;

/// <summary>A browser to scan: its process name and the window class of its top-level windows.</summary>
public sealed class BrowserSettings
{
    /// <summary>Process name without .exe, e.g. "chrome" or "firefox".</summary>
    public string ProcessName { get; set; } = "";

    /// <summary>Win32 class of the browser's top-level windows, e.g. "Chrome_WidgetWin_1" or "MozillaWindowClass".</summary>
    public string WindowClass { get; set; } = "";

    public override string ToString() => ProcessName;
}
