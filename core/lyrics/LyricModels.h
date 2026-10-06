#pragma once
#include <string>
#include <vector>

namespace openreceiver {
namespace lyrics {

struct LyricLine {
    double timestamp; // in seconds
    std::string text;

    bool operator<(const LyricLine& other) const {
        return timestamp < other.timestamp;
    }
};

struct ParsedLyrics {
    std::string title;
    std::string artist;
    std::string album;
    std::vector<LyricLine> lines; // Should be sorted by timestamp
    bool isValid = false;
};

} // namespace lyrics
} // namespace openreceiver
