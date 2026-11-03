using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using VMCI.AdSkip.Automation;
using VMCI.AdSkip.Core;

namespace VMCI.AdSkip.Tray;

/// <summary>Notification-area icon that runs the ad skipper in the background and offers pause, logs, settings and autostart.</summary>
internal sealed class TrayApplicationContext : ApplicationContext
{
    // Warnings are rare, but never flood the user with notifications.
    private static readonly TimeSpan MinTimeBetweenNotifications = TimeSpan.FromMinutes(1);

    // NotifyIcon has no public API to show its menu at the cursor the way a right-click does (taskbar-aware placement).
    private static readonly MethodInfo? ShowContextMenu =
        typeof(NotifyIcon).GetMethod("ShowContextMenu", BindingFlags.Instance | BindingFlags.NonPublic);

    private readonly TrayLog _log;
    private readonly string _version = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "";
    private readonly Icon _runningIcon = LoadIcon("AdSkip.ico");
    private readonly Icon _pausedIcon = LoadIcon("AdSkipPaused.ico");
    private readonly NotifyIcon _notifyIcon;
    private readonly ToolStripMenuItem _statusItem;
    private readonly ToolStripMenuItem _pauseItem;
    private readonly ToolStripMenuItem _autoStartItem;
    private readonly System.Windows.Forms.Timer _timer = new() { Interval = 1000 };

    private CancellationTokenSource? _cts;
    private Task? _runTask;
    private IAdSkipper? _skipper;
    private int _skippedInEarlierRuns;
    private DateTime _lastNotification = DateTime.MinValue;

    public TrayApplicationContext(TrayLog log)
    {
        _log = log;

        _statusItem = new ToolStripMenuItem { Enabled = false };
        _pauseItem = new ToolStripMenuItem("Pause", null, (_, _) => TogglePause());
        _autoStartItem = new ToolStripMenuItem("Start with Windows", null, (_, _) => ToggleAutoStart());

        var menu = new ContextMenuStrip();
        menu.Items.AddRange(
        [
            _statusItem,
            new ToolStripSeparator(),
            _pauseItem,
            new ToolStripMenuItem("Open today's log", null, (_, _) => OpenLog()),
            new ToolStripMenuItem("Open log folder", null, (_, _) => OpenLogFolder()),
            new ToolStripMenuItem("Open settings", null, (_, _) => OpenSettings()),
            new ToolStripSeparator(),
            _autoStartItem,
            new ToolStripSeparator(),
            new ToolStripMenuItem("Exit", null, (_, _) => Exit()),
        ]);
        menu.Opening += (_, _) => RefreshMenu();

        _notifyIcon = new NotifyIcon { ContextMenuStrip = menu, Visible = true };
        _notifyIcon.MouseUp += (_, e) =>
        {
            // Right-click opens the menu by default; make a left-click do the same.
            if (e.Button == MouseButtons.Left) ShowContextMenu?.Invoke(_notifyIcon, null);
        };

        _timer.Tick += (_, _) => OnTimerTick();
        _timer.Start();

        try
        {
            AutoStart.UpdatePathIfEnabled();
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException or System.Security.SecurityException)
        {
            _log.Warn($"Could not update the 'Start with Windows' entry: {ex.Message}");
        }

        _log.Info($"AdSkip tray {_version} started from {AppContext.BaseDirectory}");
        Start();
    }

    private bool IsRunning => _runTask is { IsCompleted: false };

    private int SkippedCount => _skippedInEarlierRuns + (_skipper?.SkippedCount ?? 0);

    /// <summary>(Re)loads appsettings.json and starts scanning on a background thread.</summary>
    private void Start()
    {
        AdSkipSettings settings;
        try
        {
            settings = AdSkipSettingsLoader.Load(AppContext.BaseDirectory);
        }
        catch (AdSkipConfigurationException ex)
        {
            _log.Error(ex.Message);
            RefreshState();
            return;
        }

        _cts = new CancellationTokenSource();
        var token = _cts.Token;
        _runTask = Task.Run(async () =>
        {
            // UI Automation calls stay off the UI thread; the COM objects live on this worker.
            using var windowSource = new FlaUiBrowserWindowSource();
            var skipper = new AdSkipper(windowSource, settings, _log, TimeProvider.System);
            _skipper = skipper;
            await new AdSkipRunner(skipper, settings, _log, TimeProvider.System).RunAsync(token).ConfigureAwait(false);
        }, token);
        RefreshState();
    }

