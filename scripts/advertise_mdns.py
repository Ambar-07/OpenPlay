import socket
import time
from zeroconf import ServiceInfo, Zeroconf

def get_ip():
    s = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
    try:
        # Doesn't have to be reachable
        s.connect(('10.255.255.255', 1))
        IP = s.getsockname()[0]
    except Exception:
        IP = '127.0.0.1'
    finally:
        s.close()
    return IP

if __name__ == '__main__':
    ip = get_ip()
    port = 7000
    
    # Minimal TXT records for a receiver to show up on iOS
    properties = {
        b'txtvers': b'1',
        b'ch': b'2',
        b'cn': b'0,1,2,3',
        b'et': b'0,1',
        b'md': b'0,1,2',
        b'pw': b'false',
        b'sr': b'44100',
        b'ss': b'16',
        b'tp': b'UDP',
        b'vs': b'130.14',
        b'am': b'AppleTV2,1',
        b'fv': b'76400.10',
        b'sf': b'0x4'
    }
    
    # The MAC address acts as the unique device ID
    device_id = "00:11:22:33:44:55"
    
    info = ServiceInfo(
        "_raop._tcp.local.",
        f"{device_id}@OpenReceiver._raop._tcp.local.",
        addresses=[socket.inet_aton(ip)],
        port=port,
        properties=properties,
        server="OpenReceiver.local.",
    )
    
    zeroconf = Zeroconf()
    print(f"Advertising AirPlay service on {ip}:{port}...")
    zeroconf.register_service(info)
    
    try:
        while True:
            time.sleep(0.1)
    except KeyboardInterrupt:
        pass
    finally:
        print("Unregistering...")
        zeroconf.unregister_service(info)
        zeroconf.close()
