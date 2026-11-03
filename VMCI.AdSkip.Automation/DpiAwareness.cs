using System.Runtime.InteropServices;

namespace VMCI.AdSkip.Automation;

public static class DpiAwareness
{
    private const nint PerMonitorAwareV2 = -4;

    /// <summary>Makes UI Automation coordinates match mouse coordinates on scaled and multi-monitor setups.</summary>
    public static void EnablePerMonitorV2() => SetProcessDpiAwarenessContext(PerMonitorAwareV2);

    [DllImport("user32.dll")]
    private static extern bool SetProcessDpiAwarenessContext(nint value);
}
