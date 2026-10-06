using Microsoft.UI.Xaml;
using OpenReceiver.ViewModels;

namespace OpenReceiver
{
    public sealed partial class MainWindow : Window
    {
        public NowPlayingViewModel ViewModel { get; }

        public MainWindow()
        {
            ViewModel = new NowPlayingViewModel();
            this.InitializeComponent();
            
            // Extend content into title bar
            ExtendsContentIntoTitleBar = true;
            SetTitleBar(AppTitleBar);
            
            // Connect to real C++ AirPlay State Machine
            ViewModel.ConnectRealState();
        }
    }
}
