package com.openreceiver.airplay

import android.content.Context
import android.net.nsd.NsdManager
import android.net.nsd.NsdServiceInfo
import android.net.wifi.WifiManager
import android.util.Log
import java.net.Inet4Address
import java.net.NetworkInterface

/**
 * Publishes the AirPlay 1 (RAOP) service so iPhones list this TV in the AirPlay menu.
 * Same TXT record as scripts/advertise_mdns.py.
 */
class MdnsAdvertiser(
    context: Context,
    private val deviceIdHex: String,
    private val displayName: String
) {
    interface Callback {
        fun onRegistered(serviceName: String)
        fun onFailed(errorCode: Int)
    }

    private val tag = "MdnsAdvertiser"
    private val appContext = context.applicationContext
    private var multicastLock: WifiManager.MulticastLock? = null
    private var nsd: NsdManager? = null
    private var registration: NsdManager.RegistrationListener? = null

    fun start(port: Int, callback: Callback) {
        stop()
        try {
            val wifi = appContext.getSystemService(Context.WIFI_SERVICE) as? WifiManager
            multicastLock = wifi?.createMulticastLock("OpenReceiverMdns")?.apply {
                setReferenceCounted(false)
                acquire()
            }
        } catch (e: Exception) {
            Log.w(tag, "MulticastLock unavailable: ${e.message}")
        }

        val info = NsdServiceInfo().apply {
            serviceName = "$deviceIdHex@$displayName"
            serviceType = "_raop._tcp"
            setPort(port)
            setAttribute("txtvers", "1")
            setAttribute("ch", "2")
            setAttribute("cn", "0,1")
            setAttribute("et", "0,1")
            setAttribute("ek", "1")
            setAttribute("sv", "false")
            setAttribute("da", "true")
            setAttribute("sr", "44100")
            setAttribute("ss", "16")
            setAttribute("pw", "false")
            setAttribute("vn", "65537")
            setAttribute("tp", "UDP")
            setAttribute("md", "0,1,2")
            setAttribute("vs", "130.14")
            setAttribute("am", "OpenReceiver")
            setAttribute("sf", "0x4")
        }

        val listener = object : NsdManager.RegistrationListener {
            override fun onServiceRegistered(serviceInfo: NsdServiceInfo) {
                Log.i(tag, "Registered ${serviceInfo.serviceName} on port $port")
                callback.onRegistered(serviceInfo.serviceName)
            }
            override fun onRegistrationFailed(serviceInfo: NsdServiceInfo, errorCode: Int) {
                Log.e(tag, "Registration failed: $errorCode")
                callback.onFailed(errorCode)
            }
            override fun onServiceUnregistered(serviceInfo: NsdServiceInfo) {}
            override fun onUnregistrationFailed(serviceInfo: NsdServiceInfo, errorCode: Int) {}
        }

        try {
            nsd = appContext.getSystemService(Context.NSD_SERVICE) as NsdManager
            nsd?.registerService(info, NsdManager.PROTOCOL_DNS_SD, listener)
            registration = listener
        } catch (e: Exception) {
            Log.e(tag, "registerService threw", e)
            callback.onFailed(-1)
        }
    }

    fun stop() {
        try { registration?.let { nsd?.unregisterService(it) } } catch (_: Exception) {}
        registration = null
        try { multicastLock?.let { if (it.isHeld) it.release() } } catch (_: Exception) {}
        multicastLock = null
    }

    companion object {
        fun localIpv4(): String? {
            try {
                for (iface in NetworkInterface.getNetworkInterfaces()) {
                    if (!iface.isUp || iface.isLoopback) continue
                    for (addr in iface.inetAddresses) {
                        if (addr is Inet4Address && !addr.isLoopbackAddress) return addr.hostAddress
                    }
                }
            } catch (_: Exception) {}
            return null
        }
    }
}
