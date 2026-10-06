#include "LocalLrcProvider.h"
#include "LrcParser.h"
#include <fstream>
#include <sstream>
#include <algorithm>

namespace openreceiver {
namespace lyrics {

LocalLrcProvider::LocalLrcProvider(const std::string& lyricsDirectory) 
    : lyricsDirectory_(lyricsDirectory) {
    // Ensure directory path ends with a separator
    if (!lyricsDirectory_.empty() && lyricsDirectory_.back() != '/' && lyricsDirectory_.back() != '\\') {
        lyricsDirectory_ += '/';
    }
}

std::string LocalLrcProvider::sanitizeFilename(const std::string& input) const {
    std::string safe = input;
    const std::string invalidChars = "\\/:*?\"<>|";
    for (char& c : safe) {
        if (invalidChars.find(c) != std::string::npos) {
            c = '_';
        }
    }
    return safe;
}

LyricsResult LocalLrcProvider::findLyrics(const state::TrackInfo& track) {
    LyricsResult result;
    result.source = "Local .LRC";
    
    if (track.title.empty() || track.artist.empty()) {
        result.status = LyricsStatus::NOT_FOUND;
        result.errorMessage = "Missing track title or artist.";
        return result;
    }

    std::string filename = sanitizeFilename(track.artist) + " - " + sanitizeFilename(track.title) + ".lrc";
    std::string filepath = lyricsDirectory_ + filename;

    std::ifstream file(filepath);
    if (!file.is_open()) {
        // Try fallback format: just title.lrc
        filename = sanitizeFilename(track.title) + ".lrc";
        filepath = lyricsDirectory_ + filename;
        file.open(filepath);
        
        if (!file.is_open()) {
            result.status = LyricsStatus::NOT_FOUND;
            return result;
        }
    }

    std::stringstream buffer;
    buffer << file.rdbuf();
    std::string content = buffer.str();

    ParsedLyrics parsed = LrcParser::parse(content);
    if (parsed.isValid) {
        result.status = LyricsStatus::FOUND;
        result.syncedLyrics = parsed;
    } else {
        result.status = LyricsStatus::NOT_FOUND;
        result.errorMessage = "Failed to parse local LRC file.";
    }

    return result;
}

} // namespace lyrics
} // namespace openreceiver
