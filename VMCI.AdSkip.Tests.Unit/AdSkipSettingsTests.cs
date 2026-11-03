using VMCI.AdSkip.Core;
using VMCI.AdSkip.Tests.Unit.Fakes;

namespace VMCI.AdSkip.Tests.Unit;

public class AdSkipSettingsTests
{
    [Fact]
    public void Test_settings_are_valid() => Assert.Empty(TestData.Settings().Validate());

    [Fact]
    public void Defaults_without_lists_are_invalid()
    {
        var errors = new AdSkipSettings().Validate();

        Assert.Contains(errors, e => e.Contains(nameof(AdSkipSettings.Browsers)));
        Assert.Contains(errors, e => e.Contains(nameof(AdSkipSettings.SkipButtonClassPatterns)));
    }

    public static TheoryData<string, Action<AdSkipSettings>> InvalidSettings => new()
    {
        { nameof(AdSkipSettings.PollIntervalMs), s => s.PollIntervalMs = 10 },
        { nameof(AdSkipSettings.PollIntervalMs), s => s.PollIntervalMs = 60_001 },
        { nameof(AdSkipSettings.CooldownAfterSkipMs), s => s.CooldownAfterSkipMs = -1 },
        { nameof(AdSkipSettings.CooldownAfterSkipMs), s => s.CooldownAfterSkipMs = 60_001 },
        { nameof(AdSkipSettings.NoSkipWarningSeconds), s => s.NoSkipWarningSeconds = 0 },
        { nameof(AdSkipSettings.Browsers), s => s.Browsers = [] },
        { nameof(BrowserSettings.ProcessName), s => s.Browsers = [TestData.Chrome, new() { ProcessName = " ", WindowClass = "MozillaWindowClass" }] },
        { nameof(BrowserSettings.WindowClass), s => s.Browsers = [TestData.Chrome, new() { ProcessName = "firefox" }] },
        { nameof(AdSkipSettings.PlayerAutomationId), s => s.PlayerAutomationId = "" },
        { nameof(AdSkipSettings.AdShowingClass), s => s.AdShowingClass = " " },
        { nameof(AdSkipSettings.SkipButtonClassPatterns), s => { s.SkipButtonClassPatterns = []; s.SkipButtonNamePatterns = []; } },
        { "Invalid regular expression: '(unclosed'", s => s.SkipButtonClassPatterns = ["(unclosed"] },
        { "Invalid regular expression: ''", s => s.SkipButtonNamePatterns = [""] },
    };

    [Theory]
    [MemberData(nameof(InvalidSettings))]
    public void Validate_reports_invalid_setting(string expectedInMessage, Action<AdSkipSettings> breakSettings)
    {
        var settings = TestData.Settings();
        breakSettings(settings);

        var error = Assert.Single(settings.Validate());
        Assert.Contains(expectedInMessage, error);
    }

    [Fact]
    public void Validate_reports_all_problems_at_once()
    {
        var settings = TestData.Settings();
        settings.PollIntervalMs = 0;
        settings.NoSkipWarningSeconds = 0;

        Assert.Equal(2, settings.Validate().Count);
    }
}
