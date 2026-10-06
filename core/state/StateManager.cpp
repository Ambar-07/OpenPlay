#include "StateManager.h"
#include <algorithm>

namespace openreceiver {
namespace state {

StateManager& StateManager::getInstance() {
    static StateManager instance;
    return instance;
}

void StateManager::addObserver(IStateObserver* observer) {
    if (!observer) return;
    std::lock_guard<std::mutex> lock(mutex_);
    if (std::find(observers_.begin(), observers_.end(), observer) == observers_.end()) {
        observers_.push_back(observer);
    }
}

void StateManager::removeObserver(IStateObserver* observer) {
    std::lock_guard<std::mutex> lock(mutex_);
    observers_.erase(std::remove(observers_.begin(), observers_.end(), observer), observers_.end());
}

void StateManager::setTrackInfo(const TrackInfo& trackInfo) {
    std::vector<IStateObserver*> observersCopy;
    {
        std::lock_guard<std::mutex> lock(mutex_);
        currentTrack_ = trackInfo;
        observersCopy = observers_;
    }
    for (auto observer : observersCopy) {
        observer->onTrackInfoChanged(trackInfo);
    }
}

void StateManager::setPlaybackState(const PlaybackState& playbackState) {
    std::vector<IStateObserver*> observersCopy;
    {
        std::lock_guard<std::mutex> lock(mutex_);
        currentPlayback_ = playbackState;
        observersCopy = observers_;
    }
    for (auto observer : observersCopy) {
        observer->onPlaybackStateChanged(playbackState);
    }
}

void StateManager::setReceiverState(ReceiverState receiverState) {
    std::vector<IStateObserver*> observersCopy;
    {
        std::lock_guard<std::mutex> lock(mutex_);
        currentReceiverState_ = receiverState;
        observersCopy = observers_;
    }
    for (auto observer : observersCopy) {
        observer->onReceiverStateChanged(receiverState);
    }
}

TrackInfo StateManager::getTrackInfo() const {
    std::lock_guard<std::mutex> lock(mutex_);
    return currentTrack_;
}

PlaybackState StateManager::getPlaybackState() const {
    std::lock_guard<std::mutex> lock(mutex_);
    return currentPlayback_;
}

ReceiverState StateManager::getReceiverState() const {
    std::lock_guard<std::mutex> lock(mutex_);
    return currentReceiverState_;
}

} // namespace state
} // namespace openreceiver
