using System;

namespace OpenReceiver.Lifecycle
{
    /// <summary>
    /// Handles the application lifecycle: ensuring it stays running in the background,
    /// intercepting close events to minimize to tray instead, and handling complete teardown.
    /// </summary>
    public class AppLifecycle
    {
        private readonly TrayManager _trayManager;
        private bool _isShuttingDown = false;

        public AppLifecycle(TrayManager trayManager)
        {
            _trayManager = trayManager;
            _trayManager.OnExitRequested += HandleExitRequested;
        }

        public bool HandleWindowCloseRequested()
        {
            if (_isShuttingDown)
            {
                // Allow the window to close and app to exit
                return true;
            }

            // Cancel the close operation and hide the window instead.
            // OpenReceiver is an AirPlay destination; it must stay running in the background.
            HideMainWindow();
            return false;
        }

        private void HandleExitRequested(object sender, EventArgs e)
        {
            _isShuttingDown = true;
            _trayManager.Dispose();
            
            // Stop the native C++ AirPlay Server gracefully here
            
            // Force application exit
            Environment.Exit(0);
        }

        private void HideMainWindow()
        {
            // STUB: In WinUI 3, hide the AppWindow (AppWindow.Hide())
        }
        
        public void ShowMainWindow()
        {
            // STUB: In WinUI 3, show and bring to front (AppWindow.Show())
        }
    }
}
