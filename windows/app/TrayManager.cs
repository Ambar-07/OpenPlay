using System;

namespace OpenReceiver.Lifecycle
{
    /// <summary>
    /// Manages the System Tray (Taskbar) icon for OpenReceiver.
    /// Since OpenReceiver is a background service primarily, it should minimize to the tray
    /// and only show the Cinematic UI when music starts or when explicitly opened.
    /// </summary>
    public class TrayManager
    {
        public event EventHandler OnShowUiRequested;
        public event EventHandler OnExitRequested;

        public void InitializeTrayIcon()
        {
            // STUB: In a production WinUI 3 app, use H.NotifyIcon or P/Invoke Shell_NotifyIcon
            // to register the tray icon here.
            
            // Setup Context Menu:
            // - "Show Now Playing UI" -> fires OnShowUiRequested
            // - "Settings" -> opens settings flyout
            // - "Exit OpenReceiver" -> fires OnExitRequested
        }

        public void ShowNotification(string title, string message)
        {
            // STUB: P/Invoke to show balloon tooltip for "Now Playing: Title by Artist"
        }

        public void Dispose()
        {
            // STUB: Remove tray icon from system taskbar to prevent ghost icons on exit
        }
    }
}
