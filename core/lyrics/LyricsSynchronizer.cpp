#include "LyricsSynchronizer.h"
#include <algorithm>

namespace openreceiver {
namespace lyrics {

LyricsSynchronizer::LyricsSynchronizer(const ParsedLyrics& lyrics) 
    : lyrics_(lyrics) {}

int LyricsSynchronizer::getActiveLineIndex(double playbackPositionSeconds) const {
    if (lyrics_.lines.empty()) return -1;
    
    // upper_bound returns the first element strictly greater than playbackPositionSeconds
    auto it = std::upper_bound(lyrics_.lines.begin(), lyrics_.lines.end(), playbackPositionSeconds,
        [](double pos, const LyricLine& line) {
            return pos < line.timestamp;
        });
        
    if (it == lyrics_.lines.begin()) {
        return -1; // Before the first lyric timestamp
    }
    
    // The active line is the one immediately preceding the upper bound
    return std::distance(lyrics_.lines.begin(), it) - 1;
}

std::optional<LyricLine> LyricsSynchronizer::getActiveLine(double playbackPositionSeconds) const {
    int idx = getActiveLineIndex(playbackPositionSeconds);
    if (idx >= 0 && idx < static_cast<int>(lyrics_.lines.size())) {
        return lyrics_.lines[idx];
    }
    return std::nullopt;
}

std::vector<LyricLine> LyricsSynchronizer::getSurroundingLines(int activeIndex, int contextSize) const {
    std::vector<LyricLine> result;
    if (lyrics_.lines.empty()) return result;
    
    // Even if activeIndex is -1 (before first lyric), we might want to show upcoming lines.
    int anchor = std::max(0, activeIndex); 
    
    int startIdx = std::max(0, anchor - contextSize);
    int endIdx = std::min(static_cast<int>(lyrics_.lines.size()) - 1, anchor + contextSize);
    
    for (int i = startIdx; i <= endIdx; ++i) {
        result.push_back(lyrics_.lines[i]);
    }
    
    return result;
}

} // namespace lyrics
} // namespace openreceiver
