using VMCI.AdSkip.Core;
using VMCI.AdSkip.Tray;

// Started at login and by hand: only one AdSkip may watch the browsers per user session.
using var singleInstance = new Mutex(initiallyOwned: true, @"Local\VMCI.AdSkip.Tray", out var isFirstInstance);
if (!isFirstInstance)
{
    MessageBox.Show("AdSkip is already running. Look for its icon in the notification area.", "AdSkip",
        MessageBoxButtons.OK, MessageBoxIcon.Information);
    return;
}

ApplicationConfiguration.Initialize();
var log = new TrayLog(new FileAdSkipLog(FileAdSkipLog.DefaultDirectory, TimeProvider.System));
using var context = new TrayApplicationContext(log);
Application.Run(context);
