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

            // Blob 1: Rotate and Scale
            var rotate1 = new Microsoft.UI.Xaml.Media.Animation.DoubleAnimation { From = 0, To = 360, Duration = new Microsoft.UI.Xaml.Duration(System.TimeSpan.FromSeconds(60)) };
            Microsoft.UI.Xaml.Media.Animation.Storyboard.SetTarget(rotate1, Blob1Transform);
            Microsoft.UI.Xaml.Media.Animation.Storyboard.SetTargetProperty(rotate1, "Rotation");

            var scale1 = new Microsoft.UI.Xaml.Media.Animation.DoubleAnimation { From = 1.3, To = 1.8, Duration = new Microsoft.UI.Xaml.Duration(System.TimeSpan.FromSeconds(25)), AutoReverse = true, EasingFunction = new Microsoft.UI.Xaml.Media.Animation.SineEase { EasingMode = Microsoft.UI.Xaml.Media.Animation.EasingMode.EaseInOut } };
            Microsoft.UI.Xaml.Media.Animation.Storyboard.SetTarget(scale1, Blob1Transform);
            Microsoft.UI.Xaml.Media.Animation.Storyboard.SetTargetProperty(scale1, "ScaleX");
            Microsoft.UI.Xaml.Media.Animation.Storyboard.SetTarget(scale1, Blob1Transform);
            Microsoft.UI.Xaml.Media.Animation.Storyboard.SetTargetProperty(scale1, "ScaleY");

            // Blob 2: Rotate and Scale differently
            var rotate2 = new Microsoft.UI.Xaml.Media.Animation.DoubleAnimation { From = 45, To = -315, Duration = new Microsoft.UI.Xaml.Duration(System.TimeSpan.FromSeconds(75)) };
            Microsoft.UI.Xaml.Media.Animation.Storyboard.SetTarget(rotate2, Blob2Transform);
            Microsoft.UI.Xaml.Media.Animation.Storyboard.SetTargetProperty(rotate2, "Rotation");

            var scale2 = new Microsoft.UI.Xaml.Media.Animation.DoubleAnimation { From = 1.7, To = 2.2, Duration = new Microsoft.UI.Xaml.Duration(System.TimeSpan.FromSeconds(33)), AutoReverse = true, EasingFunction = new Microsoft.UI.Xaml.Media.Animation.SineEase { EasingMode = Microsoft.UI.Xaml.Media.Animation.EasingMode.EaseInOut } };
            Microsoft.UI.Xaml.Media.Animation.Storyboard.SetTarget(scale2, Blob2Transform);
            Microsoft.UI.Xaml.Media.Animation.Storyboard.SetTargetProperty(scale2, "ScaleX");
            Microsoft.UI.Xaml.Media.Animation.Storyboard.SetTarget(scale2, Blob2Transform);
            Microsoft.UI.Xaml.Media.Animation.Storyboard.SetTargetProperty(scale2, "ScaleY");

            // Blob 3: Rotate and Translate
            var rotate3 = new Microsoft.UI.Xaml.Media.Animation.DoubleAnimation { From = 90, To = 450, Duration = new Microsoft.UI.Xaml.Duration(System.TimeSpan.FromSeconds(90)) };
            Microsoft.UI.Xaml.Media.Animation.Storyboard.SetTarget(rotate3, Blob3Transform);
            Microsoft.UI.Xaml.Media.Animation.Storyboard.SetTargetProperty(rotate3, "Rotation");

            var trans3 = new Microsoft.UI.Xaml.Media.Animation.DoubleAnimation { From = -100, To = 100, Duration = new Microsoft.UI.Xaml.Duration(System.TimeSpan.FromSeconds(40)), AutoReverse = true, EasingFunction = new Microsoft.UI.Xaml.Media.Animation.SineEase { EasingMode = Microsoft.UI.Xaml.Media.Animation.EasingMode.EaseInOut } };
            Microsoft.UI.Xaml.Media.Animation.Storyboard.SetTarget(trans3, Blob3Transform);
            Microsoft.UI.Xaml.Media.Animation.Storyboard.SetTargetProperty(trans3, "TranslateX");

            storyboard.Children.Add(rotate1);
            storyboard.Children.Add(scale1);
            storyboard.Children.Add(rotate2);
            storyboard.Children.Add(scale2);
            storyboard.Children.Add(rotate3);
            storyboard.Children.Add(trans3);
            
            storyboard.Begin();
        }

        private void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ViewModel.CurrentLyric) && !string.IsNullOrEmpty(ViewModel.CurrentLyric))
            {
                var storyboard = new Microsoft.UI.Xaml.Media.Animation.Storyboard();
                
                var scaleAnim = new Microsoft.UI.Xaml.Media.Animation.DoubleAnimation
                {
                    From = 0.9,
                    To = 1.0,
                    Duration = new Microsoft.UI.Xaml.Duration(System.TimeSpan.FromMilliseconds(450)),
                    EasingFunction = new Microsoft.UI.Xaml.Media.Animation.QuarticEase { EasingMode = Microsoft.UI.Xaml.Media.Animation.EasingMode.EaseOut }
                };
                
                var fadeAnim = new Microsoft.UI.Xaml.Media.Animation.DoubleAnimation
                {
                    From = 0.4,
                    To = 1.0,
                    Duration = new Microsoft.UI.Xaml.Duration(System.TimeSpan.FromMilliseconds(450)),
                    EasingFunction = new Microsoft.UI.Xaml.Media.Animation.QuarticEase { EasingMode = Microsoft.UI.Xaml.Media.Animation.EasingMode.EaseOut }
                };
                
                Microsoft.UI.Xaml.Media.Animation.Storyboard.SetTarget(scaleAnim, CurrentLyricScale);
                Microsoft.UI.Xaml.Media.Animation.Storyboard.SetTargetProperty(scaleAnim, "ScaleX");
                
                var scaleYAnim = new Microsoft.UI.Xaml.Media.Animation.DoubleAnimation
                {
                    From = 0.9, To = 1.0,
                    Duration = new Microsoft.UI.Xaml.Duration(System.TimeSpan.FromMilliseconds(450)),
                    EasingFunction = new Microsoft.UI.Xaml.Media.Animation.QuarticEase { EasingMode = Microsoft.UI.Xaml.Media.Animation.EasingMode.EaseOut }
                };
                Microsoft.UI.Xaml.Media.Animation.Storyboard.SetTarget(scaleYAnim, CurrentLyricScale);
                Microsoft.UI.Xaml.Media.Animation.Storyboard.SetTargetProperty(scaleYAnim, "ScaleY");

                Microsoft.UI.Xaml.Media.Animation.Storyboard.SetTarget(fadeAnim, CurrentLyricText);
                Microsoft.UI.Xaml.Media.Animation.Storyboard.SetTargetProperty(fadeAnim, "Opacity");
                
                storyboard.Children.Add(scaleAnim);
                storyboard.Children.Add(scaleYAnim);
                storyboard.Children.Add(fadeAnim);
                storyboard.Begin();
            }
            else if (e.PropertyName == nameof(ViewModel.LyricsVisibility))
            {
                if (ViewModel.LyricsVisibility == Visibility.Visible)
                {
                    AlbumColumn.Width = new GridLength(4.5, GridUnitType.Star);
                    LyricsColumn.Width = new GridLength(5.5, GridUnitType.Star);
                    AlbumViewbox.HorizontalAlignment = HorizontalAlignment.Right;
                }
                else
                {
                    // Center the album art by making it take the full width
                    AlbumColumn.Width = new GridLength(1, GridUnitType.Star);
                    LyricsColumn.Width = new GridLength(0);
                    AlbumViewbox.HorizontalAlignment = HorizontalAlignment.Center;
                }
            }
        }
    }
}
