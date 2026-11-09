cmake_minimum_required(VERSION 3.21)
project(WEBView2UnityCEF LANGUAGES CXX)

if(NOT WIN32 OR NOT CMAKE_SIZEOF_VOID_P EQUAL 8)
    message(FATAL_ERROR "v0.1 native bridge targets Windows x64 only")
endif()
if(NOT DEFINED CEF_ROOT OR NOT EXISTS "${CEF_ROOT}/include/cef_app.h")
    message(FATAL_ERROR "Pass -DCEF_ROOT=C:/path/to/cef_binary_..._windows64")
endif()

set(CMAKE_CXX_STANDARD 20)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
set(CEF_ROOT "${CEF_ROOT}" CACHE PATH "Path to CEF binary distribution")

list(APPEND CMAKE_MODULE_PATH "${CEF_ROOT}/cmake")
find_package(CEF REQUIRED)
add_subdirectory(${CEF_LIBCEF_DLL_WRAPPER_PATH} libcef_dll_wrapper)

add_library(WEBView2UnityCEF SHARED WebViewBridge.cpp)
target_include_directories(WEBView2UnityCEF PRIVATE "${CEF_ROOT}")
target_compile_definitions(WEBView2UnityCEF PRIVATE UNICODE _UNICODE NOMINMAX WIN32_LEAN_AND_MEAN)
target_link_libraries(WEBView2UnityCEF PRIVATE libcef_dll_wrapper "${CEF_ROOT}/Release/libcef.lib" comctl32 shlwapi rpcrt4)

add_executable(WEBView2UnityCEFSubprocess WIN32 CefSubprocess.cpp)
target_include_directories(WEBView2UnityCEFSubprocess PRIVATE "${CEF_ROOT}")
target_compile_definitions(WEBView2UnityCEFSubprocess PRIVATE UNICODE _UNICODE NOMINMAX WIN32_LEAN_AND_MEAN)
target_link_libraries(WEBView2UnityCEFSubprocess PRIVATE libcef_dll_wrapper "${CEF_ROOT}/Release/libcef.lib" comctl32 shlwapi rpcrt4)
