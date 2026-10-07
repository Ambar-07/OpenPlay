package com.openreceiver.lyrics

import android.util.Log
import org.json.JSONArray
import org.json.JSONObject
import java.net.HttpURLConnection
import java.net.URL
import java.net.URLEncoder

data class LyricLine(val timeSeconds: Double, val text: String)

/**
 * Robust port of windows/OpenReceiverApp/ViewModels/LyricsFetcher.cs.
 * Features 4-tier search fallback, synced lyrics parsing, and plain lyrics fallback.
 * Blocking call — must be invoked off the main/UI thread.
 */
object LyricsFetcher {

    private const val TAG = "LyricsFetcher"

    fun fetch(track: String, artist: String, duration: Double): List<LyricLine>? {
        if (track.isBlank()) return null
        val cleanTrack = cleanTitle(track)
        val cleanArtist = cleanArtist(artist)

        Log.d(TAG, "Fetching lyrics for '$cleanTrack' by '$cleanArtist' (Raw: '$track' / '$artist')")

        try {
            // Strategy 1: Exact GET with clean track and artist
            val url1 = "https://lrclib.net/api/get?track_name=${enc(cleanTrack)}&artist_name=${enc(cleanArtist)}"
            getExact(url1, duration)?.let {
                Log.i(TAG, "Lyrics found via Strategy 1 (Exact GET): ${it.size} lines")
                return it
            }

            // Strategy 2: Field search by track_name and artist_name
            val url2 = "https://lrclib.net/api/search?track_name=${enc(cleanTrack)}&artist_name=${enc(cleanArtist)}"
            search(url2, duration)?.let {
                Log.i(TAG, "Lyrics found via Strategy 2 (Field Search): ${it.size} lines")
                return it
            }

            // Strategy 3: General query search
            val query = "$cleanTrack $cleanArtist".trim()
            val url3 = "https://lrclib.net/api/search?q=${enc(query)}"
            search(url3, duration)?.let {
                Log.i(TAG, "Lyrics found via Strategy 3 (General Query): ${it.size} lines")
                return it
            }

            // Strategy 4: Raw track name search if title was modified by cleaner
            if (cleanTrack != track) {
                val url4 = "https://lrclib.net/api/search?q=${enc(track)}"
                search(url4, duration)?.let {
                    Log.i(TAG, "Lyrics found via Strategy 4 (Raw Query): ${it.size} lines")
                    return it
                }
            }
        } catch (e: Exception) {
            Log.w(TAG, "Lyrics fetch exception: ${e.message}")
        }

        Log.i(TAG, "No lyrics found on LRCLIB for '$track' by '$artist'")
        return null
    }

    private fun enc(s: String): String =
        URLEncoder.encode(s, "UTF-8").replace("+", "%20")

    private fun getExact(url: String, duration: Double): List<LyricLine>? {
        val body = httpGet(url) ?: return null
        val obj = try { JSONObject(body) } catch (_: Exception) { return null }
        obj.optStringOrNull("syncedLyrics")?.let { s ->
            parseLrc(s).takeIf { it.isNotEmpty() }?.let { return it }
        }
        obj.optStringOrNull("plainLyrics")?.let {
            return parsePlain(it, duration)
        }
        return null
    }

    private fun search(url: String, duration: Double): List<LyricLine>? {
        val body = httpGet(url) ?: return null
        val arr = try { JSONArray(body) } catch (_: Exception) { return null }
        var bestPlain: String? = null

        for (i in 0 until arr.length()) {
            val item = arr.optJSONObject(i) ?: continue
            item.optStringOrNull("syncedLyrics")?.let { s ->
                parseLrc(s).takeIf { it.isNotEmpty() }?.let { return it }
            }
            if (bestPlain == null) {
                bestPlain = item.optStringOrNull("plainLyrics")
            }
        }
        return bestPlain?.let { parsePlain(it, duration) }
    }

    private fun JSONObject.optStringOrNull(key: String): String? {
        if (!has(key) || isNull(key)) return null
        val value = optString(key)
        return if (value.isNotBlank() && value != "null") value else null
    }

    private fun httpGet(url: String): String? {
        var conn: HttpURLConnection? = null
        return try {
            conn = URL(url).openConnection() as HttpURLConnection
            conn.connectTimeout = 4000
            conn.readTimeout = 4000
            conn.setRequestProperty("User-Agent", "OpenReceiver/1.0 (https://github.com/Ambar-07/OpenPlay)")
            val code = conn.responseCode
            if (code == 200) {
                conn.inputStream.bufferedReader().use { it.readText() }
            } else {
                null
            }
        } catch (e: Exception) {
            null
        } finally {
            conn?.disconnect()
        }
    }

    private val featRegex = Regex("""\s*[(\[](feat\.|featuring|with|remastered|remaster|deluxe|version|live)[\s\S]*?[)\]]""", RegexOption.IGNORE_CASE)
    private val suffixRegex = Regex("""\s*-\s*(remastered|remaster|live|radio edit|deluxe|mono|stereo).*$""", RegexOption.IGNORE_CASE)
    private val artistFeatRegex = Regex("""\s*[(\[](feat\.|featuring)[\s\S]*?[)\]]""", RegexOption.IGNORE_CASE)

    private fun cleanTitle(title: String): String =
        suffixRegex.replace(featRegex.replace(title, ""), "").trim()

    private fun cleanArtist(artist: String): String {
        var a = artist
        val idx = a.indexOfAny(charArrayOf(',', '&', ';'))
        if (idx > 0) a = a.substring(0, idx)
        return artistFeatRegex.replace(a, "").trim()
    }

    private fun parseLrc(data: String): List<LyricLine> {
        val out = ArrayList<LyricLine>()
        for (raw in data.split('\r', '\n')) {
            val line = raw.trim()
            if (!line.startsWith("[")) continue
            val end = line.indexOf(']')
            if (end <= 0) continue
            val timeStr = line.substring(1, end)
            val parts = timeStr.split(':')
            if (parts.size != 2) continue
            val m = parts[0].toIntOrNull() ?: continue
            val s = parts[1].toDoubleOrNull() ?: continue
            val text = line.substring(end + 1).trim()
            out.add(LyricLine(m * 60.0 + s, text))
        }
        return out.sortedBy { it.timeSeconds }
    }

    private fun parsePlain(text: String, duration: Double): List<LyricLine>? {
        val raw = text.split('\r', '\n').map { it.trim() }.filter { it.isNotEmpty() }
        if (raw.isEmpty()) return null
        val step = if (duration > 0) duration / (raw.size + 1) else 4.0
        return raw.mapIndexed { i, t -> LyricLine(i * step, t) }
    }
}
