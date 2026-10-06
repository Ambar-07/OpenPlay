#pragma once
#include <string>
#include <vector>
#include <cstdint>

namespace openreceiver {
namespace state {

struct TrackInfo {
    std::string title;
    std::string artist;
    std::string album;
    std::string artworkUri;
    std::vector<uint8_t> artworkBytes;
    double duration = 0.0;
    std::string trackId;
    std::string persistentId;
    std::string albumId;
    std::string artistId;
    std::string quality;
    std::string codecHints;
};

enum class PlaybackStatus {
    STOPPED,
    PLAYING,
    PAUSED
};

struct PlaybackState {
    PlaybackStatus status = PlaybackStatus::STOPPED;
    double position = 0.0;
    double duration = 0.0;
    double volume = 1.0;
    double rate = 1.0;
};

enum class ReceiverState {
    READY,
    DISCOVERING,
    CONNECTING,
    RECEIVING,
    RECONNECTING,
    ERROR_STATE,
    DISCONNECTED
};

} // namespace state
} // namespace openreceiver
