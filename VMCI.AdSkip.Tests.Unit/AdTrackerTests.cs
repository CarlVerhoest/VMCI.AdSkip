using Microsoft.Extensions.Time.Testing;
using VMCI.AdSkip.Core;

namespace VMCI.AdSkip.Tests.Unit;

public class AdTrackerTests
{
    private readonly FakeTimeProvider _time = new();
    private readonly AdTracker _tracker;

    public AdTrackerTests() => _tracker = new AdTracker(_time, TimeSpan.FromSeconds(15));

    [Fact]
    public void First_report_starts_the_ad()
    {
        Assert.Equal(AdTrackerEvent.AdStarted, _tracker.ReportAdWithoutSkipButton(1));
        Assert.True(_tracker.IsTracking(1));
    }

    [Fact]
    public void Reports_before_the_deadline_are_quiet()
    {
        _tracker.ReportAdWithoutSkipButton(1);
        _time.Advance(TimeSpan.FromSeconds(14.9));

        Assert.Equal(AdTrackerEvent.None, _tracker.ReportAdWithoutSkipButton(1));
    }

    [Fact]
    public void Warns_once_when_the_deadline_passes()
    {
        _tracker.ReportAdWithoutSkipButton(1);
        _time.Advance(TimeSpan.FromSeconds(15));

        Assert.Equal(AdTrackerEvent.SkipButtonOverdue, _tracker.ReportAdWithoutSkipButton(1));
        _time.Advance(TimeSpan.FromSeconds(30));
        Assert.Equal(AdTrackerEvent.None, _tracker.ReportAdWithoutSkipButton(1));
    }

    [Fact]
    public void Windows_are_tracked_independently()
    {
        _tracker.ReportAdWithoutSkipButton(1);
        _time.Advance(TimeSpan.FromSeconds(10));

        Assert.Equal(AdTrackerEvent.AdStarted, _tracker.ReportAdWithoutSkipButton(2));
        _time.Advance(TimeSpan.FromSeconds(5));
        Assert.Equal(AdTrackerEvent.SkipButtonOverdue, _tracker.ReportAdWithoutSkipButton(1));
        Assert.Equal(AdTrackerEvent.None, _tracker.ReportAdWithoutSkipButton(2));
    }

    [Fact]
    public void Clear_starts_a_new_ad_next_time()
    {
        _tracker.ReportAdWithoutSkipButton(1);
        _tracker.Clear(1);

        Assert.False(_tracker.IsTracking(1));
        Assert.Equal(AdTrackerEvent.AdStarted, _tracker.ReportAdWithoutSkipButton(1));
    }

    [Fact]
    public void RetainOnly_forgets_windows_without_ad()
    {
        _tracker.ReportAdWithoutSkipButton(1);
        _tracker.ReportAdWithoutSkipButton(2);

        _tracker.RetainOnly(new HashSet<nint> { 2 });

        Assert.False(_tracker.IsTracking(1));
        Assert.True(_tracker.IsTracking(2));
    }
}
