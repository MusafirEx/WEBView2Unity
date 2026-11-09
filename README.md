# WEBView2Unity

Experimental **Chromium Embedded Framework (CEF)** web browser for Unity by MusafirEx. Our first target is a **dockable Unity Editor browser** on Windows x64 / Unity 2022.3 LTS. A shared C# runtime surface is included for a later in-game browser.

> **Status — v0.1.0 source prototype, NOT a working embedded browser release.** The UPM package provides the Editor menu, URL handling and external browser fallback. The native CEF bridge is provided as **uncompiled C++ source**. It requires a compatible CEF SDK, a native build, deployment and on-machine Unity testing before embedded pages can display.

## Unity Package Manager (Git URL)

Open **Window > Package Manager > + > Add package from git URL**, then enter:

```text
https://github.com/MusafirEx/WEBView2Unity.git
```

Or add to `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.musafirex.webview2unity": "https://github.com/MusafirEx/WEBView2Unity.git"
  }
}
```

The `package.json` manifest is at the repository root; do not use `?path=`.

## Editor

Open **Window > WEBView2Unity > Browser**. The dockable window includes an address bar, Back, Forward, Refresh and **External** fallback. Embedded rendering requires manually building and installing CEF; see [native setup](Documentation~/NATIVE_SETUP.md).

## Progress

| Capability | Status |
| --- | --- |
| Root UPM manifest, Runtime & Editor asmdefs | Implemented in source (Unity import test pending) |
| Editor browser shell + HTTP(S) validation | Implemented in source |
| CEF off-screen paint C++ bridge and helper exe | Source prototype; native compilation unverified |
| Mouse/keyboard/scroll forwarding | Source prototype; requires native testing |
| Embedded web pages in Unity Editor | **Not yet verified or shipped** |
| In-game Unity Runtime browser UI | Planned, shared API prepared |
| Tabs, downloads, IME, authentication, file dialogs | Planned |

## Technology

Unlike Microsoft's proprietary WebView2 runtime, this project targets **Chromium Embedded Framework (CEF)**, an open source Chromium embedding framework. CEF supports off-screen rendering, which can be drawn as a Unity texture. A dedicated subprocess exe avoids launching Unity.exe as a CEF renderer process.

## Security and compatibility

Native engine is experimental. **Do not sign in to sensitive accounts or use untrusted websites until the native implementation is security-reviewed and tested.** Chromium and CEF binary redistributions require compliance with their respective third-party notices. The native engine currently targets Windows x64 only; other targets will require porting.

## Development

Unity 2022.3.22f1 is the initial test target. Contributions via pull requests and GitHub issues are welcome.
