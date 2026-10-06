#include <iostream>
#include <cassert>
#include "../../core/state/StateManager.h"

using namespace openreceiver::state;

class MockObserver : public IStateObserver {
public:
    int trackUpdates = 0;
    int playbackUpdates = 0;
    int receiverUpdates = 0;

    void onTrackInfoChanged(const TrackInfo& trackInfo) override {
        trackUpdates++;
    }
    void onPlaybackStateChanged(const PlaybackState& playbackState) override {
        playbackUpdates++;
    }
    void onReceiverStateChanged(ReceiverState receiverState) override {
        receiverUpdates++;
    }
};

void testStateManager() {
    auto& manager = StateManager::getInstance();
    MockObserver observer;
    
    manager.addObserver(&observer);
    
    TrackInfo track;
    track.title = "Observer Test";
    manager.setTrackInfo(track);
    assert(observer.trackUpdates == 1);
    
    PlaybackState playback;
    playback.status = PlaybackStatus::PLAYING;
    manager.setPlaybackState(playback);
    assert(observer.playbackUpdates == 1);
    
    manager.setReceiverState(ReceiverState::RECEIVING);
    assert(observer.receiverUpdates == 1);
    
    manager.removeObserver(&observer);
    manager.setReceiverState(ReceiverState::READY);
    // Should not increment after removal
    assert(observer.receiverUpdates == 1);

    std::cout << "testStateManager PASSED\n";
}

int main() {
    testStateManager();
    std::cout << "All StateManager tests passed successfully.\n";
    return 0;
}
