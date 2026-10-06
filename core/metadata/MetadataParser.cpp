#include "MetadataParser.h"
#include <cstring>
#include <iostream>

namespace openreceiver {
namespace metadata {

state::TrackInfo MetadataParser::parse(const std::vector<uint8_t>& payload) {
    state::TrackInfo track;

    // This is a simplified mock implementation of a DMAP/DAAP parser.
    // In a real scenario, this iterates through the chunks (Type, Length, Data).
    track.title = extractTag(payload, "minm");
    track.artist = extractTag(payload, "asar");
    track.album = extractTag(payload, "asal");
    
    // Duration is usually in milliseconds in DAAP tag 'astm'
    double durationMs = extractIntTag(payload, "astm");
    if (durationMs > 0) {
        track.duration = durationMs / 1000.0;
    }

    return track;
}

std::string MetadataParser::extractTag(const std::vector<uint8_t>& payload, const std::string& tag) {
    // Stub: search for tag in binary payload (mock approach)
    if (tag.length() != 4) return "";
    
    for (size_t i = 0; i + 8 <= payload.size(); ++i) {
        if (payload[i] == tag[0] && payload[i+1] == tag[1] && payload[i+2] == tag[2] && payload[i+3] == tag[3]) {
            uint32_t len = (payload[i+4] << 24) | (payload[i+5] << 16) | (payload[i+6] << 8) | payload[i+7];
            if (i + 8 + len <= payload.size()) {
                return std::string(reinterpret_cast<const char*>(&payload[i+8]), len);
            }
        }
    }
    return "";
}

double MetadataParser::extractIntTag(const std::vector<uint8_t>& payload, const std::string& tag) {
    // Stub integer extraction for DAAP tags
    if (tag.length() != 4) return 0.0;
    
    for (size_t i = 0; i + 8 <= payload.size(); ++i) {
        if (payload[i] == tag[0] && payload[i+1] == tag[1] && payload[i+2] == tag[2] && payload[i+3] == tag[3]) {
            uint32_t len = (payload[i+4] << 24) | (payload[i+5] << 16) | (payload[i+6] << 8) | payload[i+7];
            if (len == 4 && i + 8 + len <= payload.size()) {
                uint32_t val = (payload[i+8] << 24) | (payload[i+9] << 16) | (payload[i+10] << 8) | payload[i+11];
                return static_cast<double>(val);
            }
        }
    }
    return 0.0;
}

} // namespace metadata
} // namespace openreceiver
