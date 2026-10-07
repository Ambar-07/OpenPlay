#include <jni.h>
#include <string>
#include <android/log.h>
#include "state/StateManager.h"

#define LOG_TAG "JniBridge"
#define LOGI(...) __android_log_print(ANDROID_LOG_INFO, LOG_TAG, __VA_ARGS__)
#define LOGE(...) __android_log_print(ANDROID_LOG_ERROR, LOG_TAG, __VA_ARGS__)

using namespace openreceiver::state;

static JavaVM* g_jvm = nullptr;
static jobject g_nativeBridgeObj = nullptr;

class JniObserver : public IStateObserver {
public:
    void onTrackInfoChanged(const TrackInfo& track) override {
        if (!g_jvm || !g_nativeBridgeObj) return;

        JNIEnv* env;
        if (g_jvm->AttachCurrentThread(&env, nullptr) == JNI_OK) {
            jclass clazz = env->GetObjectClass(g_nativeBridgeObj);
            jmethodID methodId = env->GetMethodID(clazz, "onTrackInfoChanged",
                "(Ljava/lang/String;Ljava/lang/String;Ljava/lang/String;D)V");
            if (methodId) {
                jstring jTitle = env->NewStringUTF(track.title.c_str());
                jstring jArtist = env->NewStringUTF(track.artist.c_str());
                jstring jAlbum = env->NewStringUTF(track.album.c_str());
                env->CallVoidMethod(g_nativeBridgeObj, methodId, jTitle, jArtist, jAlbum, track.duration);
                env->DeleteLocalRef(jTitle);
                env->DeleteLocalRef(jArtist);
                env->DeleteLocalRef(jAlbum);
            }
            env->DeleteLocalRef(clazz);
            g_jvm->DetachCurrentThread();
        }
    }

    void onPlaybackStateChanged(const PlaybackState& state) override {
        if (!g_jvm || !g_nativeBridgeObj) return;

        JNIEnv* env;
        if (g_jvm->AttachCurrentThread(&env, nullptr) == JNI_OK) {
            jclass clazz = env->GetObjectClass(g_nativeBridgeObj);
            jmethodID methodId = env->GetMethodID(clazz, "onPlaybackStateChanged", "(ID)V");
            if (methodId) {
                env->CallVoidMethod(g_nativeBridgeObj, methodId,
                    static_cast<int>(state.status), state.position);
            }
            env->DeleteLocalRef(clazz);
            g_jvm->DetachCurrentThread();
        }
    }
};

static JniObserver g_observer;

extern "C" JNIEXPORT jint JNICALL JNI_OnLoad(JavaVM* vm, void* reserved) {
    g_jvm = vm;
    LOGI("JNI_OnLoad called");
    return JNI_VERSION_1_6;
}

extern "C" JNIEXPORT void JNICALL
Java_com_openreceiver_core_NativeBridge_initCoreNative(JNIEnv* env, jobject thiz) {
    if (g_nativeBridgeObj) {
        env->DeleteGlobalRef(g_nativeBridgeObj);
    }
    g_nativeBridgeObj = env->NewGlobalRef(thiz);
    StateManager::getInstance().addObserver(&g_observer);
    LOGI("Core initialized");
}

extern "C" JNIEXPORT void JNICALL
Java_com_openreceiver_core_NativeBridge_shutdownCoreNative(JNIEnv* env, jobject thiz) {
    StateManager::getInstance().removeObserver(&g_observer);
    if (g_nativeBridgeObj) {
        env->DeleteGlobalRef(g_nativeBridgeObj);
        g_nativeBridgeObj = nullptr;
    }
    LOGI("Core shut down");
}

extern "C" {
#include "airplay/alac.h"
}

extern "C" JNIEXPORT jlong JNICALL
Java_com_openreceiver_core_NativeBridge_initAlacNative(JNIEnv* env, jobject thiz) {
    alac_file* alac = alac_create(16, 2);
    if (!alac) {
        LOGE("alac_create returned null");
        return 0;
    }
    alac->setinfo_max_samples_per_frame = 352;
    alac->setinfo_7a = 0;
    alac->setinfo_sample_size = 16;
    alac->setinfo_rice_historymult = 40;
    alac->setinfo_rice_initialhistory = 10;
    alac->setinfo_rice_kmodifier = 14;
    alac->setinfo_7f = 2;
    alac->setinfo_80 = 255;
    alac->setinfo_82 = 0;
    alac->setinfo_86 = 0;
    alac->setinfo_8a_rate = 44100;
    alac_allocate_buffers(alac);
    LOGI("ALAC decoder initialized at %p", alac);
    return reinterpret_cast<jlong>(alac);
}

extern "C" JNIEXPORT jint JNICALL
Java_com_openreceiver_core_NativeBridge_decodeAlacNative(
    JNIEnv* env, jobject thiz, jlong alacPtr, jbyteArray input, jint inputLen, jbyteArray output) {
    if (!alacPtr || !input || !output) return 0;
    alac_file* alac = reinterpret_cast<alac_file*>(alacPtr);
    
    jbyte* inBuf = env->GetByteArrayElements(input, nullptr);
    jbyte* outBuf = env->GetByteArrayElements(output, nullptr);
    
    if (!inBuf || !outBuf) {
        if (inBuf) env->ReleaseByteArrayElements(input, inBuf, JNI_ABORT);
        if (outBuf) env->ReleaseByteArrayElements(output, outBuf, JNI_ABORT);
        return 0;
    }

    int outSize = alac->setinfo_max_samples_per_frame * alac->bytespersample;
    alac_decode_frame(alac,
        reinterpret_cast<unsigned char*>(inBuf),
        reinterpret_cast<void*>(outBuf),
        &outSize);
    
    env->ReleaseByteArrayElements(input, inBuf, JNI_ABORT);
    env->ReleaseByteArrayElements(output, outBuf, 0);
    
    return outSize;
}

extern "C" JNIEXPORT void JNICALL
Java_com_openreceiver_core_NativeBridge_freeAlacNative(JNIEnv* env, jobject thiz, jlong alacPtr) {
    if (alacPtr) {
        alac_file* alac = reinterpret_cast<alac_file*>(alacPtr);
        alac_free(alac);
        LOGI("ALAC decoder freed");
    }
}
