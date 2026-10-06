package com.openreceiver.core

import android.util.Log

class NativeBridge {
    
    interface StateCallback {
        fun onTrackInfoChanged(title: String, artist: String, album: String, duration: Double)
        fun onPlaybackStateChanged(status: Int, position: Double)
    }

    private var callback: StateCallback? = null

    fun setCallback(cb: StateCallback) {
        this.callback = cb
    }

    // Called from Kotlin UI (e.g. Foreground Service) to initialize the native AirPlay server
    external fun initCore()
    
    // Called from Kotlin UI to cleanly teardown the server
    external fun shutdownCore()

    // Called asynchronously from C++ via JNI when TrackInfo changes in the Core StateManager
    private fun onTrackInfoChanged(title: String, artist: String, album: String, duration: Double) {
        Log.d("NativeBridge", "Track info changed: $title by $artist")
        callback?.onTrackInfoChanged(title, artist, album, duration)
    }

    // Called asynchronously from C++ via JNI when PlaybackState changes
    private fun onPlaybackStateChanged(status: Int, position: Double) {
        callback?.onPlaybackStateChanged(status, position)
    }

    companion object {
        init {
            // Load the compiled C++ core library
            System.loadLibrary("openreceiver_core")
        }
    }
}
