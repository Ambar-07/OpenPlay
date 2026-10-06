# Core libraries for OpenReceiver
add_library(core_lib STATIC
    airplay/AirPlaySession.cpp
    metadata/ArtworkCache.cpp
    metadata/MetadataParser.cpp
    metadata/BackgroundGenerator.cpp
    state/StateManager.cpp
    lyrics/LrcParser.cpp
    lyrics/LocalLrcProvider.cpp
    lyrics/LrclibProvider.cpp
    lyrics/LyricsSynchronizer.cpp
)
target_include_directories(core_lib PUBLIC .)

# Find and link AirPlay protocol dependencies
find_package(OpenSSL REQUIRED)
find_package(libplist-2.0 CONFIG REQUIRED)

target_link_libraries(core_lib
    PUBLIC
    OpenSSL::SSL
    OpenSSL::Crypto
    libplist-2.0::plist-2.0
)
