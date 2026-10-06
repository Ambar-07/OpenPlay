using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OpenReceiver.ViewModels
{
    public class LrcLine
    {
        public double TimeSeconds { get; set; }
        public string Text { get; set; } = string.Empty;
    }

    public static class LyricsFetcher
    {
        private static readonly HttpClient _httpClient = new HttpClient();

        static LyricsFetcher()
        {
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("OpenReceiver/1.0 (https://github.com/Ambar-07/OpenPlay)");
            _httpClient.Timeout = TimeSpan.FromSeconds(5);
        }

        public static async Task<List<LrcLine>> FetchLyricsAsync(string track, string artist, string album, double duration = 0)
        {
            if (string.IsNullOrWhiteSpace(track)) return null;

            try
            {
                AirPlayServer.Log($"[Lyrics] Searching lyrics for: '{track}' by '{artist}' (Album: '{album}')");

                string cleanTrack = CleanTitle(track);
                string cleanArtist = CleanArtist(artist);

                // Strategy 1: Exact GET with clean track and artist (omitting album for best match rate)
                string url1 = $"https://lrclib.net/api/get?track_name={Uri.EscapeDataString(cleanTrack)}&artist_name={Uri.EscapeDataString(cleanArtist)}";
                var lyrics = await TryFetchFromUrlAsync(url1, duration);
                if (lyrics != null && lyrics.Count > 0)
                {
                    AirPlayServer.Log($"[Lyrics] Found via Strategy 1 (Exact Get): {lyrics.Count} lines");
                    return lyrics;
                }

                // Strategy 2: Search by track_name and artist_name
                string url2 = $"https://lrclib.net/api/search?track_name={Uri.EscapeDataString(cleanTrack)}&artist_name={Uri.EscapeDataString(cleanArtist)}";
                lyrics = await TrySearchFromUrlAsync(url2, duration);
                if (lyrics != null && lyrics.Count > 0)
                {
                    AirPlayServer.Log($"[Lyrics] Found via Strategy 2 (Field Search): {lyrics.Count} lines");
                    return lyrics;
                }

                // Strategy 3: General query search
                string query = $"{cleanTrack} {cleanArtist}".Trim();
                string url3 = $"https://lrclib.net/api/search?q={Uri.EscapeDataString(query)}";
                lyrics = await TrySearchFromUrlAsync(url3, duration);
                if (lyrics != null && lyrics.Count > 0)
                {
                    AirPlayServer.Log($"[Lyrics] Found via Strategy 3 (General Query): {lyrics.Count} lines");
                    return lyrics;
                }

                // Strategy 4: Raw track name search
                if (cleanTrack != track)
                {
                    string url4 = $"https://lrclib.net/api/search?q={Uri.EscapeDataString(track)}";
                    lyrics = await TrySearchFromUrlAsync(url4, duration);
                    if (lyrics != null && lyrics.Count > 0)
                    {
                        AirPlayServer.Log($"[Lyrics] Found via Strategy 4 (Raw Query): {lyrics.Count} lines");
                        return lyrics;
                    }
                }

                AirPlayServer.Log("[Lyrics] No lyrics found on LRCLIB for this song.");
            }
            catch (Exception ex)
            {
                AirPlayServer.Log($"[Lyrics] Error fetching lyrics: {ex.Message}");
            }

            return null;
        }

        private static async Task<List<LrcLine>> TryFetchFromUrlAsync(string url, double duration)
        {
            try
            {
                var resp = await _httpClient.GetAsync(url);
                if (!resp.IsSuccessStatusCode) return null;

                string json = await resp.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.TryGetProperty("syncedLyrics", out var synced) && synced.ValueKind == JsonValueKind.String)
                {
                    string s = synced.GetString();
                    if (!string.IsNullOrWhiteSpace(s)) return ParseLrc(s);
                }

                if (root.TryGetProperty("plainLyrics", out var plain) && plain.ValueKind == JsonValueKind.String)
                {
                    string p = plain.GetString();
                    if (!string.IsNullOrWhiteSpace(p)) return ParsePlainLyrics(p, duration);
                }
            }
            catch (Exception ex)
            {
                AirPlayServer.Log($"[Lyrics] TryFetchFromUrl error: {ex.Message}");
            }
            return null;
        }

        private static async Task<List<LrcLine>> TrySearchFromUrlAsync(string url, double duration)
        {
            try
            {
                var resp = await _httpClient.GetAsync(url);
                if (!resp.IsSuccessStatusCode) return null;

                string json = await resp.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.ValueKind != JsonValueKind.Array) return null;

                string bestPlain = null;

                foreach (var item in doc.RootElement.EnumerateArray())
                {
                    if (item.TryGetProperty("syncedLyrics", out var synced) && synced.ValueKind == JsonValueKind.String)
                    {
                        string s = synced.GetString();
                        if (!string.IsNullOrWhiteSpace(s))
                        {
                            return ParseLrc(s);
                        }
                    }

                    if (bestPlain == null && item.TryGetProperty("plainLyrics", out var plain) && plain.ValueKind == JsonValueKind.String)
                    {
                        string p = plain.GetString();
                        if (!string.IsNullOrWhiteSpace(p))
                        {
                            bestPlain = p;
                        }
                    }
                }

                if (bestPlain != null)
                {
                    return ParsePlainLyrics(bestPlain, duration);
                }
            }
            catch (Exception ex)
            {
                AirPlayServer.Log($"[Lyrics] TrySearchFromUrl error: {ex.Message}");
            }
            return null;
        }

        private static string CleanTitle(string title)
        {
            if (string.IsNullOrWhiteSpace(title)) return "";
            // Remove (feat. ...), [feat. ...], (with ...), - Remastered..., etc.
            string cleaned = Regex.Replace(title, @"\s*[\(\[](feat\.|featuring|with|remastered|remaster|deluxe|version|live)[\s\S]*?[\)\]]", "", RegexOptions.IgnoreCase);
            cleaned = Regex.Replace(cleaned, @"\s*-\s*(remastered|remaster|live|radio edit|deluxe|mono|stereo).*$", "", RegexOptions.IgnoreCase);
            return cleaned.Trim();
        }

        private static string CleanArtist(string artist)
        {
            if (string.IsNullOrWhiteSpace(artist)) return "";
            // If artist contains feat. or commas, take the primary artist
            int commaIdx = artist.IndexOfAny(new[] { ',', '&', ';' });
            if (commaIdx > 0)
            {
                artist = artist.Substring(0, commaIdx);
            }
            string cleaned = Regex.Replace(artist, @"\s*[\(\[](feat\.|featuring)[\s\S]*?[\)\]]", "", RegexOptions.IgnoreCase);
            return cleaned.Trim();
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

        private static List<LrcLine> ParsePlainLyrics(string plainText, double totalDuration)
        {
            var raw = plainText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                              .Select(s => s.Trim())
                              .Where(s => !string.IsNullOrEmpty(s))
                              .ToList();

            if (raw.Count == 0) return null;

            double step = totalDuration > 0 ? (totalDuration / (raw.Count + 1)) : 4.0;
            var lines = new List<LrcLine>();
            for (int i = 0; i < raw.Count; i++)
            {
                lines.Add(new LrcLine
                {
                    TimeSeconds = i * step,
                    Text = raw[i]
                });
            }
            return lines;
        }

        private static bool TryParseTime(string timeStr, out double seconds)
        {
            seconds = 0;
            var parts = timeStr.Split(':');
            if (parts.Length == 2)
            {
                if (int.TryParse(parts[0], out int m) && double.TryParse(parts[1], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double s))
                {
                    seconds = m * 60 + s;
                    return true;
                }
            }
            return false;
        }
    }
}
