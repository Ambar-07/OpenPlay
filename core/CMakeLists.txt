# Core libraries for OpenReceiver
add_library(core_lib STATIC
    airplay/AirPlaySession.cpp
    metadata/ArtworkCache.cpp
    metadata/MetadataParser.cpp
    metadata/BackgroundGenerator.cpp
    state/StateManager.cpp
    lyrics/LrcParser.cpp
    lyrics/LocalLrcProvider.cpp
)
target_include_directories(core_lib PUBLIC .)
