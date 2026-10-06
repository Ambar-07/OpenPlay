#include <jni.h>
#include <string>
#include "../../../../../../../core/state/StateManager.h"

using namespace openreceiver::state;

static JavaVM* g_jvm = nullptr;
static jobject g_nativeBridgeObj = nullptr;

class JniObserver : public IStateObserver {
public:
    void onTrackInfoChanged(const TrackInfo& track) override {
        if (!g_jvm || !g_nativeBridgeObj) return;

        JNIEnv* env;
        // JNI calls from C++ background threads require attaching the thread to the JVM
        if (g_jvm->AttachCurrentThread((void**)&env, nullptr) == JNI_OK) {
            jclass clazz = env->GetObjectClass(g_nativeBridgeObj);
            // Method signature: void onTrackInfoChanged(String, String, String, double)
            jmethodID methodId = env->GetMethodID(clazz, "onTrackInfoChanged", "(Ljava/lang/String;Ljava/lang/String;Ljava/lang/String;D)V");

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
        if (g_jvm->AttachCurrentThread((void**)&env, nullptr) == JNI_OK) {
            jclass clazz = env->GetObjectClass(g_nativeBridgeObj);
            // Method signature: void onPlaybackStateChanged(int, double)
            jmethodID methodId = env->GetMethodID(clazz, "onPlaybackStateChanged", "(ID)V");

            if (methodId) {
                env->CallVoidMethod(g_nativeBridgeObj, methodId, static_cast<int>(state.status), state.position);
            }
            env->DeleteLocalRef(clazz);
            g_jvm->DetachCurrentThread();
        }
    }
};

static JniObserver g_observer;

extern "C" JNIEXPORT jint JNICALL JNI_OnLoad(JavaVM* vm, void* reserved) {
    g_jvm = vm;
    return JNI_VERSION_1_6;
}

extern "C" JNIEXPORT void JNICALL
Java_com_openreceiver_core_NativeBridge_initCore(JNIEnv* env, jobject thiz) {
    if (g_nativeBridgeObj) {
        env->DeleteGlobalRef(g_nativeBridgeObj);
    }
    // Must create a Global Ref to invoke callbacks asynchronously later
    g_nativeBridgeObj = env->NewGlobalRef(thiz);
    
    StateManager::getInstance().addObserver(&g_observer);
}

extern "C" JNIEXPORT void JNICALL
Java_com_openreceiver_core_NativeBridge_shutdownCore(JNIEnv* env, jobject thiz) {
    StateManager::getInstance().removeObserver(&g_observer);
    
    if (g_nativeBridgeObj) {
        env->DeleteGlobalRef(g_nativeBridgeObj);
        g_nativeBridgeObj = nullptr;
    }
}
