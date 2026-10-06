#pragma once
#include "ILyricsProvider.h"
#include <string>
#include <map>
#include <mutex>
#include <future>

namespace openreceiver {
namespace lyrics {

class LrclibProvider : public ILyricsProvider {
public:
    LrclibProvider() = default;
    ~LrclibProvider() override = default;

    // Synchronous call; bounded by a strict timeout to prevent blocking.
    LyricsResult findLyrics(const state::TrackInfo& track) override;

    // Asynchronous version for the UI to consume without blocking the main/audio threads.
    std::future<LyricsResult> findLyricsAsync(const state::TrackInfo& track);

    void clearCache();

private:
    std::string buildQueryUrl(const state::TrackInfo& track) const;
    std::string urlEncode(const std::string& value) const;
    
    // Abstracted HTTP fetch to allow injecting a mock or real HTTP client.
    // In production, this would use libcurl, WinHTTP, or platform APIs.
    virtual std::string fetchFromApi(const std::string& url, int timeoutMs);

    mutable std::mutex cacheMutex_;
    std::map<std::string, LyricsResult> memoryCache_;
};

} // namespace lyrics
} // namespace openreceiver
