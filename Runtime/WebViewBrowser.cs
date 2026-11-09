using System;
using UnityEngine;

namespace MusafirEx.WEBView2Unity
{
    /// <summary>Shared Editor/runtime browser wrapper. The Unity player UI is a later phase.</summary>
    public sealed class WebViewBrowser : IDisposable
    {
        private int id = -1;
        private int width;
        private int height;
        private byte[] frame;
        private byte[] flipped;
        private uint frameVersion;
        private Texture2D texture;

        public Texture2D Texture { get { return texture; } }
        public bool IsOpen { get { return id >= 0; } }

        public WebViewBrowser(string url, int initialWidth, int initialHeight)
        {
            if (!WebViewEngine.IsInitialized)
                throw new InvalidOperationException("CEF is not initialized.");
            width = Mathf.Clamp(initialWidth, 1, 4096);
            height = Mathf.Clamp(initialHeight, 1, 4096);
            Allocate();
            id = WebViewNative.W2U_Create(url, width, height);
            if (id < 0)
            {
                Dispose();
                throw new InvalidOperationException("Native browser creation failed.");
            }
        }

        private void ReleaseTexture()
        {
            if (texture == null) return;
            if (Application.isPlaying) UnityEngine.Object.Destroy(texture);
            else UnityEngine.Object.DestroyImmediate(texture);
            texture = null;
        }

        private void Allocate()
        {
            ReleaseTexture();
            frame = new byte[checked(width * height * 4)];
            flipped = new byte[frame.Length];
            texture = new Texture2D(width, height, TextureFormat.BGRA32, false, false);
            texture.filterMode = FilterMode.Bilinear;
            frameVersion = 0;
        }

        public void Resize(int newWidth, int newHeight)
        {
            newWidth = Mathf.Clamp(newWidth, 1, 4096);
            newHeight = Mathf.Clamp(newHeight, 1, 4096);
            if (!IsOpen || (newWidth == width && newHeight == height)) return;
            width = newWidth;
            height = newHeight;
            Allocate();
            WebViewNative.W2U_Resize(id, width, height);
        }

        public bool UpdateTexture()
        {
            if (!IsOpen) return false;
            int actualWidth, actualHeight;
            int result = WebViewNative.W2U_CopyFrame(id, frame, frame.Length,
                out actualWidth, out actualHeight, ref frameVersion);
            if (result != 1 || actualWidth != width || actualHeight != height) return false;
            int stride = width * 4;
            for (int y = 0; y < height; ++y)
                Buffer.BlockCopy(frame, y * stride, flipped, (height - y - 1) * stride, stride);
            texture.LoadRawTextureData(flipped);
            texture.Apply(false, false);
            return true;
        }

        public void Navigate(string url) { if (IsOpen) WebViewNative.W2U_Navigate(id, url); }
        public void Back() { if (IsOpen) WebViewNative.W2U_Back(id); }
        public void Forward() { if (IsOpen) WebViewNative.W2U_Forward(id); }
        public void Reload() { if (IsOpen) WebViewNative.W2U_Reload(id); }
        public void Focus(bool value) { if (IsOpen) WebViewNative.W2U_Focus(id, value ? 1 : 0); }
        public void MoveMouse(int x, int y, int modifiers) { if (IsOpen) WebViewNative.W2U_MouseMove(id, x, y, modifiers); }
        public void MouseClick(int x, int y, int button, bool up, int clicks, int modifiers)
        {
            if (IsOpen) WebViewNative.W2U_MouseClick(id, x, y, button, up ? 1 : 0, clicks, modifiers);
        }
        public void MouseWheel(int x, int y, int dx, int dy, int modifiers)
        {
            if (IsOpen) WebViewNative.W2U_MouseWheel(id, x, y, dx, dy, modifiers);
        }
        public void Key(int eventType, int keyCode, int modifiers, int character)
        {
            if (IsOpen) WebViewNative.W2U_Key(id, eventType, keyCode, modifiers, character);
        }
        public void Dispose()
        {
            if (IsOpen) WebViewNative.W2U_Destroy(id);
            id = -1;
            ReleaseTexture();
            frame = flipped = null;
        }
    }
}
