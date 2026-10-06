using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System;
using System.Text;
using System.IO;

namespace OpenReceiver.ViewModels
{
    public class NowPlayingViewModel : INotifyPropertyChanged
    {
        private string _title = "Welcome to OpenReceiver";
        private string _artist = "Waiting for AirPlay...";
        private string _album = "";
        private Microsoft.UI.Xaml.Media.ImageSource _albumArt;
        private bool _isPlaying = false;
        private double _position = 0;
        private double _duration = 100;
        private double _volumePercent = 100.0;
        
        public double VolumePercent
        {
            get => _volumePercent;
            set 
            { 
                _volumePercent = value; 
                OnPropertyChanged(); 
                OnPropertyChanged(nameof(VolumeScale));
            }
        }

        public double VolumeScale => _volumePercent / 100.0;
        
        // Lyrics State
        private string _previousLyric = "";
        private string _currentLyric = "";
        private string _nextLyric = "";
        private string _nextNextLyric = "";
        private bool _isLyricsAvailable = false;
        private System.Collections.Generic.List<LrcLine> _activeLyricsLines;
        
        private Microsoft.UI.Dispatching.DispatcherQueue _dispatcherQueue;
        private Microsoft.UI.Dispatching.DispatcherQueueTimer _timer;

        public Microsoft.UI.Dispatching.DispatcherQueue DispatcherQueue => _dispatcherQueue;

        public NowPlayingViewModel()
        {
            _dispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
            _timer = _dispatcherQueue.CreateTimer();
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += (s, e) =>
            {
                if (IsPlaying && Position < Duration)
                {
                    Position += 1;
                }
            };
            _timer.Start();
        }
        
        public string Title 
        { 
            get => _title; 
            set { _title = value; OnPropertyChanged(); } 
        }

        public string Artist 
        { 
            get => _artist; 
            set { _artist = value; OnPropertyChanged(); } 
        }

        public string Album 
        { 
            get => _album; 
            set { _album = value; OnPropertyChanged(); } 
        }

        public Microsoft.UI.Xaml.Media.ImageSource AlbumArt 
        { 
            get => _albumArt; 
            set { _albumArt = value; OnPropertyChanged(); } 
        }

