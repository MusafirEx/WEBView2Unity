# WEBView2Unity

**Unity 2022.3 LTS — Windows x64 browser plugin**, by MusafirEx. Primary target: a browser docked in the Unity Editor. Shared C# API lays groundwork for a future Game Runtime browser.

> v0.1.0: Experimental source prototype. The Editor UI / external browser fallback exist, but CEF native executables are NOT shipped with the UPM package. Embedded web rendering is not yet validated.

## Install via UPM

Add from Git URL:

    https://github.com/MusafirEx/WEBView2Unity.git#feature/upm-cef-prototype

Use this feature branch until PR #1 is merged: https://github.com/MusafirEx/WEBView2Unity/pull/1

## Test inside Unity

1. Open Window > WEBView2Unity > Browser. External opens your default Windows browser without CEF.
2. For an EMBEDDED page, obtain a successful build artifact from Actions > Windows CEF Native Prototype, or build the native runtime on Windows:

        .\Native~\Build-Windows.ps1

3. Choose CEF Runtime Folder: %LOCALAPPDATA%\WEBView2Unity\Runtime, NOT the source ZIP or repository folder.
4. Enter https://example.com/ and choose Initialize CEF and Open Browser.

See [Native installation and troubleshooting](Documentation~/NATIVE_SETUP.md).

## Scope and status

| Feature | Status |
| --- | --- |
| Git UPM manifest and Editor/Runtime asmdefs | Source implemented |
| Dockable EditorWindow, URL handling and external fallback | Source implemented |
| Native CEF off-screen bridge + subprocess | Source prototype, Windows compilation pending |
| PowerShell download/build/stage with checksum verification | Source implemented, Windows test pending |
| Actual embedded rendering in Unity | NOT VERIFIED |
| Game Runtime UI | Later |
| Sandboxing, popups, IME, permissions, authenticated websites | Not implemented |

CEF is an open-source Chromium embedding framework, not Microsoft WebView2. The current native prototype disables Chromium sandbox; do not use sensitive services or untrusted sites.

GitHub Actions validates UPM assets and can build the Windows native prototype. Real Unity Editor Windows testing remains necessary.
