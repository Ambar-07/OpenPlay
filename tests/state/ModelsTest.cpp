#include <iostream>
#include <cassert>
#include "../../core/state/Models.h"

using namespace openreceiver::state;

void testTrackInfo() {
    TrackInfo track;
    track.title = "Test Title";
    track.artist = "Test Artist";
    track.duration = 210.5;
    
    assert(track.title == "Test Title");
    assert(track.artist == "Test Artist");
    assert(track.duration == 210.5);
    assert(track.album.empty());
    std::cout << "testTrackInfo PASSED\n";
}

void testPlaybackState() {
    PlaybackState playback;
    assert(playback.status == PlaybackStatus::STOPPED);
    
    playback.status = PlaybackStatus::PLAYING;
    playback.position = 10.0;
    playback.duration = 210.5;
    
    assert(playback.status == PlaybackStatus::PLAYING);
    assert(playback.position == 10.0);
    std::cout << "testPlaybackState PASSED\n";
}

void testReceiverState() {
    ReceiverState state = ReceiverState::READY;
    assert(state == ReceiverState::READY);
    state = ReceiverState::RECEIVING;
    assert(state == ReceiverState::RECEIVING);
    std::cout << "testReceiverState PASSED\n";
}

int main() {
    testTrackInfo();
    testPlaybackState();
    testReceiverState();
    std::cout << "All model tests passed successfully.\n";
    return 0;
}