        public void SetAlbumArtFromPath(string path)
        {
            if (string.IsNullOrEmpty(path)) return;
            try
            {
                _dispatcherQueue.TryEnqueue(() =>
                {
                    AlbumArt = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(new Uri(path));
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }
        }

        public void SetAlbumArtFromBytes(byte[] imageBytes)
        {
            if (imageBytes == null || imageBytes.Length == 0) return;
            
            byte[] bytesToLoad = imageBytes;
            
            _dispatcherQueue.TryEnqueue(() =>
            {
                try
                {
                    var stream = new Windows.Storage.Streams.InMemoryRandomAccessStream();
                    var dotnetStream = stream.AsStreamForWrite();
                    dotnetStream.Write(bytesToLoad, 0, bytesToLoad.Length);
                    dotnetStream.Flush();
                    
                    stream.Seek(0);
                    
                    var bitmap = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage();
                    bitmap.SetSource(stream);
                    AlbumArt = bitmap;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("AlbumArt Error: " + ex.Message);
                }
            });
        }

        public bool IsPlaying
        {
            get => _isPlaying;
            set { 
                _isPlaying = value; 
                OnPropertyChanged(); 
                OnPropertyChanged(nameof(PlayPauseGlyph));
            }
        }

        public string PlayPauseGlyph => _isPlaying ? "\uE769" : "\uE768"; // Pause / Play icons

        public double Position
        {
            get => _position;
            set { 
                _position = value; 
                OnPropertyChanged(); 
                OnPropertyChanged(nameof(PositionString));
                OnPropertyChanged(nameof(RemainingString));
                OnPropertyChanged(nameof(ProgressPercent));
                OnPropertyChanged(nameof(ProgressScale));
                
                UpdateLyricsForPosition(value);
            }
        }

        public double Duration
        {
            get => _duration;
            set { 
                _duration = value; 
                OnPropertyChanged(); 
                OnPropertyChanged(nameof(RemainingString));
                OnPropertyChanged(nameof(ProgressPercent));
            }
        }

        public string PositionString => TimeSpan.FromSeconds(_position).ToString(@"m\:ss");
        public string RemainingString => "-" + TimeSpan.FromSeconds(Math.Max(0, _duration - _position)).ToString(@"m\:ss");
        public double ProgressPercent => _duration > 0 ? (_position / _duration) * 100.0 : 0;
        public double ProgressScale => _duration > 0 ? Math.Min(Math.Max(_position / _duration, 0.0), 1.0) : 0;

        public string PreviousLyric
        {
            get => _previousLyric;
            set { _previousLyric = value; OnPropertyChanged(); }
        }

        public string CurrentLyric
        {
            get => _currentLyric;
            set { _currentLyric = value; OnPropertyChanged(); }
        }

        public string NextLyric
        {
            get => _nextLyric;
            set { _nextLyric = value; OnPropertyChanged(); }
        }

        public string NextNextLyric
        {
            get => _nextNextLyric;
            set { _nextNextLyric = value; OnPropertyChanged(); }
        }

        private bool _userPrefersLyrics = true;

        public bool IsLyricsAvailable
        {
            get => _isLyricsAvailable;
            set 
            { 
                _isLyricsAvailable = value; 
                OnPropertyChanged(); 
                OnPropertyChanged(nameof(LyricsVisibility));
                OnPropertyChanged(nameof(NoLyricsVisibility));
            }
        }

        public Microsoft.UI.Xaml.Visibility LyricsVisibility => _isLyricsAvailable ? Microsoft.UI.Xaml.Visibility.Visible : Microsoft.UI.Xaml.Visibility.Collapsed;
        public Microsoft.UI.Xaml.Visibility NoLyricsVisibility => (_userPrefersLyrics && !_isLyricsAvailable) ? Microsoft.UI.Xaml.Visibility.Visible : Microsoft.UI.Xaml.Visibility.Collapsed;
        public Microsoft.UI.Xaml.Visibility LyricsPanelVisibility => _userPrefersLyrics ? Microsoft.UI.Xaml.Visibility.Visible : Microsoft.UI.Xaml.Visibility.Collapsed;

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public void LoadMockData()
        {
            Title = "Song 2 (2012 Remaster)";
            Artist = "Blur — Blur";
            Album = "Blur";
            SetAlbumArtFromPath("https://upload.wikimedia.org/wikipedia/en/3/36/Blur_-_Song_2.jpg");
            IsPlaying = true;
            Duration = 121;
            Position = 17;

            // Mock Lyrics
            IsLyricsAvailable = true;
            PreviousLyric = "Woo-hoo";
            CurrentLyric = "Woo-hoo";
            NextLyric = "Woo-hoo";
        }

        public async void TogglePlayPause()
        {
            await DacpClient.SendCommandAsync("playpause");
        }

        public async void NextTrack()
        {
            await DacpClient.SendCommandAsync("nextitem");
        }

        public async void PreviousTrack()
        {
            await DacpClient.SendCommandAsync("previtem");
        }

        public void ToggleLyrics()
        {
            _userPrefersLyrics = !_userPrefersLyrics;
            IsLyricsAvailable = _userPrefersLyrics && _activeLyricsLines != null && _activeLyricsLines.Count > 0;
            
            OnPropertyChanged(nameof(LyricsButtonBackground));
            OnPropertyChanged(nameof(LyricsButtonForeground));
            OnPropertyChanged(nameof(LyricsButtonOpacity));
            OnPropertyChanged(nameof(LyricsPanelVisibility));
            OnPropertyChanged(nameof(NoLyricsVisibility));
        }

        public Microsoft.UI.Xaml.Media.SolidColorBrush LyricsButtonBackground => new Microsoft.UI.Xaml.Media.SolidColorBrush(_userPrefersLyrics ? Windows.UI.Color.FromArgb(50, 255, 255, 255) : Windows.UI.Color.FromArgb(0, 0, 0, 0));
        public Microsoft.UI.Xaml.Media.SolidColorBrush LyricsButtonForeground => new Microsoft.UI.Xaml.Media.SolidColorBrush(_userPrefersLyrics ? Windows.UI.Color.FromArgb(255, 255, 255, 255) : Windows.UI.Color.FromArgb(180, 255, 255, 255));
        public double LyricsButtonOpacity => _userPrefersLyrics ? 1.0 : 0.6;

        // P/Invoke definitions for the Native Bridge
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void TrackInfoCallback(string title, string artist, string album, double duration);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void PlaybackStateCallback(int status, double position);

        [DllImport("OpenReceiverCore.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern void InitializeBridge();

        [DllImport("OpenReceiverCore.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern void SetTrackInfoCallback(TrackInfoCallback callback);

        [DllImport("OpenReceiverCore.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern void SetPlaybackStateCallback(PlaybackStateCallback callback);

        [DllImport("OpenReceiverCore.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern void GenerateAppleResponse(string challengeBase64, string ipAddress, string macAddress, StringBuilder responseBuffer, int bufferSize);

        [DllImport("OpenReceiverCore.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern void DecryptAesKey(string rsaaeskeyB64, [Out] byte[] aesKeyOut);

        [DllImport("OpenReceiverCore.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr InitAlac();

        [DllImport("OpenReceiverCore.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void DecodeAlac(IntPtr alacPtr, byte[] input, int inputLen, byte[] output, out int outputLen);

        [DllImport("OpenReceiverCore.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void FreeAlac(IntPtr alacPtr);

        private TrackInfoCallback _trackCallback;
        private PlaybackStateCallback _playbackCallback;

        public void ConnectRealState()
        {
            // Keep references to delegates to prevent garbage collection
            _trackCallback = (t, art, alb, d) =>
            {
                UpdateTrackInfo(t, art, alb, d);
            };

            _playbackCallback = (status, pos) =>
            {
                _dispatcherQueue.TryEnqueue(() =>
                {
                    IsPlaying = (status == 1);
                    Position = pos;
                });
            };

            // Start the mock C# AirPlay Server
            var server = new AirPlayServer(this);
            server.Start();

            InitializeBridge();
            SetTrackInfoCallback(_trackCallback);
            SetPlaybackStateCallback(_playbackCallback);
        }

        public void UpdateTrackInfo(string t, string art, string alb, double d)
        {
            _dispatcherQueue.TryEnqueue(async () =>
            {
                bool trackChanged = (Title != t || Artist != art);
                
                if (t != null) Title = t;
                if (art != null) Artist = art;
                if (alb != null) Album = alb;
                if (d > 0) Duration = d;
                
                if (trackChanged && !string.IsNullOrEmpty(Title))
                {
                    IsLyricsAvailable = false;
                    CurrentLyric = "";
                    NextLyric = "";
                    NextNextLyric = "";
                    _activeLyricsLines = null;
                    
                    var lines = await LyricsFetcher.FetchLyricsAsync(Title, Artist, Album);
                    if (lines != null && lines.Count > 0)
                    {
                        _activeLyricsLines = lines;
                        IsLyricsAvailable = _userPrefersLyrics;
                        UpdateLyricsForPosition(Position);
                    }
                    else
                    {
                        IsLyricsAvailable = false;
                    }
                    
                    OnPropertyChanged(nameof(LyricsButtonBackground));
                    OnPropertyChanged(nameof(LyricsButtonForeground));
                    OnPropertyChanged(nameof(LyricsButtonOpacity));
                    OnPropertyChanged(nameof(LyricsPanelVisibility));
                    OnPropertyChanged(nameof(NoLyricsVisibility));
                }
            });
        }
        
        private void UpdateLyricsForPosition(double positionSecs)
        {
            if (_activeLyricsLines == null || _activeLyricsLines.Count == 0)
                return;

            // Find the active line (the last line that is <= positionSecs)
            int activeIndex = -1;
            for (int i = 0; i < _activeLyricsLines.Count; i++)
            {
                if (positionSecs >= _activeLyricsLines[i].TimeSeconds)
                    activeIndex = i;
                else
                    break;
            }

            if (activeIndex >= 0)
            {
                // Only update if it actually changed, to avoid triggering the animation repeatedly
                if (CurrentLyric != _activeLyricsLines[activeIndex].Text)
                {
                    PreviousLyric = (activeIndex - 1 >= 0) ? _activeLyricsLines[activeIndex - 1].Text : "";
                    CurrentLyric = _activeLyricsLines[activeIndex].Text;
                    NextLyric = (activeIndex + 1 < _activeLyricsLines.Count) ? _activeLyricsLines[activeIndex + 1].Text : "";
                    NextNextLyric = (activeIndex + 2 < _activeLyricsLines.Count) ? _activeLyricsLines[activeIndex + 2].Text : "";
                }
            }
            else
            {
                // Before the first line
                if (CurrentLyric != "")
                {
                    PreviousLyric = "";
                    CurrentLyric = "";
                    NextLyric = _activeLyricsLines.Count > 0 ? _activeLyricsLines[0].Text : "";
                    NextNextLyric = _activeLyricsLines.Count > 1 ? _activeLyricsLines[1].Text : "";
                }
            }
        }

        public void UpdateVolume(float dbVolume)
        {
            // AirPlay volume is typically -30.0 (mute) to 0.0 (max)
            if (dbVolume <= -30.0f)
                VolumePercent = 0;
            else if (dbVolume >= 0.0f)
                VolumePercent = 100;
            else
                VolumePercent = (dbVolume + 30.0f) / 30.0f * 100.0f;
        }
    }
}
