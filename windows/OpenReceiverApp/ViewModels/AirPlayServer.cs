using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NAudio.Wave;

namespace OpenReceiver.ViewModels
{
    /// <summary>
    /// Minimal AirPlay 1 (RAOP) RTSP server.
    /// </summary>
    public class AirPlayServer
    {
        public const int RtspPort = 7000;
        public const string DeviceMac = "00:11:22:33:44:55"; // Must match advertise_mdns.py

        private const int AudioPort = 6000;
        private const int ControlPort = 6001;
        private const int TimingPort = 6002;

        private TcpListener _listener;
        private bool _isRunning;
        private readonly NowPlayingViewModel _viewModel;

        private UdpClient _audioSocket;
        private UdpClient _controlSocket;
        private UdpClient _timingSocket;
        private long _audioPackets;

        private byte[] _aesKey = new byte[16];
        private byte[] _aesIv = new byte[16];

        private IntPtr _alacPtr;
        private WaveOutEvent _waveOut;
        private BufferedWaveProvider _waveProvider;

        private static readonly string LogPath = @"C:\Users\ambar\OneDrive\Desktop\airplay.log";
        private static readonly object LogLock = new object();

        public AirPlayServer(NowPlayingViewModel viewModel)
        {
            _viewModel = viewModel;
        }

