using Microsoft.Extensions.Configuration;

namespace VMCI.AdSkip.Core;

public static class AdSkipSettingsLoader
{
    public const string DefaultFileName = "appsettings.json";
    public const string SectionName = "AdSkip";

    /// <summary>Loads and validates the "AdSkip" section of a JSON settings file.</summary>
    /// <exception cref="AdSkipConfigurationException">The file is missing, unreadable or the settings are invalid.</exception>
    public static AdSkipSettings Load(string directory, string fileName = DefaultFileName)
    {
        var path = Path.Combine(directory, fileName);
        if (!File.Exists(path))
            throw new AdSkipConfigurationException($"Settings file not found: {path}");

        AdSkipSettings settings;
        try
        {
            settings = new ConfigurationBuilder()
                .SetBasePath(directory)
                .AddJsonFile(fileName, optional: false)
                .Build()
                .GetSection(SectionName)
                .Get<AdSkipSettings>() ?? new AdSkipSettings();
        }
        catch (Exception ex) when (ex is InvalidDataException or FormatException or InvalidOperationException)
        {
            throw new AdSkipConfigurationException($"Could not read {path}: {ex.Message}", ex);
        }

        var errors = settings.Validate();
        if (errors.Count > 0)
            throw new AdSkipConfigurationException($"Invalid settings in {path}:{Environment.NewLine} - {string.Join($"{Environment.NewLine} - ", errors)}");

        return settings;
    }
}
