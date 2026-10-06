#include "Bridge.h"
#include "../../core/state/StateManager.h"

using namespace openreceiver::state;

class BridgeObserver : public IStateObserver {
public:
    TrackInfoCallback trackCb = nullptr;
    PlaybackStateCallback playCb = nullptr;

    void onTrackInfoChanged(const TrackInfo& track) override {
        if (trackCb) {
            trackCb(track.title.c_str(), track.artist.c_str(), track.album.c_str(), track.duration);
        }
    }

    void onPlaybackStateChanged(const PlaybackState& state) override {
        if (playCb) {
            playCb(static_cast<int>(state.status), state.position);
        }
    }
};

static BridgeObserver g_observer;

extern "C" {

__declspec(dllexport) void InitializeBridge() {
    StateManager::getInstance().addObserver(&g_observer);
}

__declspec(dllexport) void SetTrackInfoCallback(TrackInfoCallback callback) {
    g_observer.trackCb = callback;
}

__declspec(dllexport) void SetPlaybackStateCallback(PlaybackStateCallback callback) {
    g_observer.playCb = callback;
}

}
