#include "ArtworkCache.h"

namespace openreceiver {
namespace metadata {

void ArtworkCache::put(const std::string& trackId, const std::vector<uint8_t>& artworkData) {
    std::lock_guard<std::mutex> lock(mutex_);
    cache_[trackId] = artworkData;
}

std::optional<std::vector<uint8_t>> ArtworkCache::get(const std::string& trackId) const {
    std::lock_guard<std::mutex> lock(mutex_);
    auto it = cache_.find(trackId);
    if (it != cache_.end()) {
        return it->second;
    }
    return std::nullopt;
}

void ArtworkCache::clear() {
    std::lock_guard<std::mutex> lock(mutex_);
    cache_.clear();
}

} // namespace metadata
} // namespace openreceiver
