#include <windows.h>
#include <algorithm>
#include <cstdint>
#include <cstring>
#include <map>
#include <mutex>
#include <string>
#include <vector>
#include "include/cef_app.h"
#include "include/cef_browser.h"
#include "include/cef_client.h"
#include "include/cef_render_handler.h"
#include "include/cef_life_span_handler.h"

namespace
{
class BrowserClient final : public CefClient,
                            public CefRenderHandler,
                            public CefLifeSpanHandler
{
public:
    explicit BrowserClient(int w, int h) : width(w), height(h) {}
    CefRefPtr<CefRenderHandler> GetRenderHandler() override { return this; }
    CefRefPtr<CefLifeSpanHandler> GetLifeSpanHandler() override { return this; }

    bool GetViewRect(CefRefPtr<CefBrowser>, CefRect& rect) override
    {
        rect = CefRect(0, 0, width, height);
        return true;
    }

    void OnPaint(CefRefPtr<CefBrowser>, PaintElementType type,
                 const RectList&, const void* buffer, int w, int h) override
    {
        if (type != PET_VIEW || buffer == nullptr || w <= 0 || h <= 0) return;
        const size_t length = static_cast<size_t>(w) * static_cast<size_t>(h) * 4;
        std::lock_guard<std::mutex> lock(frameMutex);
        frame.resize(length);
        std::memcpy(frame.data(), buffer, length);
        paintedWidth = w;
        paintedHeight = h;
        ++frameVersion;
        if (frameVersion == 0) ++frameVersion;
    }

    void OnAfterCreated(CefRefPtr<CefBrowser> created) override { browser = created; }
    void OnBeforeClose(CefRefPtr<CefBrowser>) override
    {
        browser = nullptr;
        closed = true;
    }

    void Resize(int w, int h)
    {
        width = std::max(1, std::min(w, 4096));
        height = std::max(1, std::min(h, 4096));
        if (browser) browser->GetHost()->WasResized();
    }

    int CopyFrame(uint8_t* destination, int capacity, int* w, int* h, uint32_t* version)
    {
        if (!destination || !w || !h || !version) return -1;
        std::lock_guard<std::mutex> lock(frameMutex);
        if (frame.empty() || *version == frameVersion) return 0;
        if (capacity < 0 || static_cast<size_t>(capacity) < frame.size()) return -1;
        std::memcpy(destination, frame.data(), frame.size());
        *w = paintedWidth;
        *h = paintedHeight;
        *version = frameVersion;
        return 1;
    }

    CefRefPtr<CefBrowser> browser;
    bool closed = false;
    bool closing = false;

private:
    int width;
    int height;
    int paintedWidth = 0;
    int paintedHeight = 0;
    uint32_t frameVersion = 0;
    std::mutex frameMutex;
    std::vector<uint8_t> frame;

    IMPLEMENT_REFCOUNTING(BrowserClient);
    DISALLOW_COPY_AND_ASSIGN(BrowserClient);
};

bool initialized = false;
int nextId = 1;
std::map<int, CefRefPtr<BrowserClient>> clients;

CefRefPtr<BrowserClient> Find(int id)
{
    auto it = clients.find(id);
    return it == clients.end() || it->second->closed || it->second->closing
        ? nullptr : it->second;
}

void Close(CefRefPtr<BrowserClient> client)
{
    if (client && !client->closing && !client->closed)
    {
        client->closing = true;
        if (client->browser) client->browser->GetHost()->CloseBrowser(true);
    }
}

CefMouseEvent MakeMouseEvent(int x, int y, int modifiers)
{
    CefMouseEvent e;
    e.x = x;
    e.y = y;
    e.modifiers = static_cast<uint32_t>(modifiers);
    return e;
}
} // namespace

