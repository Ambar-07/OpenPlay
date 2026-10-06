#pragma once
#include <functional>
#include <vector>
#include <mutex>
#include "Models.h"

namespace openreceiver {
namespace state {

class IStateObserver {
public:
    virtual ~IStateObserver() = default;
    virtual void onTrackInfoChanged(const TrackInfo& trackInfo) {}
    virtual void onPlaybackStateChanged(const PlaybackState& playbackState) {}
    virtual void onReceiverStateChanged(ReceiverState receiverState) {}
};

class StateManager {
public:
    static StateManager& getInstance();

    void addObserver(IStateObserver* observer);
    void removeObserver(IStateObserver* observer);

    void setTrackInfo(const TrackInfo& trackInfo);
    void setPlaybackState(const PlaybackState& playbackState);
    void setReceiverState(ReceiverState receiverState);

    TrackInfo getTrackInfo() const;
    PlaybackState getPlaybackState() const;
    ReceiverState getReceiverState() const;

private:
    StateManager() = default;
    ~StateManager() = default;
    
    StateManager(const StateManager&) = delete;
    StateManager& operator=(const StateManager&) = delete;

    mutable std::mutex mutex_;
    TrackInfo currentTrack_;
    PlaybackState currentPlayback_;
    ReceiverState currentReceiverState_ = ReceiverState::DISCONNECTED;

    std::vector<IStateObserver*> observers_;
};

} // namespace state
} // namespace openreceiver
