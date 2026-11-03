using Microsoft.Extensions.Time.Testing;
using VMCI.AdSkip.Core;
using VMCI.AdSkip.Tests.Unit.Fakes;

namespace VMCI.AdSkip.Tests.Unit;

public class AdSkipRunnerTests
{
    private static readonly TimeSpan WaitTimeout = TimeSpan.FromSeconds(5);

    private readonly FakeTimeProvider _time = new();
    private readonly ListLog _log = new();
    private readonly AdSkipSettings _settings = TestData.Settings();

    [Fact]
    public async Task Polls_at_the_configured_interval()
    {
        var skipper = new FakeSkipper();
        using var cts = new CancellationTokenSource();
        var run = new AdSkipRunner(skipper, _settings, _log, _time).RunAsync(cts.Token);

        WaitFor(() => skipper.TickCount == 1);
        _time.Advance(TimeSpan.FromMilliseconds(499));
        Assert.Equal(1, skipper.TickCount);

        _time.Advance(TimeSpan.FromMilliseconds(1));
        WaitFor(() => skipper.TickCount == 2);

        await cts.CancelAsync();
        await run;
    }

    [Fact]
    public async Task Waits_for_the_cooldown_after_a_skip()
    {
        var skipper = new FakeSkipper { SkipOnTick = 1 };
        using var cts = new CancellationTokenSource();
        var run = new AdSkipRunner(skipper, _settings, _log, _time).RunAsync(cts.Token);

        WaitFor(() => skipper.TickCount == 1);
        _time.Advance(TimeSpan.FromMilliseconds(1499));
        Assert.Equal(1, skipper.TickCount);

        _time.Advance(TimeSpan.FromMilliseconds(1));
        WaitFor(() => skipper.TickCount == 2);

        await cts.CancelAsync();
        await run;
    }

    [Fact]
    public async Task Cancellation_stops_and_reports_the_count()
    {
        var skipper = new FakeSkipper { SkippedCount = 3 };
        using var cts = new CancellationTokenSource();
        var run = new AdSkipRunner(skipper, _settings, _log, _time).RunAsync(cts.Token);

        WaitFor(() => skipper.TickCount == 1);
        await cts.CancelAsync();

        Assert.Equal(3, await run.WaitAsync(WaitTimeout, TestContext.Current.CancellationToken));
        Assert.Equal("Stopped. Skipped 3 ad(s) this session.", _log.Infos[^1]);
    }

    [Fact]
    public async Task Already_cancelled_token_never_ticks()
    {
        var skipper = new FakeSkipper();

        await new AdSkipRunner(skipper, _settings, _log, _time).RunAsync(new CancellationToken(canceled: true));

        Assert.Equal(0, skipper.TickCount);
        Assert.Equal(2, _log.Infos.Count);
    }

    [Fact]
    public async Task Warns_when_no_web_content_is_visible()
    {
        var skipper = new FakeSkipper { WebContent = false };

        await new AdSkipRunner(skipper, _settings, _log, _time).RunAsync(new CancellationToken(canceled: true));

        Assert.Contains("--force-renderer-accessibility", Assert.Single(_log.Warnings));
    }

    [Fact]
    public async Task Startup_message_names_processes_and_interval()
    {
        _settings.ProcessNames = ["chrome", "brave"];

        await new AdSkipRunner(new FakeSkipper(), _settings, _log, _time).RunAsync(new CancellationToken(canceled: true));

        Assert.Equal("AdSkip watching chrome, brave every 500 ms. Press Ctrl+C to stop.", _log.Infos[0]);
        Assert.Empty(_log.Warnings);
    }

    private static void WaitFor(Func<bool> condition) =>
        Assert.True(SpinWait.SpinUntil(condition, WaitTimeout), "Condition not reached in time.");

    private sealed class FakeSkipper : IAdSkipper
    {
        private int _tickCount;

        public int SkippedCount { get; set; }
        public bool WebContent { get; set; } = true;
        public int SkipOnTick { get; set; } = -1;
        public int TickCount => Volatile.Read(ref _tickCount);

        public bool HasWebContent() => WebContent;

        public bool Tick() => Interlocked.Increment(ref _tickCount) == SkipOnTick;
    }
}
