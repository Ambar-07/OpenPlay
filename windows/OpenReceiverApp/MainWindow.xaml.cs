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
            
            ViewModel.PropertyChanged += ViewModel_PropertyChanged;
            // Connect to real C++ AirPlay State Machine
            ViewModel.ConnectRealState();
        }

        private void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ViewModel.CurrentLyric) && !string.IsNullOrEmpty(ViewModel.CurrentLyric))
            {
                var storyboard = new Microsoft.UI.Xaml.Media.Animation.Storyboard();
                
                var slideAnim = new Microsoft.UI.Xaml.Media.Animation.DoubleAnimation
                {
                    From = 25,
                    To = 0,
                    Duration = new Microsoft.UI.Xaml.Duration(System.TimeSpan.FromMilliseconds(450)),
                    EasingFunction = new Microsoft.UI.Xaml.Media.Animation.QuarticEase { EasingMode = Microsoft.UI.Xaml.Media.Animation.EasingMode.EaseOut }
                };
                
                var fadeAnim = new Microsoft.UI.Xaml.Media.Animation.DoubleAnimation
                {
                    From = 0.3,
                    To = 1.0,
                    Duration = new Microsoft.UI.Xaml.Duration(System.TimeSpan.FromMilliseconds(450))
                };
                
                Microsoft.UI.Xaml.Media.Animation.Storyboard.SetTarget(slideAnim, LyricsTranslate);
                Microsoft.UI.Xaml.Media.Animation.Storyboard.SetTargetProperty(slideAnim, "Y");
                
                Microsoft.UI.Xaml.Media.Animation.Storyboard.SetTarget(fadeAnim, LyricsPanel);
                Microsoft.UI.Xaml.Media.Animation.Storyboard.SetTargetProperty(fadeAnim, "Opacity");
                
                storyboard.Children.Add(slideAnim);
                storyboard.Children.Add(fadeAnim);
                storyboard.Begin();
            }
        }
    }
}
