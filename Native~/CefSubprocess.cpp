#include <windows.h>
#include "include/cef_app.h"

// A dedicated subprocess prevents Unity Editor itself relaunching as a CEF renderer.
int APIENTRY wWinMain(HINSTANCE instance, HINSTANCE, LPWSTR, int)
{
    CefMainArgs mainArgs(instance);
    return CefExecuteProcess(mainArgs, nullptr, nullptr);
}
