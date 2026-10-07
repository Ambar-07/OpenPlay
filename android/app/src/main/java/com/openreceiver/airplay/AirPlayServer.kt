package com.openreceiver.airplay

import android.graphics.Bitmap
import android.graphics.BitmapFactory
import android.media.AudioAttributes
import android.media.AudioFormat
import android.media.AudioManager
import android.media.AudioTrack
import android.util.Base64
import android.util.Log
import com.openreceiver.core.NativeBridge
import java.io.BufferedInputStream
import java.io.InputStream
import java.io.OutputStream
import java.net.DatagramPacket
import java.net.DatagramSocket
import java.net.InetAddress
import java.net.InetSocketAddress
import java.net.ServerSocket
import java.net.Socket
import java.security.KeyFactory
import java.security.PrivateKey
import java.security.spec.PKCS8EncodedKeySpec
import javax.crypto.Cipher
import javax.crypto.spec.IvParameterSpec
import javax.crypto.spec.SecretKeySpec

/**
 * AirPlay 1 (RAOP) receiver - Android port of windows/OpenReceiverApp/ViewModels/AirPlayServer.cs
 */
class AirPlayServer(
    private val nativeBridge: NativeBridge,
    private val deviceId: ByteArray,
    private val listener: Listener
) {

    interface Listener {
        fun onServerStarted(port: Int)
        fun onServerError(message: String)
        fun onSessionStarted(clientIp: String)
        fun onSessionEnded()
        fun onTrackMetadata(title: String?, artist: String?, album: String?, durationSec: Double)
        fun onAlbumArt(bitmap: Bitmap)
        fun onPlaybackState(isPlaying: Boolean)
        fun onProgress(positionSec: Double, durationSec: Double)
        fun onVolume(fraction: Float)
        fun onRemoteControlInfo(clientIp: String, dacpId: String?, activeRemote: String?)
    }

    companion object {
        private const val TAG = "AirPlayServer"
        private const val PREFERRED_PORT = 7000

        // Shairport RSA key (same key as windows/native/Bridge.cpp) in PKCS#8 form
        private const val PKCS8 =
            "MIIEvwIBADANBgkqhkiG9w0BAQEFAASCBKkwggSlAgEAAoIBAQDn10TyouJ4i2wfVaCOtwVEqPp5RaqL5sYs5fUcvdTcaEL+PRCD3S7ewb/UJS3ALm85i98OYUjqhIVeLkQtptYmZPZ0ofMEkpreT2iT7y325xGox3oNkcnZgIIuUNEpIq/qQOqfDhTA92k4xfOIL8AyPdn+VRVfUbtZIcIBYp/XM1LV4u+qv5ugSNe4E6K2dn9sPM8etM5nPQN7DS6jDF//6wb40Ird5AlXGpxon+8QcohV3Yz7movvXIlD7ztfqhXd5pi+3fNZlgPrPm9hNyu2KPZVn1maeL9QBoeqf0l2wFYtQSlW+JieGKY1W9gVl4JeD8h1ND7HghF2Jc2/mER7AgMBAAECggEBAOXwDHL1d9YEuaTOQSKqhLAXQ+yZWs/Mf0qyfAsYf5BmW+NZ3xJZgY3u7XnTse+EXk3d2smhVTc7XicNjhMVABouUn1UzfkACldovJjURGs3u70Asp3YtTBiEzsqbnf07jJQViKQTacg+xwSwDmW2nE6BQYJjtvt7Pk20PqcvVkpq7Dto1eZUC+YlNy4/FaaiS0XeAMkorbDFm40ZwkTS4VAQbhncGtY/vKg25Ird2KLaOaWk8evQ78qc9C3Mjd6C6F7RPBR6b95hJ3LMzJXH9inCTPC1gvexHmTSj2spAu28vN8Cp0HEG6tyLNpoD8vQciACY6K3UYkDaxozFNU82ECgYEA9+C/Wh5nGDGai2IJwxcURARZ+XOFZhOxeuFQi7PmMW5rf0YtL31kQSuEt2vCPysMNWJFUnmyQ6n3MW+VgAezTGH3aOLUTtX/KycoF+wys+STkpIo+ueOd0yg9169adWSAnmPEW42DGQ4sy4b2LncHjIy8NMJGIg8xD743aIsNpECgYEA72//+ZTx5WRBqgA1/RmgyNbwI3jHBYDZxIQgeR30B8WR+26/yjIsMIbdkB/S+uGuu2St9rt5/4BRvr0M2CCriYdABgGnsv6TkMrMmsq47Sv5HRhtj2lkPX7+D11W33V3otA16lQT/JjY8/kI2gWaN52kscw48V1WCoPMMXFTyEsCgYEA0OuvvEAluoGMdXAjNDhOj2lvgE16oOd2TlB7t9Pf78fWeMZoLT+tcTRBvurnJKCewJvcO8BwnJEz1Ins4qUa3QUxJ0kPkobRc8ikBU3CCldcfkwMmDT0od6HSRej5ADq+IUGLbXLfjQ2iecR91/ng9fhkZL9dpzVQr6kuQEH7NECgYB/QBjcfeopLaUwQjhvMQWgd4rcbz3mkNordMUFWYPt9XRmGi/Xt96AU8zA4gjwyKxib1l9PZnSzlGjezmuS36e8sB18L89g8rNMtqWkZLCiZI1glwH0c0yWaGQbNzUmcthPiLJTLHqlxkGYJ3xsPSLBj8XNyA0NpSZtf35cO9EDQKBgQCQTukg+UTvWq98lCCgD16bSAgsC4Tg+7XdoqImd9+3uEiNsr7mTJvdPKxm+jIOdvcc4q8icru9dsq5TghKDEHZsHcdxjNAwazPWonaAbQ3mG8mnPDCFuFeoUoDjNppKvDrbbAOeIArkyUgTS0gAoo/jLE0aOgPZBiOEEa6G+RYpg=="
    }

    @Volatile private var running = false
    private var serverSocket: ServerSocket? = null

    private val privateKey: PrivateKey? = loadPrivateKey()

    private val sessionLock = Any()
    @Volatile private var activeSocket: Socket? = null

    private var audioSocket: DatagramSocket? = null
    private var controlSocket: DatagramSocket? = null
    private var timingSocket: DatagramSocket? = null

    private var audioTrack: AudioTrack? = null
    private var alacPtr: Long = 0L

    private val aesKey = ByteArray(16)
    private val aesIv = ByteArray(16)

    @Volatile private var streaming = false
    @Volatile private var volume = 1f

    private fun loadPrivateKey(): PrivateKey? = try {
        val spec = PKCS8EncodedKeySpec(Base64.decode(PKCS8, Base64.DEFAULT))
        KeyFactory.getInstance("RSA").generatePrivate(spec)
    } catch (e: Exception) {
        Log.e(TAG, "Failed to load RSA key", e)
        null
    }

    // ------------------------------------------------------------------ lifecycle

    fun start() {
        if (running) return
        running = true
        Thread({
            val ss: ServerSocket = try {
                ServerSocket().apply {
                    reuseAddress = true
                    bind(InetSocketAddress(PREFERRED_PORT))
                }
            } catch (e: Exception) {
                Log.w(TAG, "Port $PREFERRED_PORT busy (${e.message}), using a free port")
                try {
                    ServerSocket(0)
                } catch (e2: Exception) {
                    running = false
                    listener.onServerError("Cannot open RTSP port: ${e2.message}")
                    return@Thread
                }
            }
            serverSocket = ss
            Log.i(TAG, "RTSP listening on ${ss.localPort}")
            listener.onServerStarted(ss.localPort)

            while (running) {
                val client = try { ss.accept() } catch (e: Exception) { break }
                Thread({ handleClient(client) }, "RtspClient").start()
            }
        }, "RtspListener").start()
    }

    fun stop() {
        running = false
        try { serverSocket?.close() } catch (_: Exception) {}
        activeSocket?.let { try { it.close() } catch (_: Exception) {} }
        closeUdpSockets()
        try { audioTrack?.stop(); audioTrack?.release() } catch (_: Exception) {}
        audioTrack = null
        if (alacPtr != 0L) {
            nativeBridge.freeAlac(alacPtr)
            alacPtr = 0L
        }
    }

    // ------------------------------------------------------------------ RTSP

    private fun handleClient(socket: Socket) {
        val clientIp = socket.inetAddress?.hostAddress ?: "unknown"
        Log.i(TAG, "TCP connection from $clientIp")
        try {
            socket.tcpNoDelay = true
            val input = BufferedInputStream(socket.getInputStream())
            val output = socket.getOutputStream()

            while (running && !socket.isClosed) {
                val requestLine = readLine(input) ?: break
                if (requestLine.isEmpty()) continue

                val headers = HashMap<String, String>()
                while (true) {
                    val line = readLine(input) ?: break
                    if (line.isEmpty()) break
                    val idx = line.indexOf(':')
                    if (idx > 0) headers[line.substring(0, idx).trim().lowercase()] = line.substring(idx + 1).trim()
                }

                val contentLength = headers["content-length"]?.toIntOrNull() ?: 0
                val body = ByteArray(contentLength.coerceAtLeast(0))
                var read = 0
                while (read < body.size) {
                    val n = input.read(body, read, body.size - read)
                    if (n <= 0) break
                    read += n
                }

                val parts = requestLine.split(" ")
                val method = parts[0]
                val cseq = headers["cseq"] ?: "0"
                Log.d(TAG, "<- $requestLine (CSeq $cseq, ${body.size} bytes)")

                if (headers.containsKey("active-remote") || headers.containsKey("dacp-id")) {
                    listener.onRemoteControlInfo(clientIp, headers["dacp-id"], headers["active-remote"])
                }

                val close = processRequest(socket, output, method, cseq, headers, body, clientIp)
                if (close) break
            }
        } catch (e: Exception) {
            Log.d(TAG, "Connection from $clientIp ended: ${e.message}")
        } finally {
            try { socket.close() } catch (_: Exception) {}
            endSession(socket)
        }
    }

    private fun processRequest(
        socket: Socket,
        output: OutputStream,
        method: String,
        cseq: String,
        headers: Map<String, String>,
        body: ByteArray,
        clientIp: String
    ): Boolean {
        var status = 200
        var statusText = "OK"
        val extra = ArrayList<String>()
        var respBody: ByteArray? = null
        var close = false

        headers["apple-challenge"]?.let { challenge ->
            appleResponse(challenge, socket.localAddress)?.let { extra.add("Apple-Response: $it") }
        }

        when (method) {
            "OPTIONS" -> extra.add("Public: ANNOUNCE, SETUP, RECORD, PAUSE, FLUSH, TEARDOWN, OPTIONS, GET_PARAMETER, SET_PARAMETER")

            "ANNOUNCE" -> {
                val sdp = String(body, Charsets.UTF_8)
                var rsaKey: String? = null
                var iv: String? = null
                for (raw in sdp.lines()) {
                    val l = raw.trim()
                    if (l.startsWith("a=rsaaeskey:")) rsaKey = l.substring(12).trim()
                    if (l.startsWith("a=aesiv:")) iv = l.substring(8).trim()
                }
                if (rsaKey != null && iv != null) {
                    decryptAesKey(rsaKey)
                    val ivBytes = b64decode(iv)
                    System.arraycopy(ivBytes, 0, aesIv, 0, minOf(16, ivBytes.size))
                    Log.i(TAG, "AES key/IV extracted")
                }
            }

            "SETUP" -> {
                synchronized(sessionLock) {
                    val previous = activeSocket
                    activeSocket = socket
                    if (previous != null && previous !== socket) {
                        try { previous.close() } catch (_: Exception) {}
                    }
                }
                initAudio()
                val ports = openUdpSockets()
                extra.add("Session: 1")
                extra.add("Transport: RTP/AVP/UDP;unicast;mode=record;server_port=${ports[0]};control_port=${ports[1]};timing_port=${ports[2]}")
                extra.add("Audio-Jack-Status: connected; type=analog")
                listener.onSessionStarted(clientIp)
            }

            "RECORD" -> {
                extra.add("Audio-Latency: 11025")
                startPlayback()
            }

            "FLUSH", "PAUSE" -> {
                streaming = false
                try { audioTrack?.pause(); audioTrack?.flush() } catch (_: Exception) {}
                listener.onPlaybackState(false)
            }

            "GET_PARAMETER" -> {
                extra.add("Content-Type: text/parameters")
                respBody = "volume: 0.000000\r\n".toByteArray(Charsets.US_ASCII)
            }

            "SET_PARAMETER", "PUT", "POST" -> handleParameters(headers["content-type"] ?: "", body)

            "TEARDOWN" -> {
                extra.add("Connection: close")
                close = true
            }

            else -> {
                Log.d(TAG, "Unsupported $method")
                status = 501
                statusText = "Not Implemented"
            }
        }

        val sb = StringBuilder()
        sb.append("RTSP/1.0 ").append(status).append(' ').append(statusText).append("\r\n")
        sb.append("CSeq: ").append(cseq).append("\r\n")
        sb.append("Server: AirTunes/130.14\r\n")
        for (h in extra) sb.append(h).append("\r\n")
        respBody?.let { sb.append("Content-Length: ").append(it.size).append("\r\n") }
        sb.append("\r\n")
        output.write(sb.toString().toByteArray(Charsets.US_ASCII))
        respBody?.let { output.write(it) }
        output.flush()
        return close
    }

    private fun handleParameters(ctype: String, body: ByteArray) {
        if (body.isEmpty()) return
        when {
            ctype.startsWith("image/") -> {
                BitmapFactory.decodeByteArray(body, 0, body.size)?.let { listener.onAlbumArt(it) }
            }
            ctype.startsWith("application/x-dmap-tagged") -> parseDmap(body)
            ctype.startsWith("text/parameters") -> {
                for (line in String(body, Charsets.UTF_8).lines()) {
                    val l = line.trim()
                    if (l.startsWith("volume:")) {
                        val db = l.substring(7).trim().toFloatOrNull() ?: continue
                        val f = when {
                            db <= -30f -> 0f
                            db >= 0f -> 1f
                            else -> (db + 30f) / 30f
                        }
                        volume = f
                        try { audioTrack?.setVolume(f) } catch (_: Exception) {}
                        listener.onVolume(f)
                    } else if (l.startsWith("progress:")) {
                        val p = l.substring(9).trim().split("/")
                        if (p.size >= 3) {
                            val start = p[0].toLongOrNull() ?: continue
                            val curr = p[1].toLongOrNull() ?: continue
                            val end = p[2].toLongOrNull() ?: continue
                            val pos = (curr - start) / 44100.0
                            val dur = (end - start) / 44100.0
                            if (pos >= 0) listener.onProgress(pos, if (dur > 0) dur else 0.0)
                        }
                    }
                }
            }
        }
    }

    private fun endSession(socket: Socket) {
        synchronized(sessionLock) {
            if (activeSocket !== socket) return
            activeSocket = null
        }
        streaming = false
        closeUdpSockets()
        try { audioTrack?.pause(); audioTrack?.flush() } catch (_: Exception) {}
        listener.onPlaybackState(false)
        listener.onSessionEnded()
    }

    // ------------------------------------------------------------------ crypto

    private fun b64decode(s: String): ByteArray {
        var t = s.trim().replace('-', '+').replace('_', '/')
        while (t.length % 4 != 0) t += "="
        return Base64.decode(t, Base64.DEFAULT)
    }

    private fun appleResponse(challengeB64: String, local: InetAddress): String? {
        val pk = privateKey ?: return null
        return try {
            val challenge = b64decode(challengeB64)
            val ip = local.address
            val payload = ByteArray(maxOf(32, 16 + ip.size + 6))
            System.arraycopy(challenge, 0, payload, 0, minOf(16, challenge.size))
            System.arraycopy(ip, 0, payload, 16, ip.size)
            System.arraycopy(deviceId, 0, payload, 16 + ip.size, 6)
            val cipher = Cipher.getInstance("RSA/ECB/PKCS1Padding")
            cipher.init(Cipher.ENCRYPT_MODE, pk)
            Base64.encodeToString(cipher.doFinal(payload), Base64.NO_WRAP).trimEnd('=')
        } catch (e: Exception) {
            Log.e(TAG, "Apple-Response failed", e)
            null
        }
    }

    private fun decryptAesKey(rsaKeyB64: String) {
        val pk = privateKey ?: return
        val enc = b64decode(rsaKeyB64)
        val dec = try {
            Cipher.getInstance("RSA/ECB/OAEPWithSHA-1AndMGF1Padding").run {
                init(Cipher.DECRYPT_MODE, pk); doFinal(enc)
            }
        } catch (e: Exception) {
            try {
                Cipher.getInstance("RSA/ECB/PKCS1Padding").run {
                    init(Cipher.DECRYPT_MODE, pk); doFinal(enc)
                }
            } catch (e2: Exception) {
                Log.e(TAG, "AES key decrypt failed", e2)
                null
            }
        }
        if (dec != null && dec.size >= 16) System.arraycopy(dec, 0, aesKey, 0, 16)
    }

    // ------------------------------------------------------------------ audio

    private fun initAudio() {
        if (alacPtr == 0L) alacPtr = nativeBridge.initAlac()
        if (audioTrack == null) {
            val minBuf = AudioTrack.getMinBufferSize(44100, AudioFormat.CHANNEL_OUT_STEREO, AudioFormat.ENCODING_PCM_16BIT)
            audioTrack = AudioTrack(
                AudioAttributes.Builder()
                    .setUsage(AudioAttributes.USAGE_MEDIA)
                    .setContentType(AudioAttributes.CONTENT_TYPE_MUSIC)
                    .build(),
                AudioFormat.Builder()
                    .setEncoding(AudioFormat.ENCODING_PCM_16BIT)
                    .setSampleRate(44100)
                    .setChannelMask(AudioFormat.CHANNEL_OUT_STEREO)
                    .build(),
                maxOf(minBuf * 4, 44100 * 4 / 2),
                AudioTrack.MODE_STREAM,
                AudioManager.AUDIO_SESSION_ID_GENERATE
            )
            try { audioTrack?.setVolume(volume) } catch (_: Exception) {}
        }
    }

    private fun startPlayback() {
        try { audioTrack?.play() } catch (_: Exception) {}
        streaming = true
        listener.onPlaybackState(true)
    }

    /** Opens audio/control/timing sockets on free ports. Returns [audio, control, timing]. */
    private fun openUdpSockets(): IntArray {
        closeUdpSockets()
        val a = DatagramSocket(0)
        val c = DatagramSocket(0)
        val t = DatagramSocket(0)
        audioSocket = a; controlSocket = c; timingSocket = t
        val key = aesKey.copyOf()
        val iv = aesIv.copyOf()
        Thread({ audioLoop(a, key, iv) }, "RaopAudio").start()
        Thread({ drain(c) }, "RaopControl").start()
        Thread({ drain(t) }, "RaopTiming").start()
        Log.i(TAG, "UDP audio=${a.localPort} control=${c.localPort} timing=${t.localPort}")
        return intArrayOf(a.localPort, c.localPort, t.localPort)
    }

    private fun closeUdpSockets() {
        try { audioSocket?.close() } catch (_: Exception) {}
        try { controlSocket?.close() } catch (_: Exception) {}
        try { timingSocket?.close() } catch (_: Exception) {}
        audioSocket = null; controlSocket = null; timingSocket = null
    }

    private fun audioLoop(socket: DatagramSocket, key: ByteArray, iv: ByteArray) {
        val buf = ByteArray(2048)
        val packet = DatagramPacket(buf, buf.size)
        val alacIn = ByteArray(2048 + 32)
        val pcm = ByteArray(352 * 4 * 2)
        val cipher = Cipher.getInstance("AES/CBC/NoPadding")
        val keySpec = SecretKeySpec(key, "AES")
        val ivSpec = IvParameterSpec(iv)

        while (running && !socket.isClosed) {
            try {
                packet.setLength(buf.size) // IMPORTANT: receive() shrinks length to the last packet size
                socket.receive(packet)
            } catch (e: Exception) {
                break
            }
            try {
                val len = packet.length
                if (len <= 12) continue
                if ((buf[1].toInt() and 0x7F) != 96) continue

                val payloadLen = len - 12
                val cipherLen = payloadLen and 0xF.inv()
                if (cipherLen > 0) {
                    // AirPlay resets the IV for every packet
                    cipher.init(Cipher.DECRYPT_MODE, keySpec, ivSpec)
                    cipher.doFinal(buf, 12, cipherLen, alacIn, 0)
                }
                if (payloadLen > cipherLen) System.arraycopy(buf, 12 + cipherLen, alacIn, cipherLen, payloadLen - cipherLen)
                java.util.Arrays.fill(alacIn, payloadLen, minOf(alacIn.size, payloadLen + 32), 0)

                val ptr = alacPtr
                val track = audioTrack ?: continue
                if (ptr == 0L) continue
                val pcmLen = nativeBridge.decodeAlac(ptr, alacIn, payloadLen, pcm)
                if (pcmLen <= 0) continue

                if (!streaming) {
                    try { track.play() } catch (_: Exception) {}
                    streaming = true
                    listener.onPlaybackState(true)
                }
                track.write(pcm, 0, pcmLen)
            } catch (e: Exception) {
                Log.w(TAG, "Audio packet error: ${e.message}")
            }
        }
    }

    private fun drain(socket: DatagramSocket) {
        val buf = ByteArray(1500)
        val p = DatagramPacket(buf, buf.size)
        while (running && !socket.isClosed) {
            try { p.setLength(buf.size); socket.receive(p) } catch (e: Exception) { break }
        }
    }

    // ------------------------------------------------------------------ metadata

    private fun parseDmap(body: ByteArray) {
        var title: String? = null
        var artist: String? = null
        var album: String? = null
        var durationMs = 0
        var pict: ByteArray? = null

        var i = 0
        while (i + 8 <= body.size) {
            val tag = String(body, i, 4, Charsets.US_ASCII)
            val len = ((body[i + 4].toInt() and 0xFF) shl 24) or ((body[i + 5].toInt() and 0xFF) shl 16) or
                    ((body[i + 6].toInt() and 0xFF) shl 8) or (body[i + 7].toInt() and 0xFF)
            i += 8
            if (len < 0 || i + len > body.size) break
            if (tag == "mlit" || tag == "mdcl" || tag == "mcor" || tag == "mlog" || tag == "mccr") continue
            when (tag) {
                "minm" -> title = String(body, i, len, Charsets.UTF_8)
                "asar" -> artist = String(body, i, len, Charsets.UTF_8)
                "asal" -> album = String(body, i, len, Charsets.UTF_8)
                "astm" -> if (len == 4) durationMs = ((body[i].toInt() and 0xFF) shl 24) or ((body[i + 1].toInt() and 0xFF) shl 16) or
                        ((body[i + 2].toInt() and 0xFF) shl 8) or (body[i + 3].toInt() and 0xFF)
                "PICT" -> pict = body.copyOfRange(i, i + len)
            }
            i += len
        }

        pict?.let { p -> BitmapFactory.decodeByteArray(p, 0, p.size)?.let { listener.onAlbumArt(it) } }
        if (title != null || artist != null || album != null) {
            listener.onTrackMetadata(title, artist, album, if (durationMs > 0) durationMs / 1000.0 else 0.0)
        }
    }

    private fun readLine(input: InputStream): String? {
        val sb = StringBuilder()
        while (true) {
            val b = input.read()
            if (b == -1) return if (sb.isNotEmpty()) sb.toString() else null
            if (b == '\n'.code) {
                if (sb.isNotEmpty() && sb[sb.length - 1] == '\r') sb.setLength(sb.length - 1)
                return sb.toString()
            }
            sb.append(b.toChar())
        }
    }
}
