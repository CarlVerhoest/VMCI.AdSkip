using VMCI.AdSkip.Core;

namespace VMCI.AdSkip;

internal sealed class ConsoleLog : IAdSkipLog
{
    public void Info(string message) => Write(message, ConsoleColor.Gray);
    public void Warn(string message) => Write(message, ConsoleColor.Yellow);
    public void Error(string message) => Write(message, ConsoleColor.Red);

    private static void Write(string message, ConsoleColor color)
    {
        Console.ForegroundColor = color;
        Console.WriteLine($"{DateTime.Now:HH:mm:ss} {message}");
        Console.ResetColor();
    }
}
