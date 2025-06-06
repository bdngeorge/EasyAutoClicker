using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using static EasyAutoClicker.Services.Helpers.Win32ApiHelper;

namespace EasyAutoClicker.Core.Helpers;

public sealed class WindowsHotkeyHelper
{
    private static nint _oldWndProc = nint.Zero;
    private static WndProcDelegate? _newWndProcDelegate;

    private const int GWLP_WNDPROC = -4;

    /// <summary>
    /// Installs a custom WndProc handler on the specified window handle.
    /// </summary>
    [SupportedOSPlatform("windows")]
    internal static void Initialize(nint hwnd, WndProcDelegate messageHandler)
    {
        _newWndProcDelegate = (hWnd, msg, wParam, lParam) =>
        {
            // Custom logic can go here
            return messageHandler(hWnd, msg, wParam, lParam);
        };

        var newWndProcPtr = Marshal.GetFunctionPointerForDelegate(_newWndProcDelegate!);
        _oldWndProc = SetWindowLongPtr(hwnd, GWLP_WNDPROC, newWndProcPtr);

        if (_oldWndProc == nint.Zero)
        {
            throw new InvalidOperationException("Failed to subclass the window. " +
                $"Error: {Marshal.GetLastWin32Error()}");
        }
    }

    /// <summary>
    /// Call the original window procedure.
    /// </summary>
    internal static nint CallOriginal(nint hWnd, uint msg, nint wParam, nint lParam)
    {
        return CallWindowProc(_oldWndProc, hWnd, msg, wParam, lParam);
    }
}
