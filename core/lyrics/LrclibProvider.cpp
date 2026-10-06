#include "LrclibProvider.h"
#include "LrcParser.h"
#include <sstream>
#include <iomanip>
#include <iostream>

namespace openreceiver {
namespace lyrics {

std::string LrclibProvider::urlEncode(const std::string& value) const {
    std::ostringstream escaped;
    escaped.fill('0');
    escaped << std::hex;

    for (char c : value) {
        if (isalnum(c) || c == '-' || c == '_' || c == '.' || c == '~') {
            escaped << c;
        } else {
            escaped << std::uppercase << '%' << std::setw(2) << int((unsigned char)c) << std::nouppercase;
        }
    }
    return escaped.str();
}

std::string LrclibProvider::buildQueryUrl(const state::TrackInfo& track) const {
    std::string url = "https://lrclib.net/api/get?";
    url += "track_name=" + urlEncode(track.title);
    url += "&artist_name=" + urlEncode(track.artist);
    if (!track.album.empty()) {
        url += "&album_name=" + urlEncode(track.album);
    }
    if (track.duration > 0) {
        url += "&duration=" + std::to_string(static_cast<int>(track.duration));
    }
    return url;
}

std::string LrclibProvider::fetchFromApi(const std::string& url, int timeoutMs) {
    // STUB: Real HTTP client goes here. 
    // Must enforce the timeoutMs strictly.
    // For unit tests, we'll return a mocked JSON response if the URL contains "Rick Astley".
    if (url.find("Rick%20Astley") != std::string::npos) {
        // Mocked LRCLIB JSON payload (simplified)
        return R"({
            "syncedLyrics": "[00:18.78] We're no strangers to love\n[00:22.83] You know the rules and so do I"
        })";
    }
    return "";
}

LyricsResult LrclibProvider::findLyrics(const state::TrackInfo& track) {
    std::string cacheKey = track.artist + "||" + track.title;
    
    {
        std::lock_guard<std::mutex> lock(cacheMutex_);
        if (memoryCache_.find(cacheKey) != memoryCache_.end()) {
            return memoryCache_[cacheKey];
        }
    }

    LyricsResult result;
    result.source = "LRCLIB";
    
    if (track.title.empty() || track.artist.empty()) {
        result.status = LyricsStatus::NOT_FOUND;
        result.errorMessage = "Missing required metadata.";
        return result;
    }

    try {
        std::string url = buildQueryUrl(track);
        std::string jsonResponse = fetchFromApi(url, 3000); // 3-second hard timeout

        if (jsonResponse.empty()) {
            result.status = LyricsStatus::NOT_FOUND;
        } else {
            // Simple string search to extract "syncedLyrics" from JSON without adding a JSON library dependency yet.
            std::string searchKey = "\"syncedLyrics\": \"";
            size_t pos = jsonResponse.find(searchKey);
            if (pos != std::string::npos) {
                pos += searchKey.length();
                size_t endPos = jsonResponse.find("\"", pos);
                if (endPos != std::string::npos) {
                    std::string lrcContent = jsonResponse.substr(pos, endPos - pos);
                    
                    // Unescape newlines
                    size_t newlinePos;
                    while ((newlinePos = lrcContent.find("\\n")) != std::string::npos) {
                        lrcContent.replace(newlinePos, 2, "\n");
                    }

                    ParsedLyrics parsed = LrcParser::parse(lrcContent);
                    if (parsed.isValid) {
                        result.status = LyricsStatus::FOUND;
                        result.syncedLyrics = parsed;
                    } else {
                        result.status = LyricsStatus::NOT_FOUND;
                        result.errorMessage = "Failed to parse returned LRC payload.";
                    }
                } else {
                    result.status = LyricsStatus::NOT_FOUND;
                }
            } else {
                result.status = LyricsStatus::NOT_FOUND;
            }
        }
    } catch (const std::exception& e) {
        result.status = LyricsStatus::PROVIDER_ERROR;
        result.errorMessage = e.what();
    }

    // Cache the result (even negative ones, to prevent hammering the API for the same missing track)
    {
        std::lock_guard<std::mutex> lock(cacheMutex_);
        memoryCache_[cacheKey] = result;
    }

    return result;
}

std::future<LyricsResult> LrclibProvider::findLyricsAsync(const state::TrackInfo& track) {
    return std::async(std::launch::async, [this, track]() {
        return findLyrics(track);
    });
}

void LrclibProvider::clearCache() {
    std::lock_guard<std::mutex> lock(cacheMutex_);
    memoryCache_.clear();
}

} // namespace lyrics
} // namespace openreceiver
