package com.openreceiver.core

import android.util.Log

class NativeBridge {
    
    interface StateCallback {
        fun onTrackInfoChanged(title: String, artist: String, album: String, duration: Double)
        fun onPlaybackStateChanged(status: Int, position: Double)
    }

    private var callback: StateCallback? = null
    private var nativeAvailable = false

    init {
        nativeAvailable = nativeLibAvailable
    }

    fun setCallback(cb: StateCallback) {
        this.callback = cb
    }

    // Called from Kotlin UI to initialize the native core
    fun initCore() {
        if (nativeAvailable) {
            try { initCoreNative() } catch (e: Throwable) {
                Log.e(TAG, "initCore failed: ${e.message}")
                nativeAvailable = false
            }
        }
    }
    
    // Called from Kotlin UI to cleanly teardown the server
    fun shutdownCore() {
        if (nativeAvailable) {
            try { shutdownCoreNative() } catch (_: Throwable) {}
        }
    }

    // ALAC decoder - returns 0 if native not available
    fun initAlac(): Long {
        if (!nativeAvailable) return 0L
        return try { initAlacNative() } catch (_: Throwable) { 0L }
    }

    fun decodeAlac(alacPtr: Long, input: ByteArray, inputLen: Int, output: ByteArray): Int {
        if (!nativeAvailable || alacPtr == 0L) return 0
        return try { decodeAlacNative(alacPtr, input, inputLen, output) } catch (_: Throwable) { 0 }
    }

    fun freeAlac(alacPtr: Long) {
        if (!nativeAvailable || alacPtr == 0L) return
        try { freeAlacNative(alacPtr) } catch (_: Throwable) {}
    }

    // Called asynchronously from C++ via JNI when TrackInfo changes
    @Suppress("unused") // called from JNI
    private fun onTrackInfoChanged(title: String, artist: String, album: String, duration: Double) {
        Log.d(TAG, "Track info changed: $title by $artist")
        callback?.onTrackInfoChanged(title, artist, album, duration)
    }

    // Called asynchronously from C++ via JNI when PlaybackState changes
    @Suppress("unused") // called from JNI
    private fun onPlaybackStateChanged(status: Int, position: Double) {
        callback?.onPlaybackStateChanged(status, position)
    }

    // --- JNI-mapped native functions (private, wrapped above) ---
    private external fun initCoreNative()
    private external fun shutdownCoreNative()
    private external fun initAlacNative(): Long
    private external fun decodeAlacNative(alacPtr: Long, input: ByteArray, inputLen: Int, output: ByteArray): Int
    private external fun freeAlacNative(alacPtr: Long)

    companion object {
        private const val TAG = "NativeBridge"
        
        @Volatile
        var nativeLibAvailable = false
            private set

        init {
            try {
                System.loadLibrary("openreceiver_core")
                nativeLibAvailable = true
                Log.i(TAG, "Native library loaded successfully")
            } catch (e: UnsatisfiedLinkError) {
                nativeLibAvailable = false
                Log.w(TAG, "Native library not available: ${e.message}. Running in pure-Kotlin mode.")
            }
        }
    }
}
