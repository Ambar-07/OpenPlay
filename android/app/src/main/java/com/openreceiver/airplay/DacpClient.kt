package com.openreceiver.airplay

import android.content.Context
import android.net.nsd.NsdManager
import android.net.nsd.NsdServiceInfo
import android.util.Log
import java.net.ConnectException
import java.net.HttpURLConnection
import java.net.URL
import java.util.concurrent.CountDownLatch
import java.util.concurrent.Executors
import java.util.concurrent.TimeUnit

/** Sends remote-control commands back to the iPhone (port of DacpClient.cs). */
class DacpClient(context: Context) {

    private val tag = "DacpClient"
    private val appContext = context.applicationContext
    private val executor = Executors.newSingleThreadExecutor()

    @Volatile private var clientIp: String? = null
    @Volatile private var dacpId: String? = null
    @Volatile private var activeRemote: String? = null
    @Volatile private var resolvedPort = -1

    val isAvailable: Boolean
        get() = clientIp != null && dacpId != null && activeRemote != null

    fun update(ip: String, id: String?, remote: String?) {
        if (ip != clientIp || (id != null && id != dacpId)) resolvedPort = -1
        clientIp = ip
        if (id != null) dacpId = id
        if (remote != null) activeRemote = remote
    }

    fun send(command: String) {
        executor.execute { doSend(command) }
    }

    fun shutdown() {
        executor.shutdownNow()
    }

    private fun doSend(command: String) {
        val ip = clientIp ?: return
        val id = dacpId ?: return
        val remote = activeRemote ?: return
        if (resolvedPort == -1) {
            resolvedPort = resolvePort(id)
            if (resolvedPort <= 0) resolvedPort = 3689
            Log.i(tag, "DACP port for $ip = $resolvedPort")
        }
        val host = if (ip.contains(':')) "[${ip.substringBefore('%')}]" else ip
        var conn: HttpURLConnection? = null
        try {
            conn = URL("http://$host:$resolvedPort/ctrl-int/1/$command").openConnection() as HttpURLConnection
            conn.connectTimeout = 3000
            conn.readTimeout = 3000
            conn.setRequestProperty("Active-Remote", remote)
            conn.setRequestProperty("Viewer-Only-Client", "1")
            conn.setRequestProperty("Client-DAAP-Version", "3.11")
            Log.i(tag, "DACP $command -> ${conn.responseCode}")
        } catch (e: ConnectException) {
            Log.w(tag, "DACP connect failed, will re-resolve: ${e.message}")
            resolvedPort = -1
        } catch (e: Exception) {
            Log.w(tag, "DACP error: ${e.message}")
        } finally {
            conn?.disconnect()
        }
    }

    @Suppress("DEPRECATION")
    private fun resolvePort(id: String): Int {
        val nsd = appContext.getSystemService(Context.NSD_SERVICE) as? NsdManager ?: return -1
        val target = "iTunes_Ctrl_$id"
        val latch = CountDownLatch(1)
        var port = -1

        val discovery = object : NsdManager.DiscoveryListener {
            override fun onDiscoveryStarted(serviceType: String) {}
            override fun onDiscoveryStopped(serviceType: String) {}
            override fun onServiceLost(serviceInfo: NsdServiceInfo) {}
            override fun onStartDiscoveryFailed(serviceType: String, errorCode: Int) { latch.countDown() }
            override fun onStopDiscoveryFailed(serviceType: String, errorCode: Int) {}
            override fun onServiceFound(serviceInfo: NsdServiceInfo) {
                if (!serviceInfo.serviceName.equals(target, ignoreCase = true)) return
                nsd.resolveService(serviceInfo, object : NsdManager.ResolveListener {
                    override fun onResolveFailed(serviceInfo: NsdServiceInfo, errorCode: Int) { latch.countDown() }
                    override fun onServiceResolved(serviceInfo: NsdServiceInfo) {
                        port = serviceInfo.port
                        latch.countDown()
                    }
                })
            }
        }

        try {
            nsd.discoverServices("_dacp._tcp", NsdManager.PROTOCOL_DNS_SD, discovery)
            latch.await(4, TimeUnit.SECONDS)
        } catch (e: Exception) {
            Log.w(tag, "DACP discovery error: ${e.message}")
        } finally {
            try { nsd.stopServiceDiscovery(discovery) } catch (_: Exception) {}
        }
        return port
    }
}
