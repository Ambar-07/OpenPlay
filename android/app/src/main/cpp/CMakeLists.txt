cmake_minimum_required(VERSION 3.10)
project("openreceiver_core")

set(CMAKE_CXX_STANDARD 17)

# Add the C++ core library directory (relative path from this CMakeLists.txt to the repo root /core)
add_library(openreceiver_core SHARED
    JniBridge.cpp
    ../../../../../core/state/StateManager.cpp
    ../../../../../core/airplay/alac.c
)

target_include_directories(openreceiver_core PRIVATE
    ../../../../../core
)

# Link Android's logging library
target_link_libraries(openreceiver_core
    log
)
