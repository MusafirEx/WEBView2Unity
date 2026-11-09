using System;
using System.Runtime.InteropServices;

namespace MusafirEx.WEBView2Unity
{
    // Cross-platform managed contract; initial native implementation is Windows x64 only.
    internal static class WebViewNative
    {
        private const string Library = "WEBView2UnityCEF";

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
        internal static extern int W2U_Initialize(string cefRoot, string subprocessPath, string cachePath);
        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void W2U_Tick();
        [DllImport(Library, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal static extern int W2U_Create(string url, int width, int height);
        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void W2U_Destroy(int browserId);
        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void W2U_Resize(int browserId, int width, int height);
        // 1 = new frame, 0 = unchanged, -1 = error/insufficient buffer
        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int W2U_CopyFrame(int browserId, byte[] buffer, int capacity,
            out int width, out int height, ref uint lastVersion);
        [DllImport(Library, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal static extern void W2U_Navigate(int browserId, string url);
        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void W2U_Back(int browserId);
        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void W2U_Forward(int browserId);
        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void W2U_Reload(int browserId);
        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void W2U_Focus(int browserId, int focused);
        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void W2U_MouseMove(int browserId, int x, int y, int modifiers);
        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void W2U_MouseClick(int browserId, int x, int y,
            int button, int mouseUp, int clickCount, int modifiers);
        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void W2U_MouseWheel(int browserId, int x, int y,
            int deltaX, int deltaY, int modifiers);
        // type 0 = RAWKEYDOWN, type 1 = KEYUP, type 2 = CHAR
        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void W2U_Key(int browserId, int eventType,
            int windowsKeyCode, int modifiers, int character);
        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void W2U_Shutdown();
    }
}
