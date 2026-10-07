package com.openreceiver.ui

import android.animation.ValueAnimator
import android.content.Context
import android.graphics.Bitmap
import android.graphics.Canvas
import android.graphics.Color
import android.graphics.Paint
import android.graphics.RadialGradient
import android.graphics.Shader
import android.os.SystemClock
import android.util.AttributeSet
import android.view.View
import android.view.animation.LinearInterpolator
import androidx.palette.graphics.Palette
import java.util.concurrent.Executors
import kotlin.math.cos
import kotlin.math.sin

class FluidBackgroundView @JvmOverloads constructor(
    context: Context,
    attrs: AttributeSet? = null,
    defStyleAttr: Int = 0
) : View(context, attrs, defStyleAttr) {

    private val bgExecutor = Executors.newSingleThreadExecutor()
    private val paint1 = Paint(Paint.ANTI_ALIAS_FLAG)
    private val paint2 = Paint(Paint.ANTI_ALIAS_FLAG)
    private val paint3 = Paint(Paint.ANTI_ALIAS_FLAG)
    private val paint4 = Paint(Paint.ANTI_ALIAS_FLAG)
    
    // Background colors
    private var color1 = Color.parseColor("#1a1a24")
    private var color2 = Color.parseColor("#2a1a1a")
    private var color3 = Color.parseColor("#1a2a1a")
    private var color4 = Color.parseColor("#1a1a1a")

    // Target colors for crossfading
    private var targetColor1 = color1
    private var targetColor2 = color2
    private var targetColor3 = color3
    private var targetColor4 = color4

    private var viewWidth = 0
    private var viewHeight = 0
    private val startTime = SystemClock.uptimeMillis()
    private var isAnimating = true
    private var colorAnimator: ValueAnimator? = null

    init {
        setWillNotDraw(false)
    }

    override fun onSizeChanged(w: Int, h: Int, oldw: Int, oldh: Int) {
        super.onSizeChanged(w, h, oldw, oldh)
        viewWidth = w
        viewHeight = h
        updateGradients()
    }

    fun setArtwork(bitmap: Bitmap?) {
        if (bitmap == null) {
            transitionToColors(Color.parseColor("#1a1a24"), Color.parseColor("#2a1a1a"), Color.parseColor("#1a2a1a"), Color.parseColor("#1a1a1a"))
            return
        }

        bgExecutor.execute {
            val p = Palette.from(bitmap).generate()
            val dom = p.getDominantColor(Color.parseColor("#1a1a24"))
            val vib = p.getVibrantColor(dom)
            val muted = p.getMutedColor(dom)
            val darkVib = p.getDarkVibrantColor(dom)

            post {
                transitionToColors(vib, dom, muted, darkVib)
            }
        }
    }

    private fun transitionToColors(c1: Int, c2: Int, c3: Int, c4: Int) {
        targetColor1 = adjustAlpha(c1, 180)
        targetColor2 = adjustAlpha(c2, 180)
        targetColor3 = adjustAlpha(c3, 180)
        targetColor4 = adjustAlpha(c4, 180)

        val start1 = color1
        val start2 = color2
        val start3 = color3
        val start4 = color4

        colorAnimator?.cancel()
        colorAnimator = ValueAnimator.ofFloat(0f, 1f).apply {
            duration = 1500
            interpolator = LinearInterpolator()
            addUpdateListener { anim ->
                val fraction = anim.animatedFraction
                color1 = blendColors(start1, targetColor1, fraction)
                color2 = blendColors(start2, targetColor2, fraction)
                color3 = blendColors(start3, targetColor3, fraction)
                color4 = blendColors(start4, targetColor4, fraction)
                updateGradients()
            }
            start()
        }
    }

    private fun blendColors(color1: Int, color2: Int, ratio: Float): Int {
        val inverseRatio = 1f - ratio
        val a = (Color.alpha(color1) * inverseRatio + Color.alpha(color2) * ratio).toInt()
        val r = (Color.red(color1) * inverseRatio + Color.red(color2) * ratio).toInt()
        val g = (Color.green(color1) * inverseRatio + Color.green(color2) * ratio).toInt()
        val b = (Color.blue(color1) * inverseRatio + Color.blue(color2) * ratio).toInt()
        return Color.argb(a, r, g, b)
    }

    private fun adjustAlpha(color: Int, alpha: Int): Int {
        return Color.argb(alpha, Color.red(color), Color.green(color), Color.blue(color))
    }

    private fun updateGradients() {
        if (viewWidth <= 0 || viewHeight <= 0) return
        val radius = maxOf(viewWidth, viewHeight).toFloat() * 0.8f

        paint1.shader = RadialGradient(0f, 0f, radius, color1, Color.TRANSPARENT, Shader.TileMode.CLAMP)
        paint2.shader = RadialGradient(0f, 0f, radius, color2, Color.TRANSPARENT, Shader.TileMode.CLAMP)
        paint3.shader = RadialGradient(0f, 0f, radius, color3, Color.TRANSPARENT, Shader.TileMode.CLAMP)
        paint4.shader = RadialGradient(0f, 0f, radius, color4, Color.TRANSPARENT, Shader.TileMode.CLAMP)
    }

    override fun onDraw(canvas: Canvas) {
        super.onDraw(canvas)
        if (viewWidth <= 0 || viewHeight <= 0) return

        canvas.drawColor(Color.parseColor("#08080A")) // Deep base

        val time = (SystemClock.uptimeMillis() - startTime) / 1000f
        
        // Flowing blob positions using Lissajous curves
        val cx1 = viewWidth / 2f + (viewWidth / 3f) * sin(time * 0.4f)
        val cy1 = viewHeight / 2f + (viewHeight / 3f) * cos(time * 0.3f)
        
        val cx2 = viewWidth / 2f + (viewWidth / 3f) * cos(time * 0.5f)
        val cy2 = viewHeight / 2f + (viewHeight / 3f) * sin(time * 0.6f)
        
        val cx3 = viewWidth / 2f + (viewWidth / 4f) * sin(time * 0.7f + 2f)
        val cy3 = viewHeight / 2f + (viewHeight / 4f) * cos(time * 0.4f + 1f)

        val cx4 = viewWidth / 2f + (viewWidth / 2.5f) * cos(time * 0.3f + 3f)
        val cy4 = viewHeight / 2f + (viewHeight / 2.5f) * sin(time * 0.5f + 4f)

        canvas.save()
        canvas.translate(cx1, cy1)
        canvas.drawPaint(paint1)
        canvas.restore()

        canvas.save()
        canvas.translate(cx2, cy2)
        canvas.drawPaint(paint2)
        canvas.restore()

        canvas.save()
        canvas.translate(cx3, cy3)
        canvas.drawPaint(paint3)
        canvas.restore()
        
        canvas.save()
        canvas.translate(cx4, cy4)
        canvas.drawPaint(paint4)
        canvas.restore()

        // Dark acrylic dimming overlay so text stays readable
        canvas.drawColor(Color.argb(120, 0, 0, 0))

        if (isAnimating) {
            postInvalidateOnAnimation()
        }
    }

    override fun onAttachedToWindow() {
        super.onAttachedToWindow()
        isAnimating = true
        postInvalidateOnAnimation()
    }

    override fun onDetachedFromWindow() {
        super.onDetachedFromWindow()
        isAnimating = false
        colorAnimator?.cancel()
    }
}
