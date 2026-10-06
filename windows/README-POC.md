# Windows AirPlay Discovery & Receiver Proof-of-Concept (PoC)

## Overview
This proof-of-concept demonstrates the basic mechanisms required for an iOS device to discover the receiver on the local network via mDNS and establish an initial TCP connection over RTSP on port 7000.

## Components
1. **RTSP TCP Listener (C++)**: `windows/native/main.cpp`
   - Binds to port `7000`.
   - Listens for incoming AirPlay connections and logs the initial RTSP headers from the sender.
   
2. **mDNS Advertiser (Python)**: `scripts/advertise_mdns.py`
   - Simulates the mDNS/Bonjour advertisement that the OpenReceiver C++ core will eventually handle.
   - Broadcasts the `_raop._tcp` service with the correct TXT records to appear on an iOS sender.

## How to Test

### 1. Start the C++ Listener
You will need to compile the C++ listener using CMake.
```bash
cmake -S . -B build
cmake --build build
./build/windows/native/Debug/OpenReceiverPoC.exe
```

*(Note: If you don't have CMake/Visual Studio installed, ensure the C++ desktop workload is installed via Visual Studio Installer).*

### 2. Start the mDNS Advertiser
Ensure you have Python installed, then install the `zeroconf` library:
```bash
pip install zeroconf
```

Run the script:
```bash
python scripts/advertise_mdns.py
```

### 3. Connect from an iOS Device
1. Ensure your iPhone/iPad is on the same Wi-Fi network as your Windows PC.
2. Open the Control Center and tap the AirPlay audio icon.
3. You should see **OpenReceiver** in the list of available devices.
4. Select it.
5. Watch the C++ console output to see the incoming RTSP connection request from your device!
