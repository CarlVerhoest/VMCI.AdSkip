using VMCI.AdSkip.Core;

namespace VMCI.AdSkip.Tests.Unit;

public sealed class AdSkipSettingsLoaderTests : IDisposable
{
    private readonly string _directory = Directory.CreateTempSubdirectory("VMCI.AdSkip.Tests-").FullName;

    public void Dispose() => Directory.Delete(_directory, recursive: true);

    [Fact]
    public void Shipped_appsettings_is_valid()
    {
        var settings = AdSkipSettingsLoader.Load(Path.Combine(AppContext.BaseDirectory, "TestData"));

        Assert.Equal(["chrome"], settings.ProcessNames);
        Assert.Contains("ytp-ad-skip-button", settings.SkipButtonClassPatterns);
        Assert.Contains(@"^\s*overslaan", settings.SkipButtonNamePatterns);
    }

    [Fact]
    public void Loads_all_values()
    {
        WriteSettings("""
            {
              "AdSkip": {
                "PollIntervalMs": 250,
                "CooldownAfterSkipMs": 2000,
                "ProcessNames": [ "chrome", "brave" ],
                "PlayerAutomationId": "player",
                "AdShowingClass": "ad-on",
                "SkipButtonClassPatterns": [ "skip-cls" ],
                "SkipButtonNamePatterns": [ "^next" ],
                "NoSkipWarningSeconds": 30
              }
            }
            """);

        var settings = AdSkipSettingsLoader.Load(_directory);

        Assert.Equal(250, settings.PollIntervalMs);
        Assert.Equal(2000, settings.CooldownAfterSkipMs);
        Assert.Equal(["chrome", "brave"], settings.ProcessNames);
        Assert.Equal("player", settings.PlayerAutomationId);
        Assert.Equal("ad-on", settings.AdShowingClass);
        Assert.Equal(["skip-cls"], settings.SkipButtonClassPatterns);
        Assert.Equal(["^next"], settings.SkipButtonNamePatterns);
        Assert.Equal(30, settings.NoSkipWarningSeconds);
    }

    [Fact]
    public void Lists_from_file_are_not_doubled()
    {
        // Regression: the configuration binder appends to non-empty default arrays.
        WriteSettings("""{ "AdSkip": { "ProcessNames": [ "chrome" ], "SkipButtonClassPatterns": [ "x" ] } }""");

        var settings = AdSkipSettingsLoader.Load(_directory);

        Assert.Equal(["chrome"], settings.ProcessNames);
        Assert.Equal(["x"], settings.SkipButtonClassPatterns);
    }

    [Fact]
    public void Missing_values_keep_their_defaults()
    {
        WriteSettings("""{ "AdSkip": { "ProcessNames": [ "chrome" ], "SkipButtonClassPatterns": [ "x" ] } }""");

        var settings = AdSkipSettingsLoader.Load(_directory);

        Assert.Equal(500, settings.PollIntervalMs);
        Assert.Equal("movie_player", settings.PlayerAutomationId);
        Assert.Equal("ad-showing", settings.AdShowingClass);
    }

    [Fact]
    public void Custom_file_name_is_supported()
    {
        WriteSettings("""{ "AdSkip": { "ProcessNames": [ "chrome" ], "SkipButtonClassPatterns": [ "x" ] } }""", "custom.json");

        Assert.Equal(["chrome"], AdSkipSettingsLoader.Load(_directory, "custom.json").ProcessNames);
    }

    [Fact]
    public void Missing_file_throws()
    {
        var ex = Assert.Throws<AdSkipConfigurationException>(() => AdSkipSettingsLoader.Load(_directory));
        Assert.Contains("not found", ex.Message);
    }

    [Fact]
    public void Malformed_json_throws()
    {
        WriteSettings("{ this is not json");

        var ex = Assert.Throws<AdSkipConfigurationException>(() => AdSkipSettingsLoader.Load(_directory));
        Assert.Contains("Could not read", ex.Message);
        Assert.NotNull(ex.InnerException);
    }

    [Fact]
    public void Wrong_value_type_throws()
    {
        WriteSettings("""{ "AdSkip": { "PollIntervalMs": "fast" } }""");

        Assert.Throws<AdSkipConfigurationException>(() => AdSkipSettingsLoader.Load(_directory));
    }

    [Fact]
    public void Missing_section_throws_with_validation_errors()
    {
        WriteSettings("""{ "Other": {} }""");

        var ex = Assert.Throws<AdSkipConfigurationException>(() => AdSkipSettingsLoader.Load(_directory));
        Assert.Contains("Invalid settings", ex.Message);
        Assert.Contains(nameof(AdSkipSettings.ProcessNames), ex.Message);
    }

    private void WriteSettings(string json, string fileName = AdSkipSettingsLoader.DefaultFileName) =>
        File.WriteAllText(Path.Combine(_directory, fileName), json);
}