        public static void Log(string msg)
        {
            string line = $"[{DateTime.Now:HH:mm:ss.fff}] {msg}";
            System.Diagnostics.Debug.WriteLine(line);
            try
            {
                lock (LogLock)
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(LogPath));
                    File.AppendAllText(LogPath, line + Environment.NewLine);
                }
            }
            catch { }
        }

        public void Start()
        {
            Log("==== AirPlayServer starting ====");
            Log("Log file: " + LogPath);
            try
            {
                _listener = TcpListener.Create(RtspPort); // dual-mode IPv4/IPv6
                _listener.Start();
                _isRunning = true;
                Log($"Listening for RTSP on port {RtspPort}");
                Task.Run(AcceptClientsAsync);
            }
            catch (Exception ex)
            {
                Log("FAILED to start listener: " + ex);
            }
        }

        private async Task AcceptClientsAsync()
        {
            while (_isRunning)
            {
                try
                {
                    var client = await _listener.AcceptTcpClientAsync();
                    Log($"TCP connection from {client.Client.RemoteEndPoint}");
                    _ = Task.Run(() => HandleClientAsync(client));
                }
                catch (Exception ex)
                {
                    Log("Accept error: " + ex.Message);
                }
            }
        }

        // ---------- RTSP parsing (byte level, no StreamReader buffering) ----------

        private static async Task<string> ReadLineAsync(NetworkStream stream)
        {
            var sb = new StringBuilder();
            var one = new byte[1];
            while (true)
            {
                int n = await stream.ReadAsync(one, 0, 1);
                if (n == 0) return sb.Length > 0 ? sb.ToString() : null;
                char c = (char)one[0];
                if (c == '\n')
                {
                    if (sb.Length > 0 && sb[sb.Length - 1] == '\r') sb.Length--;
                    return sb.ToString();
                }
                sb.Append(c);
            }
        }

        private static async Task<bool> ReadExactAsync(NetworkStream stream, byte[] buf)
        {
            int read = 0;
            while (read < buf.Length)
            {
                int n = await stream.ReadAsync(buf, read, buf.Length - read);
                if (n == 0) return false;
                read += n;
            }
            return true;
        }

        private async Task HandleClientAsync(TcpClient client)
        {
            var local = (IPEndPoint)client.Client.LocalEndPoint;
            IPAddress localIp = local.Address.IsIPv4MappedToIPv6 ? local.Address.MapToIPv4() : local.Address;
            var remote = (IPEndPoint)client.Client.RemoteEndPoint;
            IPAddress remoteIp = remote.Address.IsIPv4MappedToIPv6 ? remote.Address.MapToIPv4() : remote.Address;

            try
            {
                using (client)
                using (var stream = client.GetStream())
                {
                    while (true)
                    {
                        string requestLine = await ReadLineAsync(stream);
                        if (requestLine == null) break;
                        if (requestLine.Length == 0) continue;

                        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                        string h;
                        while (!string.IsNullOrEmpty(h = await ReadLineAsync(stream)))
                        {
                            int idx = h.IndexOf(':');
                            if (idx > 0) headers[h.Substring(0, idx).Trim()] = h.Substring(idx + 1).Trim();
                        }

                        byte[] body = Array.Empty<byte>();
                        if (headers.TryGetValue("Content-Length", out var cl) && int.TryParse(cl, out int len) && len > 0)
                        {
                            body = new byte[len];
                            if (!await ReadExactAsync(stream, body)) break;
                        }

                        string[] parts = requestLine.Split(' ');
                        string method = parts[0];
                        string uri = parts.Length > 1 ? parts[1] : "";
                        headers.TryGetValue("CSeq", out var cseq);
                        cseq ??= "0";

                        Log($"<- {requestLine} (CSeq {cseq}, {body.Length} body bytes)");
                        foreach (var kv in headers) Log($"     {kv.Key}: {kv.Value}");
                        if (body.Length > 0 && headers.TryGetValue("Content-Type", out var ctype) &&
                            (ctype.StartsWith("application/sdp") || ctype.StartsWith("text/parameters")))
                        {
                            Log("     BODY:\n" + Encoding.UTF8.GetString(body));
                        }



                        if (headers.TryGetValue("Active-Remote", out var activeRemote))
                        {
                            DacpClient.ActiveRemote = activeRemote;
                            DacpClient.ClientIp = remoteIp.ToString();
                        }
                        
                        if (headers.TryGetValue("DACP-ID", out var dacpId))
                        {
                            DacpClient.DacpId = dacpId;
                        }

                        bool close = await ProcessRequestAsync(stream, method, uri, cseq, headers, body, localIp, remoteIp);
                        if (close) break;
                    }
                }
            }
            catch (Exception ex)
            {
                Log("Client error: " + ex.Message);
            }
            Log($"Connection from {remoteIp} closed");
        }

        private async Task<bool> ProcessRequestAsync(NetworkStream stream, string method, string uri, string cseq,
            Dictionary<string, string> headers, byte[] body, IPAddress localIp, IPAddress remoteIp)
        {
            var extra = new List<string>();
            string respBody = null;
            int status = 200;
            string statusText = "OK";
            bool close = false;

            if (headers.TryGetValue("Apple-Challenge", out var challenge) && !string.IsNullOrEmpty(challenge))
            {
                var sb = new StringBuilder(1024);
                NowPlayingViewModel.GenerateAppleResponse(challenge, localIp.ToString(), DeviceMac, sb, sb.Capacity);
                string resp = sb.ToString();
                Log($"     Apple-Response computed for ip={localIp}: {(resp.StartsWith("ERROR") ? resp : resp.Substring(0, Math.Min(20, resp.Length)) + "...")}");
                extra.Add("Apple-Response: " + resp);
            }

            switch (method)
            {
                case "OPTIONS":
                    extra.Add("Public: ANNOUNCE, SETUP, RECORD, PAUSE, FLUSH, TEARDOWN, OPTIONS, GET_PARAMETER, SET_PARAMETER");
                    break;

                case "ANNOUNCE":
                    if (body.Length > 0)
                    {
                        string sdp = Encoding.UTF8.GetString(body);
                        var lines = sdp.Split('\n');
                        string rsaaeskey = null;
                        string aesiv = null;
                        foreach (var l in lines)
                        {
                            if (l.StartsWith("a=rsaaeskey:")) rsaaeskey = l.Substring(12).Trim();
                            if (l.StartsWith("a=aesiv:")) aesiv = l.Substring(8).Trim();
                        }
                        if (rsaaeskey != null && aesiv != null)
                        {
                            NowPlayingViewModel.DecryptAesKey(rsaaeskey, _aesKey);
                            var ivBytes = Convert.FromBase64String(aesiv.PadRight(aesiv.Length + (4 - aesiv.Length % 4) % 4, '='));
                            Array.Copy(ivBytes, _aesIv, Math.Min(ivBytes.Length, 16));
                            Log($"     Extracted AES Key & IV.");
                        }
                    }
                    break;

                case "SETUP":
                    EnsureUdpSockets();
                    
                    if (_waveOut == null)
                    {
                        _alacPtr = NowPlayingViewModel.InitAlac();
                        _waveOut = new WaveOutEvent();
                        _waveProvider = new BufferedWaveProvider(new WaveFormat(44100, 16, 2))
                        {
                            BufferDuration = TimeSpan.FromSeconds(5),
                            DiscardOnBufferOverflow = true
                        };
                        _waveOut.Init(_waveProvider);
                    }

                    extra.Add("Session: 1");
                    extra.Add($"Transport: RTP/AVP/UDP;unicast;mode=record;server_port={AudioPort};control_port={ControlPort};timing_port={TimingPort}");
                    extra.Add("Audio-Jack-Status: connected; type=analog");
                    _viewModel.DispatcherQueue.TryEnqueue(() =>
                    {
                        _viewModel.Title = "Connected to iPhone!";
                        _viewModel.Artist = "Ready to play";
                        _viewModel.Album = "";
                    });
                    break;

                case "RECORD":
                    extra.Add("Audio-Latency: 11025");
                    _viewModel.DispatcherQueue.TryEnqueue(() => _viewModel.IsPlaying = true);
                    _waveOut?.Play();
                    break;

                case "FLUSH":
                case "PAUSE":
                    break;

                case "GET_PARAMETER":
                    extra.Add("Content-Type: text/parameters");
                    respBody = "volume: 0.000000\r\n";
                    break;

                case "SET_PARAMETER":
                    if (headers.TryGetValue("Content-Type", out var setParamType))
                    {
                        if (setParamType.StartsWith("image/jpeg") || setParamType.StartsWith("image/png"))
                        {
                            if (body.Length > 0)
                            {
                                try { _viewModel.SetAlbumArtFromBytes(body); }
                                catch (Exception ex) { Log("Failed to set album art: " + ex.Message); }
                            }
                        }
                        else if (setParamType.StartsWith("application/x-dmap-tagged"))
                        {
                            if (body.Length > 0) ParseDmapMetadata(body);
                        }
                        else if (setParamType.StartsWith("text/parameters"))
                        {
                            string textParams = System.Text.Encoding.UTF8.GetString(body);
                            var lines = textParams.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                            foreach (var line in lines)
                            {
                                if (line.StartsWith("volume:"))
                                {
                                    if (float.TryParse(line.Substring(7).Trim(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out float vol))
                                    {
                                        _viewModel.DispatcherQueue.TryEnqueue(() => _viewModel.UpdateVolume(vol));
                                    }
                                }
                            }
                        }
                    }
                    break;

                case "TEARDOWN":
                    extra.Add("Connection: close");
                    _viewModel.DispatcherQueue.TryEnqueue(() => _viewModel.IsPlaying = false);
                    _waveOut?.Stop();
                    _waveProvider?.ClearBuffer();
                    close = true;
                    break;

                case "PUT":
                case "POST":
                    if (headers.TryGetValue("Content-Type", out var ctype2) && 
                        (ctype2.StartsWith("image/jpeg") || ctype2.StartsWith("image/png")))
                    {
                        if (body.Length > 0)
                        {
                            try
                            {
                                _viewModel.SetAlbumArtFromBytes(body);
                            }
                            catch (Exception ex)
                            {
                                Log("Failed to save album art: " + ex.Message);
                            }
                        }
                    }
                    else if (body.Length > 0 && ctype2 != null && ctype2.StartsWith("application/x-dmap-tagged"))
                    {
                        ParseDmapMetadata(body);
                    }
                    break;

                default:
                    // e.g. POST /pair-setup, /fp-setup -> AirPlay 2 features we don't support
                    Log($"     Unsupported request {method} {uri}");
                    status = 501;
                    statusText = "Not Implemented";
                    break;
            }

            var sbResp = new StringBuilder();
            sbResp.Append($"RTSP/1.0 {status} {statusText}\r\n");
            sbResp.Append($"CSeq: {cseq}\r\n");
            sbResp.Append("Server: AirTunes/130.14\r\n");
            foreach (var e in extra) sbResp.Append(e).Append("\r\n");
            byte[] bodyBytes = respBody != null ? Encoding.ASCII.GetBytes(respBody) : Array.Empty<byte>();
            if (bodyBytes.Length > 0) sbResp.Append($"Content-Length: {bodyBytes.Length}\r\n");
            sbResp.Append("\r\n");

            byte[] headerBytes = Encoding.ASCII.GetBytes(sbResp.ToString());
            await stream.WriteAsync(headerBytes, 0, headerBytes.Length);
            if (bodyBytes.Length > 0) await stream.WriteAsync(bodyBytes, 0, bodyBytes.Length);
            await stream.FlushAsync();

            Log($"-> {status} {statusText} for {method}");
            return close;
        }

        // ---------- UDP audio / control / timing ----------

        private void EnsureUdpSockets()
        {
            try
            {
                if (_audioSocket == null)
                {
                    _audioSocket = new UdpClient(AudioPort);
                    _controlSocket = new UdpClient(ControlPort);
                    _timingSocket = new UdpClient(TimingPort);
                    Task.Run(() => ReceiveLoop(_audioSocket, "audio"));
                    Task.Run(() => ReceiveLoop(_controlSocket, "control"));
                    Task.Run(() => ReceiveLoop(_timingSocket, "timing"));
                    Log($"UDP sockets open on {AudioPort}/{ControlPort}/{TimingPort}");
                }
            }
            catch (Exception ex)
            {
                Log("Failed to open UDP sockets: " + ex.Message);
            }
        }

        private async Task ReceiveLoop(UdpClient socket, string name)
        {
            System.Security.Cryptography.Aes aes = null;

            if (name == "audio")
            {
                aes = System.Security.Cryptography.Aes.Create();
                aes.Mode = System.Security.Cryptography.CipherMode.CBC;
                aes.Padding = System.Security.Cryptography.PaddingMode.None;
                aes.Key = _aesKey;
                aes.IV = _aesIv;
            }

            // Allocate a large enough buffer for decoded PCM (65536 bytes)
            byte[] pcmBuffer = new byte[65536];

            try
            {
                while (_isRunning)
                {
                    var result = await socket.ReceiveAsync();
                    if (name == "audio" && result.Buffer.Length > 12)
                    {
                        long n = Interlocked.Increment(ref _audioPackets);

                        byte payloadType = (byte)(result.Buffer[1] & 0x7F);

                        // Only decode AirPlay audio payload (type 96)
                        if (payloadType == 96)
                        {
                            int len = result.Buffer.Length - 12;
                            if (len > 0)
                            {
                                int blocks = len / 16;
                                int cipherLen = blocks * 16;
                                if (cipherLen > 0)
                                {
                                    // Update Key and IV in case they changed from a new ANNOUNCE
                                    aes.Key = _aesKey;
                                    aes.IV = _aesIv;
                                    
                                    // IMPORTANT: AirPlay audio resets the AES IV for every packet to the initial IV!
                                    // We MUST create a new decryptor per packet so it doesn't chain CBC state.
                                    using (var packetDecryptor = aes.CreateDecryptor())
                                    {
                                        packetDecryptor.TransformBlock(result.Buffer, 12, cipherLen, result.Buffer, 12);
                                    }
                                }
                                
                                // result.Buffer starts with 12 bytes of RTP header
                                // ALAC payload is at offset 12, length 'len'
                                if (_alacPtr != IntPtr.Zero)
                                {
                                    byte[] alacPayload = new byte[len];
                                    Array.Copy(result.Buffer, 12, alacPayload, 0, len);
                                    
                                    int pcmLen = 0;
                                    NowPlayingViewModel.DecodeAlac(_alacPtr, alacPayload, len, pcmBuffer, out pcmLen);
                                    if (pcmLen > 0 && _waveProvider != null)
                                    {
                                        _waveProvider.AddSamples(pcmBuffer, 0, pcmLen);
                                    }
                                }
                            }
                        }
                        else
                        {
                            // Skip non-audio RTP packets (like Sync packets) on the audio port
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"UDP {name} error: {ex.Message}");
            }
            finally
            {
                aes?.Dispose();
            }
        }

        private void ParseDmapMetadata(byte[] body)
        {
            try
            {
                string title = null;
                string artist = null;
                string album = null;
                int? durationMs = null;
                byte[] pictBytes = null;

                // DMAP tags to parse linearly. Container tags are ignored (we just step into them).
                int i = 0;
                while (i + 8 <= body.Length)
                {
                    string tag = Encoding.ASCII.GetString(body, i, 4);
                    int len = (body[i + 4] << 24) | (body[i + 5] << 16) | (body[i + 6] << 8) | body[i + 7];
                    i += 8;

                    if (i + len > body.Length)
                        break; // Malformed or truncated

                    // List of known DMAP container tags. We step into them by NOT advancing i by len.
                    if (tag == "mlit" || tag == "mdcl" || tag == "mcor" || tag == "mlog" || tag == "mccr")
                    {
                        continue;
                    }

                    if (tag == "minm") title = Encoding.UTF8.GetString(body, i, len);
                    else if (tag == "asar") artist = Encoding.UTF8.GetString(body, i, len);
                    else if (tag == "asal") album = Encoding.UTF8.GetString(body, i, len);
                    else if (tag == "astm" && len == 4) durationMs = (body[i] << 24) | (body[i+1] << 16) | (body[i+2] << 8) | body[i+3];
                    else if (tag == "PICT")
                    {
                        pictBytes = new byte[len];
                        Array.Copy(body, i, pictBytes, 0, len);
                    }
                    
                    i += len;
                }

                if (title != null || artist != null || album != null || durationMs != null || pictBytes != null)
                {
                    if (pictBytes != null && pictBytes.Length > 0)
                    {
                        try
                        {
                            _viewModel.SetAlbumArtFromBytes(pictBytes);
                        }
                        catch (Exception ex)
                        {
                            Log("Failed to process PICT: " + ex.Message);
                        }
                    }

                    if (title != null || artist != null || album != null || (durationMs != null && durationMs > 0))
                    {
                        double finalDuration = (durationMs != null && durationMs > 0) ? durationMs.Value / 1000.0 : -1;
                        _viewModel.UpdateTrackInfo(title, artist, album, finalDuration);
                        
                        if (durationMs != null && durationMs > 0)
                        {
                            _viewModel.DispatcherQueue.TryEnqueue(() => {
                                _viewModel.Position = 0;
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log("Error parsing DMAP: " + ex.Message);
            }
        }
    }
}
