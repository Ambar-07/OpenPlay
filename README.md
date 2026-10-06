<div align="center">
  <h1>OpenReceiver</h1>
  <p><b>A Clean-Room, High-Fidelity AirPlay Audio Receiver for Windows & Android TV</b></p>
  
  <a href="https://github.com/Ambar-07/OpenPlay/blob/main/LICENSE">
    <img src="https://img.shields.io/badge/License-MIT-blue.svg" alt="License">
  </a>
  <a href="https://isocpp.org/">
    <img src="https://img.shields.io/badge/Standard-C%2B%2B17-blue.svg" alt="C++17">
  </a>
  <img src="https://img.shields.io/badge/Platform-Windows%2011%20%7C%20Android%20TV-lightgrey.svg" alt="Platform">
</div>

<br/>

OpenReceiver is a modern, cross-platform AirPlay audio receiver implemented entirely from scratch in **C++17**. Designed under strict "clean-room" principles to ensure zero GPLv3 contamination, it is safe for proprietary commercial integration. 

Unlike legacy AirPlay mirroring forks, OpenReceiver focuses exclusively on providing an uncompromising, high-fidelity audio pipeline paired with a **Cinematic UX** that feels native to modern living room displays.

---

## ⚡ Core Architecture

The repository is strictly segregated to ensure absolute separation between the protocol parsing layer and the platform-specific UI shells.

- **`core/` (C++17 Engine)**
  - Implements the complete AirPlay state machine.
  - Handles Zero-Configuration Networking (mDNS) discovery.
  - Safely parses and negotiates RTSP handshakes (Options, Setup, Record, Teardown).
  - Provides the `LyricsSynchronizer` and `ILyricsProvider` interfaces.

- **`windows/` (WinUI 3 Shell)**
  - Native Windows 11 integration using C# and WinUI 3.
  - Background lifecycle management via the System Tray (`TrayManager.cs`).
  - C++ `core` integration via a P/Invoke Native Bridge.

- **`android/` (Android TV Shell)**
  - Optimized Kotlin Leanback UI for 10-foot viewing experiences.
  - Asynchronous background service lifecycle.
  - JNI bindings directly to the C++ core (`NativeBridge.kt`).

---

## ✨ Standout Features

### Cinematic "Now Playing" Experience
OpenReceiver doesn't just play audio; it visualizes it. The UI extracts album artwork over the protocol, computes a dominant-color background gradient, and displays gorgeous, full-screen metadata tailored for high-resolution displays.

### Real-Time Synchronized Lyrics
The integrated **Lyrics Engine** intercepts incoming track metadata and asynchronously fetches lyrics.
- **Primary Source**: Uses the [LRCLIB API](https://lrclib.net/) to fetch cryptographically matched time-synced lyrics.
- **Fallback Source**: Falls back to local `.lrc` files on disk.
- **Engine**: Parses timecodes using highly optimized RegEx and synchronizes the active lyric in $O(\log n)$ time using binary search algorithms tied to the audio playback position.

---

## 🛠️ Building & Integration

Because OpenReceiver strictly segregates its logic from its UI, building requires the specific toolchain for your target platform.

| Target Platform | Toolchain Requirement | Instructions |
| :--- | :--- | :--- |
| **C++ Core Engine** | CMake 3.10+ | [View Core Build Guide](docs/BUILDING.md#1-building-the-core-cross-platform) |
| **Windows 11 App** | Visual Studio 2022 (WinUI 3) | [View Windows Build Guide](docs/BUILDING.md#2-building-for-windows-winui-3) |
| **Android TV App** | Android Studio (NDK) | [View Android Build Guide](docs/BUILDING.md#4-building-for-android-tv) |

### Automated Packaging
For Windows deployments, OpenReceiver includes a standalone **Inno Setup** script (`windows/packaging/OpenReceiver.iss`). Compiling this script generates a professional `.exe` installer that automatically registers the required Windows Defender Firewall exceptions for UDP/TCP AirPlay traffic.

---

## 📡 Network & Discovery

AirPlay relies heavily on multicast DNS (mDNS). OpenReceiver provides an automated Python mock script (`scripts/advertise_mdns.py`) for testing discovery protocols locally.

If your network is failing to discover the receiver, it is almost exclusively related to router-level AP Isolation or OS-level firewall restrictions. 
Read the [Troubleshooting & Network Configuration Guide](docs/TROUBLESHOOTING.md) for step-by-step resolution paths.

---

<div align="center">
  <p>Built with precision for <b>OpenPlay</b>.</p>
</div>
