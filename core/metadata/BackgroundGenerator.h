#pragma once
#include <vector>
#include <string>
#include <cstdint>
#include <future>
#include <map>
#include <mutex>

namespace openreceiver {
namespace metadata {

struct Color {
    uint8_t r, g, b;
};

struct BackgroundColors {
    Color primary;
    Color secondary;
};

class BackgroundGenerator {
public:
    static BackgroundGenerator& getInstance();

    // Asynchronously extracts dominant colors from image bytes.
    // Uses a cache so identical trackIds return instantly.
    std::future<BackgroundColors> generateAsync(const std::string& trackId, const std::vector<uint8_t>& imageBytes);

    void clearCache();

private:
    BackgroundGenerator() = default;
    ~BackgroundGenerator() = default;
    BackgroundGenerator(const BackgroundGenerator&) = delete;
    BackgroundGenerator& operator=(const BackgroundGenerator&) = delete;

    BackgroundColors extractDominantColors(const std::vector<uint8_t>& imageBytes);

    mutable std::mutex cacheMutex_;
    std::map<std::string, BackgroundColors> colorCache_;
};

} // namespace metadata
} // namespace openreceiver