// C ABI: initialize, tick, resize and shutdown on the same Unity main thread.
// CEF's paint callback is guarded by a mutex when copying the BGRA buffer.
extern "C" {
__declspec(dllexport) int __cdecl W2U_Initialize(
    const wchar_t* root, const wchar_t* subprocess, const wchar_t* cache)
{
    if (initialized) return 1;
    if (!root || !subprocess || !cache) return 0;
    CefMainArgs args(GetModuleHandleW(nullptr));
    CefSettings settings;
    settings.no_sandbox = true; // Prototype only; review sandbox before production use.
    settings.windowless_rendering_enabled = true;
    settings.multi_threaded_message_loop = false;
    CefString(&settings.browser_subprocess_path) = subprocess;
    CefString(&settings.resources_dir_path) = root;
    CefString(&settings.locales_dir_path) = std::wstring(root) + L"\\locales";
    CefString(&settings.root_cache_path) = cache;
    CefString(&settings.cache_path) = cache;
    initialized = CefInitialize(args, settings, nullptr, nullptr);
    if (!initialized) OutputDebugStringW(L"[WEBView2Unity] CefInitialize failed\n");
    return initialized ? 1 : 0;
}

__declspec(dllexport) void __cdecl W2U_Tick()
{
    if (!initialized) return;
    CefDoMessageLoopWork();
    for (auto it = clients.begin(); it != clients.end();)
    {
        if (it->second->closed) it = clients.erase(it);
        else ++it;
    }
}

__declspec(dllexport) int __cdecl W2U_Create(const char* url, int w, int h)
{
    if (!initialized || !url) return -1;
    CefRefPtr<BrowserClient> client(new BrowserClient(std::max(1, std::min(w, 4096)),
                                    std::max(1, std::min(h, 4096))));
    CefWindowInfo window;
    window.SetAsWindowless(nullptr);
    CefBrowserSettings settings;
    CefRefPtr<CefBrowser> browser = CefBrowserHost::CreateBrowserSync(
        window, client, CefString(url), settings, nullptr, nullptr);
    if (!browser) return -1;
    client->browser = browser;
    const int id = nextId++;
    clients[id] = client;
    return id;
}

__declspec(dllexport) void __cdecl W2U_Destroy(int id)
{
    auto client = Find(id);
    Close(client);
}
__declspec(dllexport) void __cdecl W2U_Resize(int id, int w, int h)
{
    auto client = Find(id);
    if (client) client->Resize(w, h);
}
__declspec(dllexport) int __cdecl W2U_CopyFrame(int id, uint8_t* buffer,
    int capacity, int* w, int* h, uint32_t* version)
{
    auto client = Find(id);
    return client ? client->CopyFrame(buffer, capacity, w, h, version) : -1;
}
__declspec(dllexport) void __cdecl W2U_Navigate(int id, const char* url)
{
    auto client = Find(id);
    if (client && url && client->browser)
        client->browser->GetMainFrame()->LoadURL(url);
}
__declspec(dllexport) void __cdecl W2U_Back(int id)
{
    auto client = Find(id);
    if (client && client->browser && client->browser->CanGoBack())
        client->browser->GoBack();
}
__declspec(dllexport) void __cdecl W2U_Forward(int id)
{
    auto client = Find(id);
    if (client && client->browser && client->browser->CanGoForward())
        client->browser->GoForward();
}
__declspec(dllexport) void __cdecl W2U_Reload(int id)
{
    auto client = Find(id);
    if (client && client->browser) client->browser->Reload();
}
__declspec(dllexport) void __cdecl W2U_Focus(int id, int focused)
{
    auto client = Find(id);
    if (client && client->browser) client->browser->GetHost()->SetFocus(focused != 0);
}
__declspec(dllexport) void __cdecl W2U_MouseMove(int id, int x, int y, int flags)
{
    auto client = Find(id);
    if (client && client->browser)
        client->browser->GetHost()->SendMouseMoveEvent(MakeMouseEvent(x, y, flags), false);
}
__declspec(dllexport) void __cdecl W2U_MouseClick(int id, int x, int y,
    int button, int mouseUp, int clickCount, int flags)
{
    auto client = Find(id);
    if (client && client->browser)
        client->browser->GetHost()->SendMouseClickEvent(
            MakeMouseEvent(x, y, flags),
            button == 1 ? MBT_MIDDLE : (button == 2 ? MBT_RIGHT : MBT_LEFT),
            mouseUp != 0, std::max(1, clickCount));
}
__declspec(dllexport) void __cdecl W2U_MouseWheel(int id, int x, int y,
    int dx, int dy, int flags)
{
    auto client = Find(id);
    if (client && client->browser)
        client->browser->GetHost()->SendMouseWheelEvent(MakeMouseEvent(x, y, flags), dx, dy);
}
__declspec(dllexport) void __cdecl W2U_Key(int id, int type, int virtualKey, int flags, int ch)
{
    auto client = Find(id);
    if (!client || !client->browser) return;
    CefKeyEvent event;
    event.type = type == 1 ? KEYEVENT_KEYUP : type == 2 ? KEYEVENT_CHAR : KEYEVENT_RAWKEYDOWN;
    event.windows_key_code = virtualKey;
    event.native_key_code = virtualKey;
    event.modifiers = static_cast<uint32_t>(flags);
    event.character = static_cast<char16_t>(ch);
    event.unmodified_character = static_cast<char16_t>(ch);
    client->browser->GetHost()->SendKeyEvent(event);
}
__declspec(dllexport) void __cdecl W2U_Shutdown()
{
    if (!initialized) return;
    for (auto& entry : clients) Close(entry.second);
    for (int i = 0; i < 200 && !clients.empty(); ++i)
    {
        W2U_Tick();
        if (!clients.empty()) Sleep(5);
    }
    if (!clients.empty())
    {
        OutputDebugStringW(L"[WEBView2Unity] Browser shutdown incomplete; skipping CefShutdown.\n");
        return;
    }
    CefShutdown();
    initialized = false;
}
} // extern C
