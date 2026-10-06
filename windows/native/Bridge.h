#pragma once
#include <cstdint>

extern "C" {

typedef void (*TrackInfoCallback)(const char* title, const char* artist, const char* album, double duration);
typedef void (*PlaybackStateCallback)(int status, double position);

__declspec(dllexport) void InitializeBridge();
__declspec(dllexport) void SetTrackInfoCallback(TrackInfoCallback callback);
__declspec(dllexport) void SetPlaybackStateCallback(PlaybackStateCallback callback);

}
