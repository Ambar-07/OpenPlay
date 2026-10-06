using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System;

namespace OpenReceiver.ViewModels
{
    public class NowPlayingViewModel : INotifyPropertyChanged
    {
        private string _title = "Welcome to OpenReceiver";
        private string _artist = "Waiting for AirPlay...";
        private string _album = "";
        private bool _isPlaying = false;
        private double _position = 0;
        private double _duration = 100;
        
        // Lyrics State
        private string _previousLyric = "";
        private string _currentLyric = "";
        private string _nextLyric = "";
        private bool _isLyricsAvailable = false;
        
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

        public bool IsPlaying
        {
            get => _isPlaying;
            set { _isPlaying = value; OnPropertyChanged(); }
        }

        public double Position
        {
            get => _position;
            set { _position = value; OnPropertyChanged(); }
        }

        public double Duration
        {
            get => _duration;
            set { _duration = value; OnPropertyChanged(); }
        }

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

        public bool IsLyricsAvailable
        {
            get => _isLyricsAvailable;
            set { _isLyricsAvailable = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public void LoadMockData()
        {
            Title = "Starboy";
            Artist = "The Weeknd, Daft Punk";
            Album = "Starboy";
            IsPlaying = true;
            Duration = 230;
            Position = 45;

            // Mock Lyrics
            IsLyricsAvailable = true;
            PreviousLyric = "I'm a motherf***in' starboy";
            CurrentLyric = "Look what you've done";
            NextLyric = "I'm a motherf***in' starboy";
        }

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

        private TrackInfoCallback _trackCallback;
        private PlaybackStateCallback _playbackCallback;

        public void ConnectRealState()
        {
            // Keep references to delegates to prevent garbage collection
            _trackCallback = (t, art, alb, d) =>
            {
                // Must marshal back to UI thread in real app
                Title = t;
                Artist = art;
                Album = alb;
                Duration = d;
            };

            _playbackCallback = (status, pos) =>
            {
                IsPlaying = (status == 1);
                Position = pos;
            };

            InitializeBridge();
            SetTrackInfoCallback(_trackCallback);
            SetPlaybackStateCallback(_playbackCallback);
        }
    }
}
