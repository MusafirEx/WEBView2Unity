using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;

namespace MusafirEx.WEBView2Unity
{
    /// <summary>
    /// Windows x64 CEF bootstrap. The bridge and CEF DLLs are loaded from the same
    /// user-selected runtime directory; nothing is downloaded or executed silently.
    /// </summary>
    public static class WebViewEngine
    {
        private const uint LoadLibrarySearchDllLoadDir = 0x00000100;
        private const uint LoadLibrarySearchDefaultDirs = 0x00001000;

#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        [DllImport("kernel32.dll", EntryPoint = "LoadLibraryExW",
            CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr LoadLibraryEx(
            string fileName, IntPtr reserved, uint flags);
#endif

        private static IntPtr bridgeHandle = IntPtr.Zero;

        public static bool IsInitialized { get; private set; }
        public static string LastError { get; private set; }

        public static string DefaultRuntimeDirectory
        {
            get
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "WEBView2Unity", "Runtime");
            }
        }

        public static bool TryInitialize(string cefRoot, string cacheDirectory)
        {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            if (IsInitialized) return true;
            LastError = null;

            if (string.IsNullOrWhiteSpace(cefRoot))
            {
                LastError = "Select the CEF Runtime folder. The source ZIP is not a CEF runtime. " +
                            "Run Native~/Build-Windows.ps1 after installing VS2022 C++ tools.";
                return false;
            }

            string root;
            try
            {
                root = Path.GetFullPath(cefRoot);
            }
            catch (Exception ex) when (ex is ArgumentException || ex is NotSupportedException ||
                                       ex is PathTooLongException)
            {
                LastError = "Invalid CEF runtime folder: " + ex.Message;
                return false;
            }

            string helper = Path.Combine(root, "WEBView2UnityCEFSubprocess.exe");
            string bridge = Path.Combine(root, "WEBView2UnityCEF.dll");
            string cef = Path.Combine(root, "libcef.dll");
            if (!File.Exists(cef) || !File.Exists(helper) || !File.Exists(bridge))
            {
                LastError = "CEF runtime incomplete. This directory must contain libcef.dll, " +
                            "WEBView2UnityCEF.dll and WEBView2UnityCEFSubprocess.exe. " +
                            "Build it using Native~/Build-Windows.ps1 (see Documentation~/NATIVE_SETUP.md).";
                return false;
            }

            try
            {
                if (bridgeHandle == IntPtr.Zero)
                {
                    // LoadLibraryEx with DLL_LOAD_DIR resolves libcef.dll and other CEF
                    // dependencies beside the bridge without modifying global PATH.
                    bridgeHandle = LoadLibraryEx(
                        bridge, IntPtr.Zero,
                        LoadLibrarySearchDllLoadDir | LoadLibrarySearchDefaultDirs);
                    if (bridgeHandle == IntPtr.Zero)
                    {
                        int code = Marshal.GetLastWin32Error();
                        LastError = "Unable to load WEBView2UnityCEF.dll from runtime folder. " +
                                    "Windows error " + code + ": " +
                                    new Win32Exception(code).Message +
                                    ". Ensure CEF version matches the bridge build, and use Windows x64.";
                        return false;
                    }
                }

                Directory.CreateDirectory(cacheDirectory);
                int ok = WebViewNative.W2U_Initialize(
                    root, helper, Path.GetFullPath(cacheDirectory));
                IsInitialized = ok == 1;
                if (!IsInitialized)
                    LastError = "Native CefInitialize failed. Check Unity Editor.log.";
                return IsInitialized;
            }
            catch (Exception ex) when (
                ex is DllNotFoundException ||
                ex is EntryPointNotFoundException ||
                ex is BadImageFormatException ||
                ex is TypeInitializationException ||
                ex is IOException ||
                ex is UnauthorizedAccessException)
            {
                LastError = "Native browser initialization failed: " + ex.Message;
                return false;
            }
#else
            LastError = "The native CEF prototype supports Windows x64 only.";
            return false;
#endif
        }

        public static void Tick()
        {
            if (IsInitialized) WebViewNative.W2U_Tick();
        }

        /// <summary>Only call at process exit, not during an Editor domain reload.</summary>
        public static void Shutdown()
        {
            if (!IsInitialized) return;
            WebViewNative.W2U_Shutdown();
            IsInitialized = false;
            // Do not FreeLibrary: the CLR may retain P/Invoke stubs for this module.
        }
    }
}
