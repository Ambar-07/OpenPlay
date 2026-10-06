# Troubleshooting Discovery Issues

AirPlay heavily relies on **mDNS (Bonjour)** to broadcast the receiver's presence over the local network via UDP port 5353. 

If your iPhone/iPad cannot see OpenReceiver on the network, it is almost always due to one of the following network policies:

### 1. Windows Defender Firewall (Windows)
By default, Windows aggressively drops unsolicited incoming UDP broadcast packets on port 5353.
**Solution**: 
- If running from source, you must manually allow your compiled `.exe` (or `python.exe` if running the test script) through the Windows Firewall for **both Private and Public** networks.
- If using the official installer, this is handled automatically via the `.iss` script.

### 2. Network Profile (Windows)
Windows will actively suppress network discovery if your active Wi-Fi or Ethernet connection is set to a "Public" profile.
**Solution**:
- Go to Windows Settings -> Network & Internet -> Wi-Fi -> Manage known networks.
- Click your network and ensure the Network Profile is set to **Private**.

### 3. Router AP / Client Isolation
Many ISP-provided routers (e.g. JioFiber, Xfinity, Airtel) have a security feature enabled by default called "AP Isolation" or "Client Isolation". 
This completely isolates wireless clients from each other, meaning your iPhone physically cannot send packets to your PC/TV over the local network, even if they share the same Wi-Fi name.
**Solution**:
- Log into your router's admin panel (usually `192.168.1.1` or `192.168.29.1`).
- Look under "Advanced Wireless Settings" or "Security".
- Disable **AP Isolation** or **Client Isolation**.

### 4. 2.4GHz vs 5GHz Subnets
Some dual-band routers broadcast the 2.4GHz and 5GHz bands under the same name but place them on separate subnets, breaking mDNS multicast.
**Solution**: Ensure your iPhone and your Receiver are connected to the exact same frequency band (e.g. both on 5GHz).
