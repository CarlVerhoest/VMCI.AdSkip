using Microsoft.Extensions.Time.Testing;
using VMCI.AdSkip.Core;

namespace VMCI.AdSkip.Tests.Unit;

public sealed class FileAdSkipLogTests : IDisposable
{
    private readonly string _directory = Path.Combine(Directory.CreateTempSubdirectory("VMCI.AdSkip.Tests-").FullName, "logs");
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 11, 3, 19, 30, 5, TimeSpan.Zero));

    public FileAdSkipLogTests() => _time.SetLocalTimeZone(TimeZoneInfo.Utc);

    public void Dispose() => Directory.Delete(Path.GetDirectoryName(_directory)!, recursive: true);

    [Fact]
    public void Writes_levels_with_timestamp_to_todays_file_and_creates_the_folder()
    {
        var log = new FileAdSkipLog(_directory, _time);

        log.Info("Skipped ad #1 (eneco.be) via invoke");
        log.Warn("Ad visible for 15s");
        log.Error("Invalid settings");

        Assert.Equal(Path.Combine(_directory, "adskip-2026-11-03.log"), log.CurrentFilePath);
        Assert.Equal(
            [
                "2026-11-03 19:30:05 INFO  Skipped ad #1 (eneco.be) via invoke",
                "2026-11-03 19:30:05 WARN  Ad visible for 15s",
                "2026-11-03 19:30:05 ERROR Invalid settings",
            ],
            File.ReadAllLines(log.CurrentFilePath));
    }

    [Fact]
    public void Starts_a_new_file_after_midnight()
    {
        var log = new FileAdSkipLog(_directory, _time);
        log.Info("before");

        _time.Advance(TimeSpan.FromHours(5));
        log.Info("after");

        Assert.Equal(["2026-11-03 19:30:05 INFO  before"], File.ReadAllLines(Path.Combine(_directory, "adskip-2026-11-03.log")));
        Assert.Equal(["2026-11-04 00:30:05 INFO  after"], File.ReadAllLines(Path.Combine(_directory, "adskip-2026-11-04.log")));
    }

    [Fact]
    public void Deletes_log_files_older_than_the_retention_period_only()
    {
        Directory.CreateDirectory(_directory);
        string[] old = ["adskip-2026-10-01.log", "adskip-2026-10-27.log"];
        string[] kept = ["adskip-2026-10-28.log", "adskip-2026-11-02.log", "notes.txt", "adskip-latest.log"];
        foreach (var name in old.Concat(kept)) File.WriteAllText(Path.Combine(_directory, name), "x");

        new FileAdSkipLog(_directory, _time, retentionDays: 7).Info("hello");

        Assert.All(old, name => Assert.False(File.Exists(Path.Combine(_directory, name)), name));
        Assert.All(kept, name => Assert.True(File.Exists(Path.Combine(_directory, name)), name));
    }

    [Fact]
    public void Cleans_up_again_on_a_new_day()
    {
        var log = new FileAdSkipLog(_directory, _time, retentionDays: 1);
        log.Info("day 1");

        _time.Advance(TimeSpan.FromDays(1));
        log.Info("day 2");

        Assert.False(File.Exists(Path.Combine(_directory, "adskip-2026-11-03.log")));
        Assert.True(File.Exists(Path.Combine(_directory, "adskip-2026-11-04.log")));
    }

    [Fact]
    public void Concurrent_writes_are_not_lost()
    {
        var log = new FileAdSkipLog(_directory, _time);

        Parallel.For(0, 200, i => log.Info($"line {i}"));

        Assert.Equal(200, File.ReadAllLines(log.CurrentFilePath).Length);
    }

    [Fact]
    public void Retention_must_be_at_least_one_day() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new FileAdSkipLog(_directory, _time, retentionDays: 0));

    [Fact]
    public void Default_directory_is_under_local_app_data() =>
        Assert.EndsWith(Path.Combine("VMCI", "AdSkip", "logs"), FileAdSkipLog.DefaultDirectory);
}
