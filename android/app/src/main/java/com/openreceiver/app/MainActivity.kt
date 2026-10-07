package com.openreceiver.app

import android.animation.Animator
import android.animation.AnimatorSet
import android.animation.ObjectAnimator
import android.app.Activity
import android.graphics.Bitmap
import android.graphics.Outline
import android.graphics.drawable.BitmapDrawable
import android.graphics.drawable.TransitionDrawable
import android.os.Bundle
import android.os.Handler
import android.os.Looper
import android.view.Gravity
import android.view.View
import android.view.ViewGroup
import android.view.ViewOutlineProvider
import android.view.WindowManager
import android.view.animation.DecelerateInterpolator
import android.widget.FrameLayout
import android.widget.ImageView
import android.widget.LinearLayout
import android.widget.TextView
import com.openreceiver.airplay.AirPlayServer
import com.openreceiver.airplay.DacpClient
import com.openreceiver.airplay.MdnsAdvertiser
import com.openreceiver.core.NativeBridge
import com.openreceiver.lyrics.LyricLine
import com.openreceiver.lyrics.LyricsFetcher
import com.openreceiver.ui.FluidBackgroundView
import java.util.concurrent.Executors
import kotlin.math.abs

class MainActivity : Activity(), AirPlayServer.Listener {

    // --- Core services ---
    private lateinit var nativeBridge: NativeBridge
    private lateinit var mdnsAdvertiser: MdnsAdvertiser
    private lateinit var airPlayServer: AirPlayServer
    private lateinit var dacpClient: DacpClient
    private val lyricsExecutor = Executors.newSingleThreadExecutor()
    private val mainHandler = Handler(Looper.getMainLooper())

    // --- UI Views ---
    private lateinit var fluidBackground: FluidBackgroundView
    private lateinit var artworkCard: FrameLayout
    private lateinit var ivArtwork: ImageView
    private lateinit var tvTitle: TextView
    private lateinit var tvArtist: TextView
    private lateinit var tvTimeLeft: TextView
    private lateinit var tvTimeRight: TextView
    private lateinit var progressTrack: android.widget.SeekBar

    private lateinit var albumColumn: LinearLayout
    private lateinit var lyricsColumn: FrameLayout
    private lateinit var lyricPanel: View
    private lateinit var tvPrevPrevLyric: TextView
    private lateinit var tvPrevLyric: TextView
    private lateinit var tvCurrentLyric: TextView
    private lateinit var tvNextLyric: TextView
    private lateinit var tvNextNextLyric: TextView
    private lateinit var tvNextNextNextLyric: TextView
    private lateinit var noLyricsText: TextView

    private lateinit var btnLyrics: View
    private lateinit var ivLyricsIcon: ImageView

    // --- State ---
    private var currentDurationSec = 0.0
    private var currentPositionSec = 0.0
    private var lastProgressPos = -1.0
    private var isPlaying = false
    private var lyricsEnabled = true
    private var activeLyrics: List<LyricLine>? = null
    private var currentLyricIndex = -999
    private var currentAlbumBitmap: Bitmap? = null

    private var currentTrackTitle: String = ""
    private var currentTrackArtist: String = ""
    private var lyricsFetchSeq: Int = 0

    // MAC-like device ID for mDNS advertisement
    private val deviceMac = byteArrayOf(0x5E.toByte(), 0x14.toByte(), 0x2C.toByte(), 0x0A.toByte(), 0x71.toByte(), 0x38.toByte())
    private val deviceIdHex = deviceMac.joinToString("") { "%02X".format(it) }

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        window.addFlags(WindowManager.LayoutParams.FLAG_KEEP_SCREEN_ON)
        window.decorView.systemUiVisibility = (
            View.SYSTEM_UI_FLAG_FULLSCREEN or
            View.SYSTEM_UI_FLAG_HIDE_NAVIGATION or
            View.SYSTEM_UI_FLAG_IMMERSIVE_STICKY or
            View.SYSTEM_UI_FLAG_LAYOUT_FULLSCREEN or
            View.SYSTEM_UI_FLAG_LAYOUT_HIDE_NAVIGATION
        )
        setContentView(R.layout.activity_main)

