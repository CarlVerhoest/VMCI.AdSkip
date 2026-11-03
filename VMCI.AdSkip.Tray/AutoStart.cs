using Microsoft.Win32;

namespace VMCI.AdSkip.Tray;

/// <summary>Starts AdSkip at login through the current user's Run key (no admin rights needed).</summary>
internal static class AutoStart
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "VMCI.AdSkip";

    private static string Command => $"\"{Environment.ProcessPath}\"";

    public static bool IsEnabled
    {
        get
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath);
            return key?.GetValue(ValueName) is string;
        }
    }

    public static void Enable()
    {
        using var key = Registry.CurrentUser.CreateSubKey(RunKeyPath);
        key.SetValue(ValueName, Command);
    }

    public static void Disable()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true);
        key?.DeleteValue(ValueName, throwOnMissingValue: false);
    }

    /// <summary>When the AdSkip folder was moved or copied to a new place, point the Run entry at this exe.</summary>
    public static void UpdatePathIfEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true);
        if (key?.GetValue(ValueName) is string command && !string.Equals(command, Command, StringComparison.OrdinalIgnoreCase))
            key.SetValue(ValueName, Command);
    }
}
