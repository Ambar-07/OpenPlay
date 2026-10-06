# Building OpenReceiver

Because OpenReceiver strictly segregates its C++ Core from its platform-specific UI shells, the build process requires platform-specific IDEs.

## 1. Building the Core (Cross-Platform)
The core library, RTSP parsers, and Lyrics Engine are written in standard C++17. 
You can build and run the unit tests using CMake:
```bash
mkdir build
cd build
cmake ..
cmake --build .
ctest -V
```

## 2. Building for Windows (WinUI 3)
1. Install **Visual Studio 2022** with the following workloads:
   - Desktop development with C++
   - Windows App SDK (C#)
2. Create a new "Blank App, Packaged (WinUI 3 in Desktop)" project in the `windows/` directory.
3. Replace the default generated `.xaml` and `.cs` files with the ones provided in `windows/app/`.
4. Compile the `windows/native/` bridge as a C++ DLL (`OpenReceiverCore.dll`) using MSVC and place it in the same output directory as your C# executable.
5. Hit **F5** in Visual Studio to run the Cinematic UI.

## 3. Packaging for Windows
1. Download and install **Inno Setup**.
2. Open `windows/packaging/OpenReceiver.iss`.
3. Click **Compile**. 
4. This will generate a standalone `OpenReceiver_Setup_x64.exe` installer that automatically handles Windows Defender Firewall exceptions for AirPlay UDP/TCP ports.

## 4. Building for Android TV
1. Install **Android Studio**.
2. Create a new **Android TV Blank Activity** project.
3. Copy the contents of `android/app/src/main/` over the default Android Studio structure.
4. Android Studio's Gradle sync will automatically detect `CMakeLists.txt`, compile the C++ `openreceiver_core.so` library via the NDK, and bind it to the Kotlin JNI interfaces.
5. Deploy to your Android TV or emulator via ADB.
