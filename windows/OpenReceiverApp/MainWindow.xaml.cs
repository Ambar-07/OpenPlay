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
            
            // Enable gorgeous Acrylic Backdrop
            SystemBackdrop = new Microsoft.UI.Xaml.Media.DesktopAcrylicBackdrop();
            
            ViewModel.PropertyChanged += ViewModel_PropertyChanged;
            // Connect to real C++ AirPlay State Machine
            ViewModel.ConnectRealState();
            
            StartBackgroundAnimation();
        }

        private void StartBackgroundAnimation()
        {
            var storyboard = new Microsoft.UI.Xaml.Media.Animation.Storyboard { RepeatBehavior = Microsoft.UI.Xaml.Media.Animation.RepeatBehavior.Forever };

            var scaleXAnim = new Microsoft.UI.Xaml.Media.Animation.DoubleAnimation
            {
                From = 1.1, To = 1.5,
                Duration = new Microsoft.UI.Xaml.Duration(System.TimeSpan.FromSeconds(25)),
                AutoReverse = true,
                EasingFunction = new Microsoft.UI.Xaml.Media.Animation.SineEase { EasingMode = Microsoft.UI.Xaml.Media.Animation.EasingMode.EaseInOut }
            };
            Microsoft.UI.Xaml.Media.Animation.Storyboard.SetTarget(scaleXAnim, BgTransform);
            Microsoft.UI.Xaml.Media.Animation.Storyboard.SetTargetProperty(scaleXAnim, "ScaleX");

            var scaleYAnim = new Microsoft.UI.Xaml.Media.Animation.DoubleAnimation
            {
                From = 1.1, To = 1.5,
                Duration = new Microsoft.UI.Xaml.Duration(System.TimeSpan.FromSeconds(30)),
                AutoReverse = true,
                EasingFunction = new Microsoft.UI.Xaml.Media.Animation.SineEase { EasingMode = Microsoft.UI.Xaml.Media.Animation.EasingMode.EaseInOut }
            };
            Microsoft.UI.Xaml.Media.Animation.Storyboard.SetTarget(scaleYAnim, BgTransform);
            Microsoft.UI.Xaml.Media.Animation.Storyboard.SetTargetProperty(scaleYAnim, "ScaleY");

            var rotateAnim = new Microsoft.UI.Xaml.Media.Animation.DoubleAnimation
            {
                From = -5, To = 5,
                Duration = new Microsoft.UI.Xaml.Duration(System.TimeSpan.FromSeconds(40)),
                AutoReverse = true,
                EasingFunction = new Microsoft.UI.Xaml.Media.Animation.SineEase { EasingMode = Microsoft.UI.Xaml.Media.Animation.EasingMode.EaseInOut }
            };
            Microsoft.UI.Xaml.Media.Animation.Storyboard.SetTarget(rotateAnim, BgTransform);
            Microsoft.UI.Xaml.Media.Animation.Storyboard.SetTargetProperty(rotateAnim, "Rotation");

            storyboard.Children.Add(scaleXAnim);
            storyboard.Children.Add(scaleYAnim);
            storyboard.Children.Add(rotateAnim);
            storyboard.Begin();
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
                    From = 0.6,
                    To = 1.0,
                    Duration = new Microsoft.UI.Xaml.Duration(System.TimeSpan.FromMilliseconds(500)),
                    EasingFunction = new Microsoft.UI.Xaml.Media.Animation.QuarticEase { EasingMode = Microsoft.UI.Xaml.Media.Animation.EasingMode.EaseOut }
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