        initViews()
        setupArtworkCardOutline()
        setupButtonListeners()
        showIdleState()

        nativeBridge = NativeBridge()
        nativeBridge.initCore()

        dacpClient = DacpClient(this)

        // Start mDNS so iPhones discover OpenReceiver immediately
        mdnsAdvertiser = MdnsAdvertiser(this, deviceIdHex, "OpenReceiver")
        mdnsAdvertiser.start(7000, object : MdnsAdvertiser.Callback {
            override fun onRegistered(serviceName: String) {
                android.util.Log.i("MainActivity", "mDNS registered: $serviceName")
            }
            override fun onFailed(errorCode: Int) {
                android.util.Log.e("MainActivity", "mDNS registration failed: $errorCode")
            }
        })

        // Start AirPlay RTSP server
        airPlayServer = AirPlayServer(nativeBridge, deviceMac, this)
        airPlayServer.start()
    }

    private fun initViews() {
        fluidBackground = findViewById(R.id.fluidBackground)
        artworkCard = findViewById(R.id.artworkCard)
        ivArtwork = findViewById(R.id.ivArtwork)
        tvTitle = findViewById(R.id.tvTitle)
        tvArtist = findViewById(R.id.tvArtist)
        tvTimeLeft = findViewById(R.id.tvTimeLeft)
        tvTimeRight = findViewById(R.id.tvTimeRight)
        progressTrack = findViewById(R.id.progressTrack)

        albumColumn = findViewById(R.id.albumColumn)
        lyricsColumn = findViewById(R.id.lyricsColumn)
        lyricPanel = findViewById(R.id.lyricPanel)
        tvPrevPrevLyric = findViewById(R.id.tvPrevPrevLyric)
        tvPrevLyric = findViewById(R.id.tvPrevLyric)
        tvCurrentLyric = findViewById(R.id.tvCurrentLyric)
        tvNextLyric = findViewById(R.id.tvNextLyric)
        tvNextNextLyric = findViewById(R.id.tvNextNextLyric)
        tvNextNextNextLyric = findViewById(R.id.tvNextNextNextLyric)
        noLyricsText = findViewById(R.id.noLyricsText)

        btnLyrics = findViewById(R.id.btnLyrics)
        ivLyricsIcon = findViewById(R.id.ivLyricsIcon)
    }

    private fun setupArtworkCardOutline() {
        // Guarantee clean, smooth 16dp rounded corners on Android TV 9
        val radius = 16f * resources.displayMetrics.density
        artworkCard.outlineProvider = object : ViewOutlineProvider() {
            override fun getOutline(view: View, outline: Outline) {
                outline.setRoundRect(0, 0, view.width, view.height, radius)
            }
        }
        artworkCard.clipToOutline = true
    }

    private fun setupButtonListeners() {
        val focusListener = View.OnFocusChangeListener { v, hasFocus ->
            val targetScale = if (hasFocus) 1.15f else 1.0f
            v.animate()
                .scaleX(targetScale)
                .scaleY(targetScale)
                .setDuration(200)
                .setInterpolator(DecelerateInterpolator(1.5f))
                .start()
            val baseAlpha = if (lyricsEnabled) 1.0f else 0.5f
            v.animate().alpha(if (hasFocus) baseAlpha else baseAlpha * 0.7f).setDuration(200).start()
        }
            
        btnLyrics.onFocusChangeListener = focusListener
        btnLyrics.alpha = if (lyricsEnabled) 0.7f else 0.35f

        btnLyrics.setOnClickListener {
            toggleLyrics()
        }
    }

    private fun animateButtonPress(v: View) {
        val scaleDown = AnimatorSet().apply {
            playTogether(
                ObjectAnimator.ofFloat(v, "scaleX", 1f, 0.88f),
                ObjectAnimator.ofFloat(v, "scaleY", 1f, 0.88f)
            )
            duration = 80
        }
        val scaleUp = AnimatorSet().apply {
            playTogether(
                ObjectAnimator.ofFloat(v, "scaleX", 0.88f, 1f),
                ObjectAnimator.ofFloat(v, "scaleY", 0.88f, 1f)
            )
            duration = 120
            interpolator = DecelerateInterpolator()
        }
        scaleDown.addListener(object : Animator.AnimatorListener {
            override fun onAnimationEnd(a: Animator) { scaleUp.start() }
            override fun onAnimationStart(a: Animator) {}
            override fun onAnimationCancel(a: Animator) {}
            override fun onAnimationRepeat(a: Animator) {}
        })
        scaleDown.start()
    }

    private fun toggleLyrics() {
        lyricsEnabled = !lyricsEnabled
        
        val baseAlpha = if (lyricsEnabled) 1.0f else 0.5f
        btnLyrics.alpha = if (btnLyrics.hasFocus()) baseAlpha else baseAlpha * 0.7f
        
        btnLyrics.setBackgroundResource(
            if (lyricsEnabled) R.drawable.bg_lyrics_btn_active else R.drawable.bg_lyrics_btn
        )

        if (lyricsEnabled && activeLyrics == null && currentTrackTitle.isNotEmpty()) {
            // Trigger fetch if user re-enabled lyrics and none are loaded yet
            val t = currentTrackTitle
            val art = currentTrackArtist
            val seq = ++lyricsFetchSeq
            lyricsExecutor.execute {
                val lines = LyricsFetcher.fetch(t, art, currentDurationSec)
                mainHandler.post {
                    if (seq == lyricsFetchSeq) {
                        activeLyrics = lines
                        currentLyricIndex = -999
                        if (lines != null && lines.isNotEmpty()) {
                            updateLyricsForPosition(currentPositionSec)
                        }
                        updateLyricsPanelVisibility()
                    }
                }
            }
        }

        updateLyricsPanelVisibility()
    }

    private fun updateLyricsPanelVisibility() {
        val hasLyrics = activeLyrics != null && activeLyrics!!.isNotEmpty()

        if (lyricsEnabled && hasLyrics) {
            // Two-column layout with lyrics (Stitch UI layout weights)
            albumColumn.layoutParams = LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.MATCH_PARENT, 4.0f)
            lyricsColumn.visibility = View.VISIBLE
            lyricsColumn.layoutParams = LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.MATCH_PARENT, 6.0f)
            albumColumn.gravity = Gravity.CENTER_VERTICAL or Gravity.END

            lyricPanel.visibility = View.VISIBLE
            noLyricsText.visibility = View.GONE
        } else if (lyricsEnabled && currentDurationSec > 0) {
            // Two-column layout with "Lyrics unavailable"
            albumColumn.layoutParams = LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.MATCH_PARENT, 4.0f)
            lyricsColumn.visibility = View.VISIBLE
            lyricsColumn.layoutParams = LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.MATCH_PARENT, 6.0f)
            albumColumn.gravity = Gravity.CENTER_VERTICAL or Gravity.END

            lyricPanel.visibility = View.GONE
            noLyricsText.visibility = View.VISIBLE
        } else {
            // Single-column: Center the album art on screen (exact Windows parity)
            albumColumn.layoutParams = LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.MATCH_PARENT, 10f)
            lyricsColumn.visibility = View.GONE
            lyricsColumn.layoutParams = LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.MATCH_PARENT, 0f)
            albumColumn.gravity = Gravity.CENTER

            lyricPanel.visibility = View.GONE
            noLyricsText.visibility = View.GONE
        }
    }

    // ────────────────────────────────────────────────── Idle / Connected state

    private fun showIdleState() {
        tvTitle.text = "Ready for AirPlay"
        tvArtist.text = "Select 'OpenReceiver' on your iPhone"
        tvTimeLeft.text = "0:00"
        tvTimeRight.text = "-0:00"
        setProgressScale(0f)
        currentTrackTitle = ""
        currentTrackArtist = ""
        clearLyrics()
        ivArtwork.setImageDrawable(null)
        fluidBackground.setArtwork(null)
        updateLyricsPanelVisibility()
    }

    private fun clearLyrics() {
        activeLyrics = null
        currentLyricIndex = -999
        tvPrevPrevLyric.text = ""
        tvPrevLyric.text = ""
        tvCurrentLyric.text = ""
        tvNextLyric.text = ""
        tvNextNextLyric.text = ""
        tvNextNextNextLyric.text = ""
    }

    // ────────────────────────────────────────────────── Progress bar

    private fun setProgressScale(scale: Float) {
        progressTrack.progress = (scale.coerceIn(0f, 1f) * 1000).toInt()
    }

    // ────────────────────────────────────────────────── Time formatting

    private fun fmtTime(sec: Double): String {
        val s = sec.coerceAtLeast(0.0).toLong()
        return "%d:%02d".format(s / 60, s % 60)
    }

    private fun fmtRemaining(pos: Double, dur: Double): String {
        val rem = (dur - pos).coerceAtLeast(0.0).toLong()
        return "-%d:%02d".format(rem / 60, rem % 60)
    }

    // ────────────────────────────────────────────────── Lyrics sync & animation

    private fun updateLyricsForPosition(pos: Double) {
        val lines = activeLyrics ?: return
        if (lines.isEmpty()) return

        var activeIdx = -1
        for (i in lines.indices) {
            if (pos >= lines[i].timeSeconds) activeIdx = i else break
        }

        if (activeIdx == currentLyricIndex) return
        currentLyricIndex = activeIdx

        if (activeIdx >= 0) {
            val prevPrev = if (activeIdx > 1) lines[activeIdx - 2].text else ""
            val prev = if (activeIdx > 0) lines[activeIdx - 1].text else ""
            val curr = lines[activeIdx].text
            val next = if (activeIdx + 1 < lines.size) lines[activeIdx + 1].text else ""
            val nextNext = if (activeIdx + 2 < lines.size) lines[activeIdx + 2].text else ""
            val nextNextNext = if (activeIdx + 3 < lines.size) lines[activeIdx + 3].text else ""

            tvPrevPrevLyric.text = prevPrev
            tvPrevLyric.text = prev
            animateLyricChange(tvCurrentLyric, curr)
            tvNextLyric.text = next
            tvNextNextLyric.text = nextNext
            tvNextNextNextLyric.text = nextNextNext
        } else {
            // Before the first line starts (intro) -> Apple Music 3 dots
            tvPrevPrevLyric.text = ""
            tvPrevLyric.text = ""
            animateLyricChange(tvCurrentLyric, "• • •")
            tvNextLyric.text = if (lines.isNotEmpty()) lines[0].text else ""
            tvNextNextLyric.text = if (lines.size > 1) lines[1].text else ""
            tvNextNextNextLyric.text = if (lines.size > 2) lines[2].text else ""
        }
    }

    private fun animateLyricChange(tv: TextView, newText: String) {
        if (tv.text == newText) return
        tv.text = newText
        // Apple Music & Windows QuarticEaseOut scale (0.92 -> 1.0) and opacity (0.45 -> 1.0)
        tv.scaleX = 0.92f
        tv.scaleY = 0.92f
        tv.alpha = 0.45f
        tv.animate()
            .scaleX(1.0f)
            .scaleY(1.0f)
            .alpha(1.0f)
            .setDuration(420)
            .setInterpolator(DecelerateInterpolator(2.0f))
            .start()
    }

    // ────────────────────────────────────────────────── AirPlayServer.Listener callbacks

    override fun onServerStarted(port: Int) {
        android.util.Log.i("MainActivity", "AirPlay server started on port $port")
    }

    override fun onServerError(message: String) {
        android.util.Log.e("MainActivity", "Server error: $message")
    }

    override fun onSessionStarted(clientIp: String) {
        runOnUiThread {
            tvTitle.text = "iPhone Connected"
            tvArtist.text = "Select audio to start streaming"
        }
    }

    override fun onSessionEnded() {
        runOnUiThread { showIdleState() }
    }

    override fun onTrackMetadata(title: String?, artist: String?, album: String?, durationSec: Double) {
        val t = title?.trim() ?: ""
        val art = artist?.trim() ?: ""
        val alb = album?.trim() ?: ""
        val dur = if (durationSec > 0) durationSec else 0.0

        val trackChanged = (t.isNotEmpty() && (t != currentTrackTitle || art != currentTrackArtist))
        if (t.isNotEmpty()) currentTrackTitle = t
        if (art.isNotEmpty()) currentTrackArtist = art
        if (dur > 0) currentDurationSec = dur

        runOnUiThread {
            tvTitle.text = if (t.isNotEmpty()) t else "Playing Audio"
            val artistText = when {
                art.isNotEmpty() && alb.isNotEmpty() -> "$art — $alb"
                art.isNotEmpty() -> art
                alb.isNotEmpty() -> alb
                else -> "AirPlay Stream"
            }
            tvArtist.text = artistText
            if (dur > 0) {
                tvTimeRight.text = fmtRemaining(currentPositionSec, dur)
            }
        }

        // Only clear lyrics and fetch if track title or artist actually changed!
        if (trackChanged) {
            currentPositionSec = 0.0
            lastProgressPos = -1.0
            currentLyricIndex = -999
            val seq = ++lyricsFetchSeq

            runOnUiThread {
                tvTimeLeft.text = "0:00"
                setProgressScale(0f)
                clearLyrics()
                updateLyricsPanelVisibility()
            }

            if (lyricsEnabled) {
                lyricsExecutor.execute {
                    val lines = LyricsFetcher.fetch(t, art, currentDurationSec)
                    mainHandler.post {
                        if (seq == lyricsFetchSeq) {
                            activeLyrics = lines
                            currentLyricIndex = -999
                            if (lines != null && lines.isNotEmpty()) {
                                updateLyricsForPosition(currentPositionSec)
                            }
                            updateLyricsPanelVisibility()
                        }
                    }
                }
            }
        }
    }

    override fun onAlbumArt(bitmap: Bitmap) {
        currentAlbumBitmap = bitmap
        runOnUiThread {
            // Crossfade album art
            val old = ivArtwork.drawable
            if (old != null) {
                val td = TransitionDrawable(arrayOf(old, BitmapDrawable(resources, bitmap)))
                ivArtwork.setImageDrawable(td)
                td.startTransition(500)
            } else {
                ivArtwork.setImageBitmap(bitmap)
            }
            // Update fluid ambient mesh background
            fluidBackground.setArtwork(bitmap)
        }
    }

    override fun onPlaybackState(isPlaying: Boolean) {
        this.isPlaying = isPlaying
    }

    override fun onProgress(positionSec: Double, durationSec: Double) {
        if (durationSec > 0) currentDurationSec = durationSec
        currentPositionSec = positionSec

        // Small jitter filter to prevent UI thrashing but allow smooth lyric sync
        if (lastProgressPos >= 0 && abs(positionSec - lastProgressPos) < 0.05) return
        lastProgressPos = positionSec

        runOnUiThread {
            val dur = currentDurationSec
            tvTimeLeft.text = fmtTime(positionSec)
            tvTimeRight.text = if (dur > 0) fmtRemaining(positionSec, dur) else "-0:00"
            if (dur > 0) {
                setProgressScale((positionSec / dur).toFloat())
            }
            if (activeLyrics != null) {
                // Snappy sync: highlight line exactly 0.3s before its actual timestamp
                updateLyricsForPosition(positionSec + 0.3)
            }
        }
    }

    override fun onVolume(fraction: Float) {
        // Volume UI removed
    }

    override fun onRemoteControlInfo(clientIp: String, dacpId: String?, activeRemote: String?) {
        dacpClient.update(clientIp, dacpId, activeRemote)
    }

    // ────────────────────────────────────────────────── Lifecycle

    override fun onDestroy() {
        super.onDestroy()
        airPlayServer.stop()
        mdnsAdvertiser.stop()
        dacpClient.shutdown()
        nativeBridge.shutdownCore()
        lyricsExecutor.shutdownNow()
    }
}
