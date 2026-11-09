using System;
using System.IO;
using MusafirEx.WEBView2Unity;
using UnityEditor;
using UnityEngine;

namespace MusafirEx.WEBView2Unity.Editor
{
    /// <summary>Dockable Unity Editor browser; native CEF is an optional manual install.</summary>
    public sealed class WebViewEditorWindow : EditorWindow
    {
        private const string RootPref = "WEBView2Unity.CEFRoot";
        private const string UrlPref = "WEBView2Unity.LastUrl";
        private const string Home = "https://example.com/";

        private string address = Home;
        private string cefRoot;
        private string message;
        private WebViewBrowser browser;

        [MenuItem("Window/WEBView2Unity/Browser")]
        public static void Open()
        {
            var window = GetWindow<WebViewEditorWindow>();
            window.titleContent = new GUIContent("WEBView2Unity");
            window.minSize = new Vector2(320, 240);
            window.Show();
        }

        private void OnEnable()
        {
            address = EditorPrefs.GetString(UrlPref, Home);
            cefRoot = EditorPrefs.GetString(RootPref, string.Empty);
            wantsMouseMove = true;
            EditorApplication.update += UpdateBrowser;
        }

        private void OnDisable()
        {
            EditorApplication.update -= UpdateBrowser;
            if (browser != null) browser.Dispose();
            browser = null;
        }

        private void OnLostFocus()
        {
            if (browser != null) browser.Focus(false);
        }

        private void UpdateBrowser()
        {
            if (browser == null) return;
            try
            {
                WebViewEngine.Tick();
                if (browser.UpdateTexture()) Repaint();
            }
            catch (Exception ex)
            {
                message = "CEF browser error: " + ex.Message;
                Debug.LogException(ex);
                browser.Dispose();
                browser = null;
                Repaint();
            }
        }

