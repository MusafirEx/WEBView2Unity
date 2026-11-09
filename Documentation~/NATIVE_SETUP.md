# Native CEF setup (Windows x64 only)

**The Git UPM package deliberately does not bundle CEF or a compiled native DLL.** Install the package to obtain the Editor shell and fallback; actual embedded rendering needs a compiled native engine. The implementation has **not yet been compiled or tested in a Windows Unity Editor process**.

## Requirements

- Unity 2022.3 LTS on Windows x64.
- Visual Studio 2022 with C++ Desktop Development and CMake 3.21+.
- A compatible CEF Windows x64 **Standard Distribution** from https://cef-builds.spotifycdn.com/index.html .
- Review the exact CEF version, DLL dependencies, security updates and BSD/Chromium licences before redistribution.

## Build

1. Extract the CEF standard distribution to `C:\cef\cef_binary_..._windows64`. It should contain `include/`, `libcef_dll/`, `Release/`, `Resources/`, and `cmake/`.
2. At the repository root run:

   ```powershell
   cmake -S Native~ -B build/cef -A x64 -DCEF_ROOT="C:/cef/cef_binary_..._windows64"
   cmake --build build/cef --config Release
   ```

3. Expected outputs: `WEBView2UnityCEF.dll` and `WEBView2UnityCEFSubprocess.exe`.
4. Import the compiled DLL into Unity as a Windows x64 native plug-in. Ensure that all required CEF runtime DLLs (including `libcef.dll`) are found by Windows' native DLL loader. This native DLL installation path still requires integration testing.
5. Configure a runtime folder containing `libcef.dll`, `WEBView2UnityCEFSubprocess.exe`, and the resource files copied from CEF `Resources/` (typically `.pak`, `.dat` and `locales/`). **Do not put the helper executable in Unity's managed script folders.**
6. In Unity select **Window > WEBView2Unity > Browser**. Choose the CEF Runtime Folder, then choose **Initialize CEF and Open Browser**.

## Architecture

```text
Unity EditorWindow / IMGUI
  -> WebViewBrowser (Runtime API)
     -> WEBView2UnityCEF.dll (C++ ABI)
        -> CEF OSR paint buffer (BGRA) + input
           -> WEBView2UnityCEFSubprocess.exe
```

The browser is painted into a Unity `Texture2D` and receives keyboard/mouse/scroll events via the C ABI. Browser UI and native loop are run from the Editor main thread, with explicit shutdown at editor quit rather than managed domain reload.

## Unverified items / known limitations

- **No native binary has yet been compiled or deployed into Unity.** Build compatibility, native DLL loading, CEF subprocess startup, GPU graphics, texture orientation and render timing require real Windows tests.
- Browser input excludes complex IME, drag and drop, accessibility, context menus, popup windows, permission prompts and file dialogs.
- Native CEF sandbox is disabled in this prototype. **Not production safe.**
- Chromium does not guarantee that every web service, including ChatGPT sign-in, will accept embedded login.
- Browser process shutdown, managed domain reload, multi-window usage and lifecycle must be stress-tested before public release.
- Do not download binaries silently or grant arbitrary JavaScript access to Unity. Any future JS bridge must authenticate origins and constrain actions.
