namespace VMCI.AdSkip.Core;

/// <summary>Runs <see cref="IAdSkipper.Tick"/> in a loop until cancelled.</summary>
public sealed class AdSkipRunner(IAdSkipper skipper, AdSkipSettings settings, IAdSkipLog log, TimeProvider timeProvider)
{
    /// <summary>Returns the number of ads skipped before cancellation.</summary>
    public async Task<int> RunAsync(CancellationToken cancellationToken)
    {
        log.Info($"AdSkip watching {string.Join(", ", settings.Browsers.Select(b => b.ProcessName))} every {settings.PollIntervalMs} ms.");
        if (!skipper.HasWebContent())
            log.Warn("No web content visible in any browser window. If ads are not skipped, start Chrome with --force-renderer-accessibility.");

        while (!cancellationToken.IsCancellationRequested)
        {
            var delayMs = skipper.Tick() ? settings.CooldownAfterSkipMs : settings.PollIntervalMs;
            try
            {
                await Task.Delay(TimeSpan.FromMilliseconds(delayMs), timeProvider, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        log.Info($"Stopped. Skipped {skipper.SkippedCount} ad(s) this session.");
        return skipper.SkippedCount;
    }
}
