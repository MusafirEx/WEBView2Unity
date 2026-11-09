# WEBView2Unity: Windows x64 native CEF installation

**The source ZIP and Git UPM package are NOT native CEF runtimes.** If Unity shows "CEF files missing: libcef.dll or WEBView2UnityCEFSubprocess.exe", the source directory was selected instead of a built CEF runtime directory.

CEF is a multi-process browser runtime (Minimal Windows x64 SDK is ~165 MB compressed). The native C++ bridge must be built with Visual Studio 2022, and runtime assets staged together.

> **Experimental, not safe for sensitive accounts.** Sandbox is DISABLED in the native prototype. Only test with a non-sensitive site such as https://example.com/. Do not log into ChatGPT, email or bank accounts until sandboxing and browser permissions are secured.

## Option A — GitHub Actions artifact

See GitHub Actions > Windows CEF Native Prototype. Once a Windows build **succeeds**, download its artifact named WEBView2Unity-CEF-Windows-x64-PROTOTYPE and extract its contents to:

    %LOCALAPPDATA%\WEBView2Unity\Runtime

The selected folder in Unity must DIRECTLY contain WEBView2UnityCEF.dll, WEBView2UnityCEFSubprocess.exe, libcef.dll and the CEF Resources/locales files. The CI artifact is a source-linked unsandboxed prototype, not a production release.

## Option B — local build using Windows PowerShell

1. Install Visual Studio 2022 Desktop development with C++, a Windows SDK and CMake in PATH. Windows 10/11 provides tar.
2. Clone the feature/upm-cef-prototype branch of https://github.com/MusafirEx/WEBView2Unity, or locate the package folder with Native~.
3. Open Windows PowerShell in the repository root:

        .\Native~\Build-Windows.ps1

   The script explicitly downloads the pinned CEF Windows x64 Minimal SDK from the official CEF automated builds site, checks its upstream SHA1, extracts it, builds our Release C++ DLL and helper EXE, and stages all runtime dependencies under %LOCALAPPDATA%\WEBView2Unity\Runtime.

   Default version (published 5 October 2026): CEF 154.0.34+g14c5a08+chromium-154.0.8037.98. Review the official site for security updates before opening untrusted sites.

   To use your own pre-downloaded matching SDK instead:

        .\Native~\Build-Windows.ps1 -CefRoot "C:\cef\cef_binary_154.0.34+g14c5a08+chromium-154.0.8037.98_windows64_minimal"

4. In Unity open Window > WEBView2Unity > Browser.
5. Choose Use Default Runtime Folder or browse to %LOCALAPPDATA%\WEBView2Unity\Runtime.
6. Enter https://example.com/ and click Initialize CEF and Open Browser.

Our bridge bootstrap calls LoadLibraryExW with LOAD_LIBRARY_SEARCH_DLL_LOAD_DIR, so placing the native files inside Unity Assets/Plugins is not needed in this prototype.

## Troubleshooting

- CEF runtime incomplete: selected source ZIP or wrong folder; choose one containing all 3 required files.
- Windows error 126: native dependency missing; check CEF DLLs, VC++ redistributable.
- Windows error 193: wrong architecture; compile for x64.
- EntryPointNotFound: bridge DLL and C# declarations mismatched; build from the same repo commit.
- CefInitialize failed: missing resources or startup/cache issue; check Unity Editor.log.
- Blank page: native subprocess, render loop, graphics or input integration still needs debugging.

## Known limitations

Embedded rendering has NOT been verified in Unity 2022.3. The CEF source and loader are experimental and may require additional native fixes after Windows compilation. Popup menus, file chooser, IME, permission prompts, multi-window lifecycle, sandbox and secure JavaScript integration are not supported yet. Game Runtime support is a future phase.
