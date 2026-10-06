# OpenReceiver

OpenReceiver is a cross-platform AirPlay audio receiver implemented in modern C++17. It is built using a clean-room approach, ensuring no GPLv3 contamination, making it suitable for integration into proprietary products. The receiver is specifically tailored for Windows 11 and Android TV, focusing on high-fidelity audio playback and a cinematic metadata-driven user interface.

## Architecture Overview

The codebase is strictly segregated into a platform-agnostic C++ core and native UI shells.

- **Core Engine (`/core`)**: Implements the AirPlay state machine, zero-configuration networking discovery (mDNS), and RTSP handshakes. 
- **Lyrics Engine**: Asynchronously fetches and caches time-synced `.lrc` files from local storage or LRCLIB, parsing and synchronizing them to the current playback position in O(log n) time.
- **Windows Integration (`/windows`)**: Provides a native WinUI 3 interface and system tray lifecycle management. The C++ core is bridged to C# via P/Invoke.
- **Android TV Integration (`/android`)**: Features a Kotlin-based Leanback UI optimized for 10-foot viewing distances, bridging the core engine via JNI.

## Building and Compiling

OpenReceiver must be compiled for the target platform using its respective native toolchain. 

- For detailed build instructions across platforms, refer to [BUILDING.md](docs/BUILDING.md).
- The core C++ module includes a comprehensive suite of unit tests, runnable via CMake and CTest.

## Discovery and Networking

AirPlay relies heavily on multicast DNS (mDNS) over UDP port 5353. Depending on the target environment, firewall configuration or router-level client isolation may interfere with device discovery.

- For network debugging and firewall configuration guidelines, refer to [TROUBLESHOOTING.md](docs/TROUBLESHOOTING.md).

## License

This project is licensed under the MIT License. See the `LICENSE` file for details.
