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
            
            // Task 16: Load mock state initially
            ViewModel.LoadMockData();
        }
    }
}
