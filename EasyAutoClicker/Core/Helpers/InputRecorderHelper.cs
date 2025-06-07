using EasyAutoClicker.Core;
using EasyAutoClicker.Views.Pages;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using static EasyAutoClicker.Services.Helpers.Win32ApiHelper;

namespace EasyAutoClicker.Core.Helpers;

[StructLayout(LayoutKind.Sequential)]

internal static class InputRecorderHelper
{
    private static readonly HookProc _keyboardProc = KeyboardHookCallback;
    private static readonly HookProc _mouseProc = MouseHookCallback;
    private static nint _keyboardHook = nint.Zero;
    private static nint _mouseHook = nint.Zero;

    private static readonly int WH_KEYBOARD_LL = 13;
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_KEYUP = 0x0101;
    private const int WM_SYSKEYDOWN = 0x0104;
    private const int WM_SYSKEYUP = 0x0105;

    private static readonly int WH_MOUSE_LL = 14;
    private const int WM_LBUTTONDOWN = 0x0201;
    private const int WM_LBUTTONUP = 0x0202;
    private const int WM_RBUTTONDOWN = 0x0204;
    private const int WM_RBUTTONUP = 0x0205;
    private const int WM_MBUTTONDOWN = 0x0207;
    private const int WM_MBUTTONUP = 0x0208;
    private const int WM_MOUSEMOVE = 0x0200;
    private const int WM_MOUSEWHEEL = 0x020A;

    private const uint VK_F9 = 0x78;
    private const uint VK_F10 = 0x79;

    private const int _minMoveDistance = 5;
    private const int _minMoveTimestampDifference = 16;
    private static int _lastMoveTimestamp = 0;
    private static int _recordingStart = 0;

    private static Page _originPage = new();
    private static List<InputEvent> _inputEvents = [];

    internal static void StartAllHook(Page page, int recordingStart)
    {
        _originPage = page;
        _recordingStart = recordingStart;
        _inputEvents = [];

        using var curProcess = Process.GetCurrentProcess();
        using var curModule = curProcess.MainModule;

        var hInstance = GetModuleHandleFromName();
        _keyboardHook = SetWindowsHookEx(WH_KEYBOARD_LL, _keyboardProc, hInstance, 0);
        _mouseHook = SetWindowsHookEx(WH_MOUSE_LL, _mouseProc, hInstance, 0);
    }

    internal async static Task StopAllHook()
    {
        if (_keyboardHook != nint.Zero)
            UnhookWindowsHookEx(_keyboardHook);
        if (_mouseHook != nint.Zero)
            UnhookWindowsHookEx(_mouseHook);

        if (_originPage is RecordAndPlaybackPage recordPage)
        {
            await recordPage.SaveInputEvents(_inputEvents);
        }
    }

    internal static void StartMouseHook(Page page)
    {
        _originPage = page;

        _mouseHook = SetWindowsHookEx(WH_MOUSE_LL, _mouseProc, GetModuleHandleFromName(), 0);
    }

    internal static void StopMouseHook()
    {
        if (_mouseHook != nint.Zero)
        {
            UnhookWindowsHookEx(_mouseHook);
            _mouseHook = nint.Zero;
        }
    }

    private static nint GetModuleHandleFromName()
    {
        using var curProcess = Process.GetCurrentProcess();
        using var curModule = curProcess.MainModule;

        return GetModuleHandle(curModule?.ModuleName ?? "");
    }

    private static nint KeyboardHookCallback(int nCode, nint wParam, nint lParam)
    {
        if (nCode >= 0)
        {
            var kbData = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);

            if (kbData.vkCode == VK_F9 || kbData.vkCode == VK_F10)
                return CallNextHookEx(_keyboardHook, nCode, wParam, lParam);

            InputActions action;
            if (wParam == WM_KEYDOWN || wParam == WM_SYSKEYDOWN)
                action = InputActions.KeyDown;
            else if (wParam == WM_KEYUP || wParam == WM_SYSKEYUP)
                action = InputActions.KeyUp;
            else
                return CallNextHookEx(_keyboardHook, nCode, wParam, lParam);

            _inputEvents.Add(new InputEvent
            {
                Type = InputTypes.Keyboard,
                Action = action,
                Key = kbData.vkCode,
                Timestamp = Environment.TickCount - _recordingStart
            });
        }
        return CallNextHookEx(_keyboardHook, nCode, wParam, lParam);
    }

    private static nint MouseHookCallback(int nCode, nint wParam, nint lParam)
    {
        if (nCode >= 0)
        {
            var mouseData = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);
            var pos = new POINT {
                X = mouseData.pt.X,
                Y = mouseData.pt.Y
            };

            int message = wParam.ToInt32();

            if (wParam == WM_LBUTTONDOWN && _originPage is MainPage mainPage)
            {
                mainPage.EndMousePosLoop(pos);
            }
            else if (_originPage is RecordAndPlaybackPage)
            {
                var action = new InputEvent
                {
                    Type = InputTypes.Mouse,
                    X = pos.X,
                    Y = pos.Y,
                    Delta = (short)(mouseData.mouseData >> 16 & 0xffff),
                    Timestamp = Environment.TickCount - _recordingStart,
                    Action = message switch
                    {
                        WM_LBUTTONDOWN => InputActions.ClickLeftDown,
                        WM_LBUTTONUP => InputActions.ClickLeftUp,
                        WM_MBUTTONDOWN => InputActions.ClickMiddleDown,
                        WM_MBUTTONUP => InputActions.ClickMiddleUp,
                        WM_RBUTTONDOWN => InputActions.ClickRightDown,
                        WM_RBUTTONUP => InputActions.ClickRightUp,
                        WM_MOUSEMOVE => InputActions.Move,
                        WM_MOUSEWHEEL => InputActions.Scroll,
                        _ => InputActions.None
                    }
                };

                if (action.Action == InputActions.None)
                    // If the action is not recognized, skip processing
                    return CallNextHookEx(_mouseHook, nCode, wParam, lParam);

                var lastEntry = _inputEvents.LastOrDefault(x => x.Action == InputActions.Move);
                if (action.Action == InputActions.Move && lastEntry != null && lastEntry.Action == InputActions.Move)
                {
                    bool tooSoon = action.Timestamp - _lastMoveTimestamp < _minMoveTimestampDifference;
                    bool tooClose = lastEntry.X != null && lastEntry.Y != null
                        && Math.Abs((int)action.X - (int)lastEntry.X) < _minMoveDistance
                        && Math.Abs((int)action.Y - (int)lastEntry.Y) < _minMoveDistance;

                    if (tooSoon || tooClose)
                    {
                        // Skip this move event
                        return CallNextHookEx(_mouseHook, nCode, wParam, lParam);
                    }
                    _lastMoveTimestamp = action.Timestamp;
                }

                _inputEvents.Add(action);
            }
        }
        return CallNextHookEx(_mouseHook, nCode, wParam, lParam);
    }
}
