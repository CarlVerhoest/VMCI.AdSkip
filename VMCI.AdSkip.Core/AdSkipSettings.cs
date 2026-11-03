using System.Text.RegularExpressions;

namespace VMCI.AdSkip.Core;

/// <summary>Settings bound from the "AdSkip" section of appsettings.json, so matchers can be fixed without a rebuild.</summary>
/// <remarks>Arrays default to empty on purpose: the configuration binder appends to non-empty defaults instead of replacing them.</remarks>
public sealed class AdSkipSettings
{
    /// <summary>Delay between two scans of the browser windows.</summary>
    public int PollIntervalMs { get; set; } = 500;

    /// <summary>Delay after a skip, so the player can settle (next ad, or the video itself).</summary>
    public int CooldownAfterSkipMs { get; set; } = 1500;

    /// <summary>Browsers whose windows are scanned.</summary>
    public BrowserSettings[] Browsers { get; set; } = [];

    /// <summary>AutomationId (HTML id) of the YouTube player; skip buttons are only searched inside it.</summary>
    public string PlayerAutomationId { get; set; } = "movie_player";

    /// <summary>CSS class YouTube puts on the player while an ad is playing.</summary>
    public string AdShowingClass { get; set; } = "ad-showing";

    /// <summary>Case-insensitive regexes matched against the button's CSS classes (language independent).</summary>
    public string[] SkipButtonClassPatterns { get; set; } = [];

    /// <summary>Fallback: case-insensitive regexes matched against the button's visible name, only while an ad is showing.</summary>
    public string[] SkipButtonNamePatterns { get; set; } = [];

    /// <summary>Warn when an ad has been showing this long without a skip button being found.</summary>
    public int NoSkipWarningSeconds { get; set; } = 15;

    /// <summary>Returns a human readable message for every invalid setting; empty when the settings are usable.</summary>
    public IReadOnlyList<string> Validate()
    {
        var errors = new List<string>();

        if (PollIntervalMs is < 50 or > 60_000)
            errors.Add($"{nameof(PollIntervalMs)} must be between 50 and 60000 (was {PollIntervalMs}).");
        if (CooldownAfterSkipMs is < 0 or > 60_000)
            errors.Add($"{nameof(CooldownAfterSkipMs)} must be between 0 and 60000 (was {CooldownAfterSkipMs}).");
        if (NoSkipWarningSeconds < 1)
            errors.Add($"{nameof(NoSkipWarningSeconds)} must be at least 1 (was {NoSkipWarningSeconds}).");
        if (Browsers.Length == 0)
            errors.Add($"{nameof(Browsers)} must contain at least one browser.");
        if (Browsers.Any(b => string.IsNullOrWhiteSpace(b.ProcessName) || string.IsNullOrWhiteSpace(b.WindowClass)))
            errors.Add($"Every entry in {nameof(Browsers)} needs a {nameof(BrowserSettings.ProcessName)} and a {nameof(BrowserSettings.WindowClass)}.");
        if (string.IsNullOrWhiteSpace(PlayerAutomationId))
            errors.Add($"{nameof(PlayerAutomationId)} must not be empty.");
        if (string.IsNullOrWhiteSpace(AdShowingClass))
            errors.Add($"{nameof(AdShowingClass)} must not be empty.");
        if (SkipButtonClassPatterns.Length == 0 && SkipButtonNamePatterns.Length == 0)
            errors.Add($"At least one of {nameof(SkipButtonClassPatterns)} or {nameof(SkipButtonNamePatterns)} is required.");

        foreach (var pattern in SkipButtonClassPatterns.Concat(SkipButtonNamePatterns))
        {
            if (!IsValidRegex(pattern))
                errors.Add($"Invalid regular expression: '{pattern}'.");
        }

        return errors;
    }

    private static bool IsValidRegex(string pattern)
    {
        if (string.IsNullOrEmpty(pattern)) return false;
        try
        {
            _ = new Regex(pattern);
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
}
