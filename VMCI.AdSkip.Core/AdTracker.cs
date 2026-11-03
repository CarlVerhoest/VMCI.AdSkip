namespace VMCI.AdSkip.Core;

public enum AdTrackerEvent
{
    None,
    AdStarted,
    SkipButtonOverdue,
}

/// <summary>Remembers, per browser window, since when an ad without skip button has been showing.</summary>
public sealed class AdTracker(TimeProvider timeProvider, TimeSpan warnAfter)
{
    private readonly Dictionary<nint, (DateTimeOffset Since, bool Warned)> _ads = [];

    /// <summary>
    /// Reports that the window shows an ad without skip button. Returns <see cref="AdTrackerEvent.AdStarted"/> the first
    /// time, <see cref="AdTrackerEvent.SkipButtonOverdue"/> once after <c>warnAfter</c>, and <see cref="AdTrackerEvent.None"/> otherwise.
    /// </summary>
    public AdTrackerEvent ReportAdWithoutSkipButton(nint window)
    {
        var now = timeProvider.GetUtcNow();
        if (!_ads.TryGetValue(window, out var ad))
        {
            _ads[window] = (now, false);
            return AdTrackerEvent.AdStarted;
        }

        if (ad.Warned || now - ad.Since < warnAfter) return AdTrackerEvent.None;

        _ads[window] = ad with { Warned = true };
        return AdTrackerEvent.SkipButtonOverdue;
    }

    public bool IsTracking(nint window) => _ads.ContainsKey(window);

    public void Clear(nint window) => _ads.Remove(window);

    /// <summary>Forgets every window that no longer shows an ad.</summary>
    public void RetainOnly(IReadOnlySet<nint> windowsWithAd)
    {
        foreach (var window in _ads.Keys.Where(w => !windowsWithAd.Contains(w)).ToList())
            _ads.Remove(window);
    }
}
