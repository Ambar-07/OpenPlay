# Core libraries for OpenReceiver
add_library(core_lib STATIC
    airplay/AirPlaySession.cpp
    metadata/ArtworkCache.cpp
    metadata/MetadataParser.cpp
    state/StateManager.cpp
)
target_include_directories(core_lib PUBLIC .)