        private void OnGUI()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                using (new EditorGUI.DisabledScope(browser == null))
                {
                    if (GUILayout.Button("<", EditorStyles.toolbarButton, GUILayout.Width(28))) browser.Back();
                    if (GUILayout.Button(">", EditorStyles.toolbarButton, GUILayout.Width(28))) browser.Forward();
                    if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(55))) browser.Reload();
                }
                GUI.SetNextControlName("WEBView2UnityAddress");
                address = GUILayout.TextField(address, GUILayout.MinWidth(75));
                if (GUILayout.Button("Go", EditorStyles.toolbarButton, GUILayout.Width(33))) Navigate();
                if (GUILayout.Button("External", EditorStyles.toolbarButton, GUILayout.Width(62))) OpenExternal();
            }

            if (browser != null)
            {
                Rect view = GUILayoutUtility.GetRect(1, 100000, 1, 100000,
                    GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
                if (Event.current.type == EventType.Repaint)
                {
                    browser.Resize(Mathf.RoundToInt(view.width), Mathf.RoundToInt(view.height));
                    if (browser.Texture != null)
                        GUI.DrawTexture(view, browser.Texture, ScaleMode.StretchToFill, false);
                }
                HandleInput(view, Event.current);
                return;
            }

            EditorGUILayout.Space(12);
            EditorGUILayout.HelpBox(
                "Experimental CEF backend: source is included but native Chromium/CEF " +
                "binaries are not shipped with this UPM package. The Editor window " +
                "will not render web pages until a compatible native build is installed.",
                MessageType.Info);
            EditorGUILayout.LabelField("CEF Runtime Folder (Windows x64)", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                cefRoot = EditorGUILayout.TextField(cefRoot);
                if (GUILayout.Button("...", GUILayout.Width(30)))
                {
                    string selected = EditorUtility.OpenFolderPanel("Select CEF runtime", cefRoot, "");
                    if (!string.IsNullOrEmpty(selected)) cefRoot = selected;
                }
            }
            if (GUILayout.Button("Initialize CEF and Open Browser")) StartBrowser();
            if (GUILayout.Button("Open browser externally (fallback)")) OpenExternal();
            if (!string.IsNullOrEmpty(message)) EditorGUILayout.HelpBox(message, MessageType.Warning);
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Build instructions: Documentation~/NATIVE_SETUP.md", EditorStyles.miniLabel);
        }

        private void StartBrowser()
        {
            EditorPrefs.SetString(RootPref, cefRoot ?? string.Empty);
            var cachePath = Path.Combine(Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData), "WEBView2Unity", "Cache");
            if (!WebViewEngine.TryInitialize(cefRoot, cachePath))
            {
                message = WebViewEngine.LastError;
                return;
            }
            string url, error;
            if (!WebViewUrl.TryNormalize(address, out url, out error))
            {
                message = error;
                return;
            }
            try
            {
                browser = new WebViewBrowser(url, 900, 600);
                address = url;
                EditorPrefs.SetString(UrlPref, url);
                message = null;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                Debug.LogException(ex);
            }
        }

        private void Navigate()
        {
            string url, error;
            if (!WebViewUrl.TryNormalize(address, out url, out error))
            {
                message = error;
                return;
            }
            address = url;
            EditorPrefs.SetString(UrlPref, url);
            if (browser != null) browser.Navigate(url);
            else message = "Initialize CEF first, or use External to open in your default browser.";
        }

        private void OpenExternal()
        {
            string url, error;
            if (!WebViewUrl.TryNormalize(address, out url, out error))
            {
                message = error;
                return;
            }
            Application.OpenURL(url);
        }

        private void HandleInput(Rect view, Event e)
        {
            if (e.type == EventType.MouseDown && view.Contains(e.mousePosition))
            {
                GUI.FocusControl(null);
                browser.Focus(true);
            }
            bool inside = view.Contains(e.mousePosition);
            int x = Mathf.Clamp((int)(e.mousePosition.x - view.x), 0, Mathf.Max(0, (int)view.width - 1));
            int y = Mathf.Clamp((int)(e.mousePosition.y - view.y), 0, Mathf.Max(0, (int)view.height - 1));
            int flags = GetModifiers(e.modifiers);
            if (inside && e.type == EventType.MouseMove)
            {
                browser.MoveMouse(x, y, flags);
                e.Use();
            }
            else if (inside && (e.type == EventType.MouseDown || e.type == EventType.MouseUp) && e.button <= 2)
            {
                browser.MouseClick(x, y, e.button == 2 ? 1 : e.button == 1 ? 2 : 0,
                    e.type == EventType.MouseUp, Mathf.Max(1, e.clickCount), flags);
                e.Use();
            }
            else if (inside && e.type == EventType.ScrollWheel)
            {
                browser.MouseWheel(x, y, Mathf.RoundToInt(-e.delta.x * 40),
                    Mathf.RoundToInt(-e.delta.y * 40), flags);
                e.Use();
            }
            else if (hasFocus && GUI.GetNameOfFocusedControl() != "WEBView2UnityAddress" &&
                     (e.type == EventType.KeyDown || e.type == EventType.KeyUp))
            {
                int keyCode = WindowsVirtualKey(e.keyCode);
                if (keyCode == 0 && e.character == '\0') return;
                browser.Key(e.type == EventType.KeyDown ? 0 : 1, keyCode, flags, 0);
                if (e.type == EventType.KeyDown && !char.IsControl(e.character))
                    browser.Key(2, e.character, flags, e.character);
                e.Use();
            }
        }

        private static int GetModifiers(EventModifiers modifiers)
        {
            int result = 0;
            if ((modifiers & EventModifiers.Shift) != 0) result |= 1 << 1;
            if ((modifiers & EventModifiers.Control) != 0) result |= 1 << 2;
            if ((modifiers & EventModifiers.Alt) != 0) result |= 1 << 3;
            return result;
        }

        private static int WindowsVirtualKey(KeyCode key)
        {
            if (key >= KeyCode.A && key <= KeyCode.Z) return 0x41 + ((int)key - (int)KeyCode.A);
            if (key >= KeyCode.Alpha0 && key <= KeyCode.Alpha9) return 0x30 + ((int)key - (int)KeyCode.Alpha0);
            if (key >= KeyCode.Keypad0 && key <= KeyCode.Keypad9) return 0x60 + ((int)key - (int)KeyCode.Keypad0);
            switch (key)
            {
                case KeyCode.Backspace: return 0x08;
                case KeyCode.Tab: return 0x09;
                case KeyCode.Return: case KeyCode.KeypadEnter: return 0x0D;
                case KeyCode.Escape: return 0x1B;
                case KeyCode.Space: return 0x20;
                case KeyCode.LeftArrow: return 0x25;
                case KeyCode.UpArrow: return 0x26;
                case KeyCode.RightArrow: return 0x27;
                case KeyCode.DownArrow: return 0x28;
                case KeyCode.Delete: return 0x2E;
                case KeyCode.Home: return 0x24;
                case KeyCode.End: return 0x23;
                case KeyCode.PageUp: return 0x21;
                case KeyCode.PageDown: return 0x22;
                default: return 0;
            }
        }
    }

    [InitializeOnLoad]
    internal static class WebViewEditorShutdown
    {
        static WebViewEditorShutdown()
        {
            EditorApplication.quitting += WebViewEngine.Shutdown;
        }
    }
}
