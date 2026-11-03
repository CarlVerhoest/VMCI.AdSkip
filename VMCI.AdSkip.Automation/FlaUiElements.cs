using System.Drawing;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.Core.Input;
using VMCI.AdSkip.Core;

namespace VMCI.AdSkip.Automation;

internal sealed class FlaUiBrowserWindow(AutomationElement element) : IBrowserWindow
{
    public nint Handle => element.Properties.NativeWindowHandle.ValueOrDefault;

    public string Title => element.Properties.Name.ValueOrDefault ?? "";

    public bool HasWebContent() =>
        element.FindFirstDescendant(cf => cf.ByControlType(ControlType.Document)) is not null;

    public IReadOnlyList<IVideoPlayer> FindPlayers(string playerAutomationId) =>
        element.FindAllDescendants(cf => cf.ByAutomationId(playerAutomationId))
            .Select(IVideoPlayer (e) => new FlaUiVideoPlayer(e))
            .ToList();
}

internal sealed class FlaUiVideoPlayer(AutomationElement element) : IVideoPlayer
{
    private const string VisitAdvertiserClass = "visit-advertiser";

    public string ClassName => element.Properties.ClassName.ValueOrDefault ?? "";

    public IReadOnlyList<IUiButton> FindButtons() =>
        element.FindAllDescendants(cf => cf.ByControlType(ControlType.Button))
            .Select(IUiButton (e) => new FlaUiButton(e))
            .ToList();

    public string? FindAdvertiserLinkName() =>
        element.FindAllDescendants(cf => cf.ByControlType(ControlType.Hyperlink))
            .FirstOrDefault(e => (e.Properties.ClassName.ValueOrDefault ?? "").Contains(VisitAdvertiserClass, StringComparison.OrdinalIgnoreCase))
            ?.Properties.Name.ValueOrDefault;
}

internal sealed class FlaUiButton(AutomationElement element) : IUiButton
{
    public string Name => element.Properties.Name.ValueOrDefault ?? "";

    public string ClassName => element.Properties.ClassName.ValueOrDefault ?? "";

    public bool IsVisible
    {
        get
        {
            var rect = element.Properties.BoundingRectangle.ValueOrDefault;
            return rect.Width > 0 && rect.Height > 0 && !element.Properties.IsOffscreen.ValueOrDefault;
        }
    }

    public bool TryInvoke()
    {
        if (!element.Patterns.Invoke.TryGetPattern(out var invoke)) return false;
        invoke.Invoke();
        return true;
    }

    public void Click()
    {
        // Real click on the button centre, then put the cursor back where the user left it.
        var previous = Mouse.Position;
        var rect = element.BoundingRectangle;
        Mouse.Click(new Point(rect.X + rect.Width / 2, rect.Y + rect.Height / 2));
        Mouse.Position = previous;
    }
}
