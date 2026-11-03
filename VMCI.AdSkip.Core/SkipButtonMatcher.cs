using System.Text.RegularExpressions;

namespace VMCI.AdSkip.Core;

/// <summary>Decides whether a player shows an ad and whether a button is its skip button.</summary>
public sealed class SkipButtonMatcher
{
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromMilliseconds(100);

    private readonly Regex[] _classPatterns;
    private readonly Regex[] _namePatterns;
    private readonly string _adShowingClass;
    private readonly string _pausedClass;

    public SkipButtonMatcher(AdSkipSettings settings)
    {
        _classPatterns = settings.SkipButtonClassPatterns.Select(Compile).ToArray();
        _namePatterns = settings.SkipButtonNamePatterns.Select(Compile).ToArray();
        _adShowingClass = settings.AdShowingClass;
        _pausedClass = settings.PausedClass;
    }

    /// <summary>True when the ad-showing class is one of the player's CSS classes (whole class, not a substring).</summary>
    public bool IsAdShowing(string? playerClassName) => HasClass(playerClassName, _adShowingClass);

    /// <summary>True when the paused class is one of the player's CSS classes (whole class, not a substring).</summary>
    public bool IsPaused(string? playerClassName) => HasClass(playerClassName, _pausedClass);

    /// <summary>
    /// A class match is specific to ad skip buttons and always trusted; the name fallback ("Skip", "Overslaan")
    /// is only trusted while the player shows an ad, so other "skip"-like player buttons are never pressed.
    /// </summary>
    public bool IsSkipButton(IUiButton button, bool adShowing)
    {
        if (!button.IsVisible) return false;
        if (_classPatterns.Any(r => r.IsMatch(button.ClassName))) return true;
        return adShowing && _namePatterns.Any(r => r.IsMatch(button.Name));
    }

    private static bool HasClass(string? classNames, string className) =>
        (classNames ?? "")
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
            .Contains(className, StringComparer.OrdinalIgnoreCase);

    private static Regex Compile(string pattern) =>
        new(pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, RegexTimeout);
}
