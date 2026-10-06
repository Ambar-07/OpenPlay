#include "AirPlaySession.h"
#include <iostream>

namespace openreceiver {
namespace airplay {

AirPlaySession::AirPlaySession() : state_(SessionState::READY), session_id_("12345678") {}

rtsp::RtspResponse AirPlaySession::handleRequest(const rtsp::RtspRequest& request) {
    std::cout << "[AirPlaySession] Handling " << request.method << " request.\n";
    if (request.method == "OPTIONS") return handleOptions(request);
    if (request.method == "SETUP") return handleSetup(request);
    if (request.method == "RECORD") return handleRecord(request);
    if (request.method == "TEARDOWN") return handleTeardown(request);
    if (request.method == "SET_PARAMETER") return handleSetParameter(request);

    rtsp::RtspResponse response;
    response.status_code = 501;
    response.status_message = "Not Implemented";
    return response;
}

rtsp::RtspResponse AirPlaySession::handleOptions(const rtsp::RtspRequest& request) {
    rtsp::RtspResponse response;
    response.headers["Public"] = "ANNOUNCE, SETUP, RECORD, PAUSE, FLUSH, TEARDOWN, OPTIONS, GET_PARAMETER, SET_PARAMETER, POST, GET";
    
    // In a real implementation, we would extract the challenge and compute the response
    if (request.headers.count("Apple-Challenge")) {
        // Mock response for challenge. Needs crypto in real implementation.
        response.headers["Apple-Response"] = "mock_response_base64"; 
    }
    return response;
}

rtsp::RtspResponse AirPlaySession::handleSetup(const rtsp::RtspRequest& request) {
    state_ = SessionState::SETUP;
    rtsp::RtspResponse response;
    response.headers["Session"] = session_id_;
    // Provide dummy transport parameters for the mock session
    response.headers["Transport"] = "RTP/AVP/UDP;unicast;interleaved=0-1;server_port=6000-6001";
    return response;
}

rtsp::RtspResponse AirPlaySession::handleRecord(const rtsp::RtspRequest& request) {
    state_ = SessionState::RECORDING;
    rtsp::RtspResponse response;
    response.headers["Session"] = session_id_;
    response.headers["Audio-Latency"] = "11025";
    return response;
}

rtsp::RtspResponse AirPlaySession::handleTeardown(const rtsp::RtspRequest& request) {
    state_ = SessionState::TEARDOWN;
    rtsp::RtspResponse response;
    response.headers["Session"] = session_id_;
    return response;
}

rtsp::RtspResponse AirPlaySession::handleSetParameter(const rtsp::RtspRequest& request) {
    rtsp::RtspResponse response;
    // Handle volume, metadata, progress here
    return response;
}

} // namespace airplay
} // namespace openreceiver
