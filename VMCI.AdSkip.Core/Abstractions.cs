namespace VMCI.AdSkip.Core;

/// <summary>Supplies the top-level browser windows to scan.</summary>
public interface IBrowserWindowSource
{
    IReadOnlyList<IBrowserWindow> GetWindows(IReadOnlyCollection<string> processNames);
}

/// <summary>A top-level browser window, as seen through UI Automation.</summary>
public interface IBrowserWindow
{
    nint Handle { get; }
    string Title { get; }

    /// <summary>True when the browser exposes web page content (its accessibility tree is switched on).</summary>
    bool HasWebContent();

    IReadOnlyList<IVideoPlayer> FindPlayers(string playerAutomationId);
}

/// <summary>A YouTube player element inside a web page.</summary>
public interface IVideoPlayer
{
    /// <summary>The player's CSS classes, space separated.</summary>
    string ClassName { get; }

    IReadOnlyList<IUiButton> FindButtons();

    /// <summary>Accessible name of the "visit advertiser" link, e.g. "baloise.be This link opens in new tab".</summary>
    string? FindAdvertiserLinkName();
}

/// <summary>A button inside a player.</summary>
public interface IUiButton
{
    string Name { get; }

    /// <summary>The button's CSS classes, space separated.</summary>
    string ClassName { get; }

    /// <summary>True when the button has a non-empty on-screen rectangle.</summary>
    bool IsVisible { get; }

    /// <summary>Presses the button through UI Automation; false when the button does not support it.</summary>
    bool TryInvoke();

    /// <summary>Presses the button with a real mouse click.</summary>
    void Click();
}

public interface IAdSkipLog
{
    void Info(string message);
    void Warn(string message);
}
