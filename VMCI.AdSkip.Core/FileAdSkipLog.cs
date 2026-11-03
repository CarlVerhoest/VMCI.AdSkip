using System.Globalization;

namespace VMCI.AdSkip.Core;

/// <summary>Appends log lines to one file per day (adskip-yyyy-MM-dd.log) and deletes files older than the retention period.</summary>
public sealed class FileAdSkipLog : IAdSkipLog
{
    public const string FilePrefix = "adskip-";
    public const string FileExtension = ".log";

    private readonly string _directory;
    private readonly TimeProvider _timeProvider;
    private readonly int _retentionDays;
    private readonly Lock _lock = new();
    private DateOnly? _cleanedUpOn;

    public FileAdSkipLog(string directory, TimeProvider timeProvider, int retentionDays = 7)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(retentionDays, 1);
        _directory = directory;
        _timeProvider = timeProvider;
        _retentionDays = retentionDays;
    }

    /// <summary>Default log folder: %LOCALAPPDATA%\VMCI\AdSkip\logs.</summary>
    public static string DefaultDirectory =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "VMCI", "AdSkip", "logs");

    public string Directory => _directory;

    /// <summary>The file today's lines are written to (it may not exist yet).</summary>
    public string CurrentFilePath => PathFor(Today());

    public void Info(string message) => Write("INFO", message);
    public void Warn(string message) => Write("WARN", message);
    public void Error(string message) => Write("ERROR", message);

    private void Write(string level, string message)
    {
        var now = _timeProvider.GetLocalNow();
        var line = $"{now:yyyy-MM-dd HH:mm:ss} {level,-5} {message}{Environment.NewLine}";
        lock (_lock)
        {
            System.IO.Directory.CreateDirectory(_directory);
            File.AppendAllText(PathFor(DateOnly.FromDateTime(now.DateTime)), line);
            CleanUpOncePerDay(DateOnly.FromDateTime(now.DateTime));
        }
    }

    private void CleanUpOncePerDay(DateOnly today)
    {
        if (_cleanedUpOn == today) return;
        _cleanedUpOn = today;

        var oldest = today.AddDays(-(_retentionDays - 1));
        foreach (var file in System.IO.Directory.EnumerateFiles(_directory, $"{FilePrefix}*{FileExtension}"))
        {
            var datePart = Path.GetFileNameWithoutExtension(file)[FilePrefix.Length..];
            if (DateOnly.TryParseExact(datePart, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
                && date < oldest)
            {
                try
                {
                    File.Delete(file);
                }
                catch (IOException)
                {
                    // Open in an editor: try again tomorrow.
                }
            }
        }
    }

    private DateOnly Today() => DateOnly.FromDateTime(_timeProvider.GetLocalNow().DateTime);

    private string PathFor(DateOnly date) =>
        Path.Combine(_directory, $"{FilePrefix}{date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}{FileExtension}");
}
