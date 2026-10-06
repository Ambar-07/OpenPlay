# Audit of UxPlay and Open-Source AirPlay Implementations

## Objective
To document protocol behavior, reusable components, limitations, and licenses of existing open-source AirPlay receiver implementations (primarily UxPlay), as mandated by Task 10 of the OpenReceiver agent-ready backlog.

## 1. UxPlay (Primary Reference)
**Repository:** [FDH2/UxPlay](https://github.com/FDH2/UxPlay)

### Protocol Behavior & Implementation Insights
- **Discovery (mDNS/DNS-SD):** UxPlay advertises itself via Bonjour/mDNS as an Apple TV or similar AirPlay destination.
- **Session Negotiation:** Uses RTSP for session setup and teardown. It handles the specific authentication/pairing handshakes required by modern iOS devices.
- **Media Pipeline:** Relies heavily on GStreamer for RTP packet reception, decryption, and decoding (both audio and video). 
- **Metadata:** Parses Apple-specific property lists (plists) and DMAP tags to extract track information and artwork during the RTSP flow.
- **Windows Behavior:** Uses MinGW for Windows builds. It demonstrates how AirPlay receivers can function on Windows using GStreamer.

### Reusable Components vs. Limitations
- **Reusable Concepts:** The RTSP flow sequence, plist parsing strategies, and GStreamer pipeline configurations are excellent references for interoperability.
- **Limitations:** UxPlay is built as a monolithic application rather than a reusable C/C++ library. Its architecture tightly couples the protocol handling with the GStreamer media output.
- **License Constraint:** **GPLv3**. The OpenReceiver specification strictly warns: *"its code cannot simply be copied into an independently licensed architecture without understanding the consequences."* Code must not be blindly copied to avoid inadvertently forcing OpenReceiver into GPLv3, especially given the goal of a flexible, local-first architecture.

## 2. Other Key Implementations & References

### OpenAirPlay Specification
**URL:** [openairplay.github.io/airplay-spec](https://openairplay.github.io/airplay-spec/)
- **Value:** The primary reference for publicly documented AirPlay behavior, RTSP flows, audio metadata, timing, and pairing structures.
- **Usage:** Should be used as the authoritative guide for implementing the `core/` protocol parsers rather than reverse-engineering UxPlay's source code.

### Shairport Sync
**Repository:** [mikebrady/shairport-sync](https://github.com/mikebrady/shairport-sync)
- **Value:** An open-source AirPlay audio receiver reference. It is particularly useful for studying audio-only receiving, precise timing/synchronization, and practical AirPlay audio behavior.
- **License:** MIT License (generally safe for reference and conceptual reuse).

### pyatv
**Repository:** [postlund/pyatv](https://github.com/postlund/pyatv)
- **Value:** Excellent research reference for the Apple TV/AirPlay ecosystem and modern Apple-device protocol behavior. It is written in Python, so it is strictly for research and not for direct code inclusion in the C/C++ core.
- **License:** MIT License.

### libplist
**Repository:** [libimobiledevice/libplist](https://github.com/libimobiledevice/libplist)
- **Value:** A portable C library for handling Apple property lists (binary/XML). This is highly relevant for parsing metadata sent by the AirPlay sender.
- **License:** LGPL-2.1. Safe for dynamic linking in the core if a concrete dependency is justified.

## Summary of Protocol Behavior (AirPlay 1/2 Audio)
1. **Advertisement:** The receiver publishes `_raop._tcp` and `_airplay._tcp` mDNS records.
2. **RTSP Handshake:** The sender connects via RTSP, exchanges keys (if using FairPlay/Pairing, though circumvention is strictly out-of-scope for OpenReceiver), and sets up the audio format.
3. **Metadata Channel:** The sender pushes metadata (title, artist, album) and artwork via HTTP/RTSP POST requests containing binary plists or DMAP structures.
4. **Media Stream:** Audio is streamed via RTP (usually ALAC or AAC format). Time synchronization is maintained using NTP-like timing packets.

## Architectural Decisions based on Audit
- **Clean-Room Implementation:** Due to UxPlay's GPLv3 license, the OpenReceiver `core/` will implement AirPlay session management and parsing independently, using OpenAirPlay specs and UxPlay *only* as a behavioral reference.
- **Dependency Strategy:** 
  - `libplist` may be evaluated for metadata parsing.
  - `GStreamer` will be used for the Windows media pipeline (as per architecture specs) but kept strictly in the platform shell layer, not bleeding into the core session logic.
