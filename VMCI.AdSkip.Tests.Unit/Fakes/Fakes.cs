using VMCI.AdSkip.Core;

namespace VMCI.AdSkip.Tests.Unit.Fakes;

internal static class TestData
{
    public const string PlayerWithAd = "html5-video-player ytp-hide-controls ad-created ad-showing ad-interrupting paused-mode";
    public const string PlayerWithoutAd = "html5-video-player ytp-hide-controls ad-created paused-mode";
    public const string SkipButtonClass = "ytp-ad-skip-button-modern ytp-button ";

    /// <summary>Same values as the shipped appsettings.json.</summary>
    public static AdSkipSettings Settings() => new()
    {
        ProcessNames = ["chrome"],
        SkipButtonClassPatterns = ["ytp-ad-skip-button", "ytp-skip-ad-button"],
        SkipButtonNamePatterns = [@"^\s*skip", @"^\s*overslaan"],
    };
}

internal sealed class FakeButton(string name = "Skip", string className = TestData.SkipButtonClass, bool isVisible = true, bool supportsInvoke = true)
    : IUiButton
{
    public string Name => name;
    public string ClassName => className;
    public bool IsVisible => isVisible;
    public int InvokeCount { get; private set; }
    public int ClickCount { get; private set; }

    public bool TryInvoke()
    {
        if (!supportsInvoke) return false;
        InvokeCount++;
        return true;
    }

    public void Click() => ClickCount++;
}

internal sealed class FakePlayer(string className, params IUiButton[] buttons) : IVideoPlayer
{
    public string ClassName { get; set; } = className;
    public List<IUiButton> Buttons { get; } = [.. buttons];
    public string? AdvertiserLinkName { get; set; }
    public Exception? AdvertiserError { get; set; }

    public IReadOnlyList<IUiButton> FindButtons() => Buttons;

    public string? FindAdvertiserLinkName() => AdvertiserError is null ? AdvertiserLinkName : throw AdvertiserError;
}

internal sealed class FakeWindow(nint handle, string title = "LoL Esports | Vod - Google Chrome", params FakePlayer[] players)
    : IBrowserWindow
{
    public nint Handle => handle;
    public string Title => title;
    public List<FakePlayer> Players { get; } = [.. players];
    public bool WebContent { get; set; } = true;
    public Exception? Error { get; set; }
    public string? RequestedPlayerId { get; private set; }

    public bool HasWebContent() => Error is null ? WebContent : throw Error;

    public IReadOnlyList<IVideoPlayer> FindPlayers(string playerAutomationId)
    {
        RequestedPlayerId = playerAutomationId;
        return Error is null ? Players : throw Error;
    }
}

internal sealed class FakeWindowSource(params FakeWindow[] windows) : IBrowserWindowSource
{
    public List<FakeWindow> Windows { get; } = [.. windows];
    public Exception? Error { get; set; }
    public IReadOnlyCollection<string>? RequestedProcessNames { get; private set; }

    public IReadOnlyList<IBrowserWindow> GetWindows(IReadOnlyCollection<string> processNames)
    {
        RequestedProcessNames = processNames;
        return Error is null ? Windows : throw Error;
    }
}

internal sealed class ListLog : IAdSkipLog
{
    private readonly Lock _lock = new();
    private readonly List<string> _infos = [];
    private readonly List<string> _warnings = [];

    public IReadOnlyList<string> Infos { get { lock (_lock) return [.. _infos]; } }
    public IReadOnlyList<string> Warnings { get { lock (_lock) return [.. _warnings]; } }

    public void Info(string message) { lock (_lock) _infos.Add(message); }
    public void Warn(string message) { lock (_lock) _warnings.Add(message); }
}