    private void Stop()
    {
        if (_cts is null) return;
        _cts.Cancel();
        try
        {
            _runTask?.Wait(TimeSpan.FromSeconds(5));
        }
        catch (AggregateException)
        {
            // Reported by OnTimerTick or not relevant anymore while stopping.
        }

        _skippedInEarlierRuns += _skipper?.SkippedCount ?? 0;
        _skipper = null;
        _runTask = null;
        _cts.Dispose();
        _cts = null;
        RefreshState();
    }

    private void TogglePause()
    {
        if (IsRunning)
        {
            Stop();
            _log.Info("Paused from the tray menu.");
        }
        else
        {
            Stop(); // Clean up a run that ended by itself.
            Start();
        }
    }

    private void ToggleAutoStart()
    {
        try
        {
            if (AutoStart.IsEnabled)
            {
                AutoStart.Disable();
                _log.Info("'Start with Windows' turned off.");
            }
            else
            {
                AutoStart.Enable();
                _log.Info($"'Start with Windows' turned on for {Environment.ProcessPath}.");
            }
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException or System.Security.SecurityException)
        {
            _log.Error($"Could not change 'Start with Windows': {ex.Message}");
        }
    }

    private void OnTimerTick()
    {
        if (_runTask is { IsFaulted: true } faulted)
        {
            _log.Error($"AdSkip stopped after an unexpected error: {faulted.Exception?.GetBaseException().Message}");
            Stop();
        }

        RefreshState();

        if (DateTime.UtcNow - _lastNotification >= MinTimeBetweenNotifications
            && _log.TryDequeueNotification(out var notification))
        {
            _lastNotification = DateTime.UtcNow;
            _notifyIcon.ShowBalloonTip(10_000, "AdSkip", notification.Message,
                notification.IsError ? ToolTipIcon.Error : ToolTipIcon.Warning);
        }
    }

    private void RefreshState()
    {
        var running = IsRunning;
        _notifyIcon.Icon = running ? _runningIcon : _pausedIcon;
        _notifyIcon.Text = running ? $"AdSkip: {SkippedCount} ad(s) skipped" : "AdSkip: paused";
    }

    private void RefreshMenu()
    {
        _statusItem.Text = $"AdSkip {_version}: {SkippedCount} ad(s) skipped{(IsRunning ? "" : " (paused)")}";
        _pauseItem.Text = IsRunning ? "Pause" : "Resume (reloads settings)";
        try
        {
            _autoStartItem.Checked = AutoStart.IsEnabled;
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException or System.Security.SecurityException)
        {
            _autoStartItem.Enabled = false;
        }
    }

    private void OpenLog()
    {
        var path = _log.File.CurrentFilePath;
        if (File.Exists(path)) Open("notepad.exe", path);
        else OpenLogFolder();
    }

    private void OpenLogFolder()
    {
        Directory.CreateDirectory(_log.File.Directory);
        Open("explorer.exe", _log.File.Directory);
    }

    private void OpenSettings() =>
        Open("notepad.exe", Path.Combine(AppContext.BaseDirectory, AdSkipSettingsLoader.DefaultFileName));

    private void Open(string program, string path)
    {
        try
        {
            Process.Start(new ProcessStartInfo(program) { ArgumentList = { path }, UseShellExecute = true })?.Dispose();
        }
        catch (Win32Exception ex)
        {
            _log.Error($"Could not open {path}: {ex.Message}");
        }
    }

    private void Exit()
    {
        Stop();
        _log.Info("Exited from the tray menu.");
        _notifyIcon.Visible = false;
        ExitThread();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _timer.Dispose();
            _notifyIcon.Dispose();
            _runningIcon.Dispose();
            _pausedIcon.Dispose();
            _cts?.Dispose();
        }
        base.Dispose(disposing);
    }

    private static Icon LoadIcon(string resourceName)
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName)
                           ?? throw new InvalidOperationException($"Missing embedded resource {resourceName}.");
        return new Icon(stream, SystemInformation.SmallIconSize);
    }
}
