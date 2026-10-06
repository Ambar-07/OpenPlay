#include "BackgroundGenerator.h"

namespace openreceiver {
namespace metadata {

BackgroundGenerator& BackgroundGenerator::getInstance() {
    static BackgroundGenerator instance;
    return instance;
}

std::future<BackgroundColors> BackgroundGenerator::generateAsync(const std::string& trackId, const std::vector<uint8_t>& imageBytes) {
    {
        std::lock_guard<std::mutex> lock(cacheMutex_);
        if (colorCache_.find(trackId) != colorCache_.end()) {
            BackgroundColors cached = colorCache_[trackId];
            return std::async(std::launch::deferred, [cached]() { return cached; });
        }
    }

    // Run extraction asynchronously (off the critical path)
    return std::async(std::launch::async, [this, trackId, imageBytes]() {
        BackgroundColors colors = extractDominantColors(imageBytes);
        
        std::lock_guard<std::mutex> lock(cacheMutex_);
        colorCache_[trackId] = colors;
        return colors;
    });
}

BackgroundColors BackgroundGenerator::extractDominantColors(const std::vector<uint8_t>& imageBytes) {
    // Stub implementation: 
    // In a real application, this would decode the image (e.g. using stb_image),
    // downsample it, and run a clustering algorithm (like K-Means) to find dominant colors.
    
    // For the stub, we will just return a mocked dark cinematic gradient.
    BackgroundColors colors;
    colors.primary = { 43, 28, 42 };   // Dark purple/red tint #2B1C2A
    colors.secondary = { 17, 17, 17 }; // Near black #111111
    return colors;
}

void BackgroundGenerator::clearCache() {
    std::lock_guard<std::mutex> lock(cacheMutex_);
    colorCache_.clear();
}

} // namespace metadata
} // namespace openreceiver
