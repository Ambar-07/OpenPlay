#pragma once
#include <string>
#include <memory>
#include "../rtsp/RtspMessage.h"

namespace openreceiver {
namespace airplay {

enum class SessionState {
    READY,
    SETUP,
    RECORDING,
    TEARDOWN
};

class AirPlaySession {
public:
    AirPlaySession();
    ~AirPlaySession() = default;

    rtsp::RtspResponse handleRequest(const rtsp::RtspRequest& request);
    SessionState getState() const { return state_; }

private:
    SessionState state_;
    std::string session_id_;

    rtsp::RtspResponse handleOptions(const rtsp::RtspRequest& request);
    rtsp::RtspResponse handleSetup(const rtsp::RtspRequest& request);
    rtsp::RtspResponse handleRecord(const rtsp::RtspRequest& request);
    rtsp::RtspResponse handleTeardown(const rtsp::RtspRequest& request);
    rtsp::RtspResponse handleSetParameter(const rtsp::RtspRequest& request);
};

} // namespace airplay
} // namespace openreceiver
