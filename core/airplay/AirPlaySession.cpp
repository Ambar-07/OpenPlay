#include "AirPlaySession.h"
#include <iostream>
#include <string>
#include <vector>
#include <algorithm>
#include <cstring>
#include <openssl/rsa.h>
#include <openssl/pem.h>
#include <openssl/bio.h>
#include <openssl/err.h>
#include <openssl/evp.h>

// The Shairport global RSA Private Key (Truncated for now)
static const char* private_key_pem = 
"-----BEGIN RSA PRIVATE KEY-----\n"
"MIIEpQIBAAKCAQEA59dE8qLieItsH1WgjrcFRKj6eUWqi+bGLOX1HL3U3GhC/j0Qg90u3sG/1CUt\n"
"wC5vOYvfaiFicWEbVG3dx0E2yCRptxEo09XGUI7x67sjoYpN03g4m/T0hOEKM5f17yBpsC4nC930\n"
"OQhH+O5OQ32F8ZqNqE5I/I90/C7h9G8Uv+Vb+yYpA//9yP0nB5T//L//s/b//30///hX//\n" // TODO: Paste Full Key Here
"-----END RSA PRIVATE KEY-----\n";

namespace openreceiver {
namespace airplay {

// Helper to base64 decode
static int base64_decode(const std::string& input, unsigned char* output) {
    return EVP_DecodeBlock(output, (const unsigned char*)input.c_str(), input.length());
}

// Helper to base64 encode
static std::string base64_encode(const unsigned char* input, int length) {
    // EVP_EncodeBlock adds a null terminator, so allocate enough space
    int expected_len = 4 * ((length + 2) / 3);
    std::vector<unsigned char> output(expected_len + 1);
    int encoded_len = EVP_EncodeBlock(output.data(), input, length);
    return std::string((char*)output.data(), encoded_len);
}

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
    
    if (request.headers.count("Apple-Challenge")) {
        std::cout << "[AirPlaySession] Received Apple-Challenge. Encrypting with OpenSSL...\n";
        
        std::string challenge_b64 = request.headers.at("Apple-Challenge");
        unsigned char challenge[256] = {0};
        int challenge_len = base64_decode(challenge_b64, challenge);
        
        // Strip base64 padding bytes if any (EVP_DecodeBlock doesn't handle padding exactly as we want)
        while(challenge_len > 0 && challenge_b64.back() == '=' && challenge[challenge_len - 1] == 0) {
            challenge_len--;
        }

        // Build 32-byte payload: 16 bytes challenge + 16 bytes zeros (dummy IP/MAC)
        unsigned char payload[32] = {0};
        memcpy(payload, challenge, std::min(challenge_len, 16));

        // Load RSA Private Key
        BIO* bio = BIO_new_mem_buf(private_key_pem, -1);
        RSA* rsa = PEM_read_bio_RSAPrivateKey(bio, NULL, NULL, NULL);
        BIO_free(bio);

        if (rsa) {
            unsigned char encrypted[256];
            int encrypted_len = RSA_private_encrypt(32, payload, encrypted, rsa, RSA_PKCS1_PADDING);
            if (encrypted_len != -1) {
                response.headers["Apple-Response"] = base64_encode(encrypted, encrypted_len);
            } else {
                std::cout << "[AirPlaySession] RSA Encryption failed!\n";
            }
            RSA_free(rsa);
        } else {
            std::cout << "[AirPlaySession] Failed to load RSA Private Key (Probably truncated dummy key).\n";
        }
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
