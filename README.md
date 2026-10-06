# OpenReceiver

A "clean-room", cross-platform AirPlay audio receiver written in modern C++17, designed specifically for **Windows 11** and **Android TV**.

OpenReceiver focuses exclusively on providing a high-fidelity audio pipeline and a premium **Cinematic Now Playing UX** (with synchronized lyrics and dominant-color artwork backgrounds), avoiding the GPL contamination risks associated with older AirPlay 1 mirroring implementations.

## Features
- **Clean-Room Protocol Implementation**: Safely parses RTSP Options, Setup, Record, and Teardown handshakes without relying on GPLv3 codebases.
- **Cinematic UI**: Extracts album artwork natively, computes dominant background gradients, and renders full-screen metadata.
- **Lyrics Engine**: Asynchronously fetches lyrics from [LRCLIB](https://lrclib.net/) (with local `.lrc` fallback) and synchronizes them to the playback position in real-time.
- **Native Platform Integrations**: 
  - **Windows**: WinUI 3 shell, System Tray background lifecycle, and Inno Setup automated firewall rules.
  - **Android TV**: Kotlin Leanback UI, JNI native bridge, and background service lifecycle.

## Project Structure
- `/core`: The platform-agnostic C++17 AirPlay server, RTSP parsers, and Lyrics Engine.
- `/windows/native`: C++ Bridge exporting core functionality to C#.
- `/windows/app`: C# WinUI 3 frontend application and Tray Manager.
- `/windows/packaging`: Standalone `.iss` installer config.
- `/android/app/src/main`: Kotlin Leanback frontend, XML layouts, and JNI bindings.
- `/tests`: CTest unit test suite for the core C++ modules.
- `/docs`: Technical specifications, architectural decisions, and build guides.

## Building the Project
See [docs/BUILDING.md](docs/BUILDING.md) for step-by-step instructions on how to compile the C++ core and construct the UI for your target platform.

## Troubleshooting
AirPlay relies heavily on zero-configuration networking (mDNS/Bonjour). If you are experiencing discovery issues, please refer to [docs/TROUBLESHOOTING.md](docs/TROUBLESHOOTING.md).

## License
MIT License. See `LICENSE` for more details.
