using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace OpenReceiver.ViewModels
{
    public class LrcLine
    {
        public double TimeSeconds { get; set; }
        public string Text { get; set; }
    }

    public class LrcLibResponse
    {
        [JsonPropertyName("syncedLyrics")]
        public string SyncedLyrics { get; set; }
    }

    public static class LyricsFetcher
    {
        private static readonly HttpClient _httpClient = new HttpClient();

        public static async Task<List<LrcLine>> FetchLyricsAsync(string track, string artist, string album)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(track)) return null;
                
                // Give user agent to respect LRCLIB API guidelines
                if (!_httpClient.DefaultRequestHeaders.UserAgent.TryParseAdd("OpenReceiver/1.0"))
                {
                    _httpClient.DefaultRequestHeaders.Add("User-Agent", "OpenReceiver/1.0 (github.com)");
                }

                var url = $"https://lrclib.net/api/get?track_name={Uri.EscapeDataString(track)}&artist_name={Uri.EscapeDataString(artist ?? "")}&album_name={Uri.EscapeDataString(album ?? "")}";
                var response = await _httpClient.GetAsync(url);
                
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<LrcLibResponse>();
                    if (!string.IsNullOrEmpty(result?.SyncedLyrics))
                    {
                        return ParseLrc(result.SyncedLyrics);
                    }
                }
                
                // Fallback to search API if get fails
                var searchUrl = $"https://lrclib.net/api/search?q={Uri.EscapeDataString((track + " " + artist).Trim())}";
                var searchResponse = await _httpClient.GetAsync(searchUrl);
                if (searchResponse.IsSuccessStatusCode)
                {
                    var results = await searchResponse.Content.ReadFromJsonAsync<List<LrcLibResponse>>();
                    var firstSynced = results?.FirstOrDefault(r => !string.IsNullOrEmpty(r.SyncedLyrics));
                    if (firstSynced != null)
                    {
                        return ParseLrc(firstSynced.SyncedLyrics);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to fetch lyrics: {ex.Message}");
            }
            return null;
        }

        private static List<LrcLine> ParseLrc(string lrcData)
        {
            var lines = new List<LrcLine>();
            var rawLines = lrcData.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            
            foreach (var rawLine in rawLines)
            {
                if (rawLine.StartsWith("[") && rawLine.IndexOf("]") > 0)
                {
                    int endBracket = rawLine.IndexOf("]");
                    string timeStr = rawLine.Substring(1, endBracket - 1);
                    string text = rawLine.Substring(endBracket + 1).Trim();
                    
                    if (TryParseTime(timeStr, out double seconds))
                    {
                        lines.Add(new LrcLine { TimeSeconds = seconds, Text = text });
                    }
                }
            }
            
            return lines.OrderBy(l => l.TimeSeconds).ToList();
        }

        private static bool TryParseTime(string timeStr, out double seconds)
        {
            seconds = 0;
            // timeStr format: "mm:ss.xx" or "mm:ss"
            var parts = timeStr.Split(':');
            if (parts.Length == 2)
            {
                if (int.TryParse(parts[0], out int m) && double.TryParse(parts[1], out double s))
                {
                    seconds = m * 60 + s;
                    return true;
                }
            }
            return false;
        }
    }
}
