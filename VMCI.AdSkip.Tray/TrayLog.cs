using System.Collections.Concurrent;
using VMCI.AdSkip.Core;

namespace VMCI.AdSkip.Tray;

/// <summary>Writes to the log file and queues warnings and errors so the UI thread can show them as notifications.</summary>
internal sealed class TrayLog(FileAdSkipLog file) : IAdSkipLog
{
    private readonly ConcurrentQueue<(string Message, bool IsError)> _notifications = new();

    public FileAdSkipLog File => file;

    public void Info(string message) => file.Info(message);

    public void Warn(string message)
    {
        file.Warn(message);
        _notifications.Enqueue((message, false));
    }

    public void Error(string message)
    {
        file.Error(message);
        _notifications.Enqueue((message, true));
    }

    public bool TryDequeueNotification(out (string Message, bool IsError) notification) =>
        _notifications.TryDequeue(out notification);
}
