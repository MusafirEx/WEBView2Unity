using System;
using System.IO;
using UnityEngine;

namespace MusafirEx.WEBView2Unity
{
    /// <summary>Process-wide CEF backend; native engine is optional and never downloaded silently.</summary>
    public static class WebViewEngine
    {
        public static bool IsInitialized { get; private set; }
        public static string LastError { get; private set; }

        public static bool TryInitialize(string cefRoot, string cacheDirectory)
        {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            if (IsInitialized) return true;
            LastError = null;
            if (string.IsNullOrWhiteSpace(cefRoot))
            {
                LastError = "Set the CEF Runtime Folder in WEBView2Unity settings.";
                return false;
            }

            string root = Path.GetFullPath(cefRoot);
            string helper = Path.Combine(root, "WEBView2UnityCEFSubprocess.exe");
            if (!File.Exists(Path.Combine(root, "libcef.dll")) || !File.Exists(helper))
            {
                LastError = "CEF files missing: libcef.dll or WEBView2UnityCEFSubprocess.exe.";
                return false;
            }

            try
            {
                Directory.CreateDirectory(cacheDirectory);
                int ok = WebViewNative.W2U_Initialize(root, helper, Path.GetFullPath(cacheDirectory));
                IsInitialized = ok == 1;
                if (!IsInitialized) LastError = "Native CEF initialization failed. See Unity Editor log.";
                return IsInitialized;
            }
            catch (Exception ex) when (ex is DllNotFoundException || ex is EntryPointNotFoundException ||
                                        ex is BadImageFormatException || ex is TypeInitializationException)
            {
                LastError = "Missing/incompatible native plugin WEBView2UnityCEF: " + ex.Message;
                return false;
            }
#else
            LastError = "The initial native CEF backend supports Windows x64 only.";
            return false;
#endif
        }

        public static void Tick()
        {
            if (IsInitialized) WebViewNative.W2U_Tick();
        }

        /// <summary>Call on process shutdown, not on managed domain reload.</summary>
        public static void Shutdown()
        {
            if (!IsInitialized) return;
            WebViewNative.W2U_Shutdown();
            IsInitialized = false;
        }
    }
}
