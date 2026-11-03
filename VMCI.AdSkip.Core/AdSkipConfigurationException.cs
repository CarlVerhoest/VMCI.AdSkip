namespace VMCI.AdSkip.Core;

/// <summary>Thrown when appsettings.json is missing, unreadable or contains invalid settings.</summary>
public sealed class AdSkipConfigurationException(string message, Exception? innerException = null)
    : Exception(message, innerException);
