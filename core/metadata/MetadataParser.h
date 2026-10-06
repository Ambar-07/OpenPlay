#pragma once
#include <vector>
#include <cstdint>
#include <string>
#include "../state/Models.h"

namespace openreceiver {
namespace metadata {

class MetadataParser {
public:
    // Parses DMAP/DAAP binary metadata or plist metadata into a normalized TrackInfo
    static state::TrackInfo parse(const std::vector<uint8_t>& payload);

private:
    // Helper function to extract DAAP strings from payload
    static std::string extractTag(const std::vector<uint8_t>& payload, const std::string& tag);
    static double extractIntTag(const std::vector<uint8_t>& payload, const std::string& tag);
};

} // namespace metadata
} // namespace openreceiver
