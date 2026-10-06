cmake_minimum_required(VERSION 3.10)
project("openreceiver_core")

set(CMAKE_CXX_STANDARD 17)

# Add the C++ core library directory (relative path from this CMakeLists.txt to the repo root /core)
add_subdirectory(../../../../../../core core_lib_build)

# Create the shared library that JNI will load
add_library(openreceiver_core SHARED
    JniBridge.cpp
)

# Link the core library and Android's logging library
target_link_libraries(openreceiver_core
    core_lib
    log
)
