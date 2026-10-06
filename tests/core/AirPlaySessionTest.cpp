#include <iostream>
#include <cassert>
#include "../../core/airplay/AirPlaySession.h"

using namespace openreceiver;
using namespace openreceiver::airplay;
using namespace openreceiver::rtsp;

void testSessionLifecycle() {
    AirPlaySession session;
    assert(session.getState() == SessionState::READY);

    // 1. OPTIONS
    RtspRequest optionsReq;
    optionsReq.method = "OPTIONS";
    RtspResponse optionsRes = session.handleRequest(optionsReq);
    assert(optionsRes.status_code == 200);
    assert(optionsRes.headers.count("Public"));
    assert(session.getState() == SessionState::READY);

    // 2. SETUP
    RtspRequest setupReq;
    setupReq.method = "SETUP";
    RtspResponse setupRes = session.handleRequest(setupReq);
    assert(setupRes.status_code == 200);
    assert(setupRes.headers.count("Session"));
    assert(setupRes.headers.count("Transport"));
    assert(session.getState() == SessionState::SETUP);

    // 3. RECORD
    RtspRequest recordReq;
    recordReq.method = "RECORD";
    RtspResponse recordRes = session.handleRequest(recordReq);
    assert(recordRes.status_code == 200);
    assert(recordRes.headers.count("Audio-Latency"));
    assert(session.getState() == SessionState::RECORDING);

    // 4. TEARDOWN
    RtspRequest teardownReq;
    teardownReq.method = "TEARDOWN";
    RtspResponse teardownRes = session.handleRequest(teardownReq);
    assert(teardownRes.status_code == 200);
    assert(session.getState() == SessionState::TEARDOWN);

    std::cout << "testSessionLifecycle PASSED\n";
}

int main() {
    testSessionLifecycle();
    std::cout << "All tests passed successfully.\n";
    return 0;
}
