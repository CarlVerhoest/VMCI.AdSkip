namespace VMCI.AdSkip.Core;

public interface IAdSkipper
{
    int SkippedCount { get; }

    /// <summary>True when at least one browser window exposes web content.</summary>
    bool HasWebContent();

    /// <summary>One scan over all browser windows. Returns true if an ad was skipped.</summary>
    bool Tick();
}

/// <summary>Finds YouTube players in browser windows and presses their skip button.</summary>
public sealed class AdSkipper : IAdSkipper
{
    private readonly IBrowserWindowSource _windowSource;
    private readonly AdSkipSettings _settings;
    private readonly IAdSkipLog _log;
    private readonly SkipButtonMatcher _matcher;
    private readonly AdTracker _tracker;

    // Errors seen in the previous scan; UIA errors are common (tabs switch, ads end) so each is only reported when new.
    private HashSet<string> _previousErrors = [];

    public AdSkipper(IBrowserWindowSource windowSource, AdSkipSettings settings, IAdSkipLog log, TimeProvider timeProvider)
    {
        _windowSource = windowSource;
        _settings = settings;
        _log = log;
        _matcher = new SkipButtonMatcher(settings);
        _tracker = new AdTracker(timeProvider, TimeSpan.FromSeconds(settings.NoSkipWarningSeconds));
    }

    public int SkippedCount { get; private set; }

    public bool HasWebContent()
    {
        foreach (var window in _windowSource.GetWindows(_settings.Browsers))
        {
            try
            {
                if (window.HasWebContent()) return true;
            }
            catch (Exception)
            {
                // A window that cannot be inspected simply does not count.
            }
        }
        return false;
    }

    public bool Tick()
    {
        var skipped = false;
        var windowsWithAd = new HashSet<nint>();
        var errors = new HashSet<string>();

        try
        {
            foreach (var window in _windowSource.GetWindows(_settings.Browsers))
            {
                try
                {
                    skipped |= ScanWindow(window, windowsWithAd);
                }
                catch (Exception ex)
                {
                    ReportError(ex, errors);
                }
            }
        }
        catch (Exception ex)
        {
            ReportError(ex, errors);
        }

        _previousErrors = errors;
        _tracker.RetainOnly(windowsWithAd);
        return skipped;
    }

    private bool ScanWindow(IBrowserWindow window, HashSet<nint> windowsWithAd)
    {
        var skipped = false;
        foreach (var player in window.FindPlayers(_settings.PlayerAutomationId))
        {
            var adShowing = _matcher.IsAdShowing(player.ClassName);
            var button = player.FindButtons().FirstOrDefault(b => _matcher.IsSkipButton(b, adShowing));

            if (button is not null)
            {
                Press(button, player);
                _tracker.Clear(window.Handle);
                skipped = true;
            }
            else if (adShowing && !_matcher.IsPaused(player.ClassName))
            {
                // A paused ad freezes YouTube's skip countdown, so only playing time counts towards the warning.
                windowsWithAd.Add(window.Handle);
                ReportAdWithoutSkipButton(window);
            }
        }
        return skipped;
    }

    private void Press(IUiButton button, IVideoPlayer player)
    {
        string method;
        if (button.TryInvoke())
        {
            method = "invoke";
        }
        else
        {
            button.Click();
            method = "mouse";
        }

        SkippedCount++;
        var advertiser = AdvertiserName.FromLinkName(TryFindAdvertiserLinkName(player));
        _log.Info($"Skipped ad #{SkippedCount}{(advertiser is null ? "" : $" ({advertiser})")} via {method}");
    }

    private void ReportAdWithoutSkipButton(IBrowserWindow window)
    {
        switch (_tracker.ReportAdWithoutSkipButton(window.Handle))
        {
            case AdTrackerEvent.AdStarted:
                _log.Info($"Ad playing in '{window.Title}', waiting for skip button...");
                break;
            case AdTrackerEvent.SkipButtonOverdue:
                _log.Warn($"Ad visible for {_settings.NoSkipWarningSeconds}s but no skip button found. " +
                          "Unskippable ad, or the patterns in appsettings.json are outdated.");
                break;
        }
    }

    private void ReportError(Exception ex, HashSet<string> errors)
    {
        if (errors.Add(ex.Message) && !_previousErrors.Contains(ex.Message))
            _log.Warn($"UI Automation error: {ex.Message}");
    }

    private static string? TryFindAdvertiserLinkName(IVideoPlayer player)
    {
        try
        {
            return player.FindAdvertiserLinkName();
        }
        catch (Exception)
        {
            return null; // Only used for logging; never let it fail a skip.
        }
    }
}
