#pragma once
#include <string>
#include <map>

namespace openreceiver {
namespace rtsp {

struct RtspRequest {
    std::string method;
    std::string uri;
    std::string version;
    std::map<std::string, std::string> headers;
    std::string body;
};

struct RtspResponse {
    std::string version = "RTSP/1.0";
    int status_code = 200;
    std::string status_message = "OK";
    std::map<std::string, std::string> headers;
    std::string body;
};

} // namespace rtsp
} // namespace openreceiver
