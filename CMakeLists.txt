cmake_minimum_required(VERSION 3.14)
project(OpenReceiver VERSION 0.1.0 LANGUAGES CXX C)

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

# Add subdirectories
add_subdirectory(core)
add_subdirectory(windows)
add_subdirectory(tests)
