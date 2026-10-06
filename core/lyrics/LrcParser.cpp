#include "LrcParser.h"
#include <sstream>
#include <regex>
#include <algorithm>
#include <iostream>

namespace openreceiver {
namespace lyrics {

ParsedLyrics LrcParser::parse(const std::string& lrcContent) {
    ParsedLyrics parsed;
    parsed.isValid = false;
    
    if (lrcContent.empty()) {
        return parsed;
    }

    std::istringstream stream(lrcContent);
    std::string line;
    
    // Regex to match metadata: [key:value]
    std::regex metaRegex(R"(\[([a-zA-Z]+):([^\]]+)\])");
    // Regex to match timestamps: [mm:ss.xx]
    std::regex timeRegex(R"(\[(\d{2,}):(\d{2}(?:\.\d+)?)\])");

    while (std::getline(stream, line)) {
        if (line.empty() || line.back() == '\r') {
            if (!line.empty()) line.pop_back(); // Handle CRLF
        }
        if (line.empty()) continue;

        // Parse metadata
        std::smatch metaMatch;
        if (std::regex_match(line, metaMatch, metaRegex)) {
            std::string key = metaMatch[1];
            std::string value = metaMatch[2];
            if (key == "ti") parsed.title = value;
            else if (key == "ar") parsed.artist = value;
            else if (key == "al") parsed.album = value;
            continue;
        }

        // Parse timestamps
        std::sregex_iterator words_begin(line.begin(), line.end(), timeRegex);
        std::sregex_iterator words_end;
        
        std::vector<double> timestamps;
        int lastMatchPos = 0;

        for (std::sregex_iterator i = words_begin; i != words_end; ++i) {
            std::smatch match = *i;
            double minutes = std::stod(match[1].str());
            double seconds = std::stod(match[2].str());
            timestamps.push_back(minutes * 60.0 + seconds);
            lastMatchPos = match.position() + match.length();
        }

        if (!timestamps.empty()) {
            // Extract the lyric text which follows the timestamps
            std::string text = line.substr(lastMatchPos);
            // Trim leading spaces
            text.erase(text.begin(), std::find_if(text.begin(), text.end(), [](unsigned char ch) { return !std::isspace(ch); }));

            for (double ts : timestamps) {
                parsed.lines.push_back({ts, text});
            }
            parsed.isValid = true;
        }
    }

    // Sort lines chronologically
    std::sort(parsed.lines.begin(), parsed.lines.end());
    
    return parsed;
}

} // namespace lyrics
} // namespace openreceiver
