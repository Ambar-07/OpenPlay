#pragma once
#include "LyricModels.h"
#include <vector>
#include <optional>

namespace openreceiver {
namespace lyrics {

class LyricsSynchronizer {
public:
    explicit LyricsSynchronizer(const ParsedLyrics& lyrics);
    ~LyricsSynchronizer() = default;

    // Returns the index of the active lyric line, or -1 if before the first line
    int getActiveLineIndex(double playbackPositionSeconds) const;

    // Returns the active lyric line if it exists
    std::optional<LyricLine> getActiveLine(double playbackPositionSeconds) const;

    // Helper to get surrounding lines for UI rendering
    std::vector<LyricLine> getSurroundingLines(int activeIndex, int contextSize) const;

    const ParsedLyrics& getLyrics() const { return lyrics_; }

private:
    ParsedLyrics lyrics_;
};

} // namespace lyrics
} // namespace openreceiver
