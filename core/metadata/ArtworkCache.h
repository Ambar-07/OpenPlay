#pragma once
#include <string>
#include <vector>
#include <cstdint>
#include <map>
#include <mutex>
#include <optional>

namespace openreceiver {
namespace metadata {

class ArtworkCache {
public:
    ArtworkCache() = default;
    ~ArtworkCache() = default;

    void put(const std::string& trackId, const std::vector<uint8_t>& artworkData);
    std::optional<std::vector<uint8_t>> get(const std::string& trackId) const;
    void clear();

private:
    // Simple thread-safe cache
    mutable std::mutex mutex_;
    std::map<std::string, std::vector<uint8_t>> cache_;
};

} // namespace metadata
} // namespace openreceiver
