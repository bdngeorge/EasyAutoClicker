﻿using EasyAutoClicker.Core;
using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using static EasyAutoClicker.Services.Helpers.Win32ApiHelper;

namespace EasyAutoClicker.Services;

public interface IInputSimulationService
{
    /// <summary>
    /// Simulates a mouse click at the current or given mouse position.
    /// </summary>
    /// <param name="input">The input object.</param>
    Task SimulateMouseClickAsync(InputEvent input);

    /// <summary>
    /// Simulates a key press for the given key code.
    /// </summary>
    /// <param name="input">The input object.</param>
    void SimulateKeyPress(InputEvent input);
}

public sealed partial class InputSimulationService : IInputSimulationService
{
    private const uint MOUSEEVENTF_LEFTDOWN = 0x02;
    private const uint MOUSEEVENTF_LEFTUP = 0x04; 
    private const uint MOUSEEVENTF_RIGHTDOWN = 0x08;
    private const uint MOUSEEVENTF_RIGHTUP = 0x10;
    private const uint MOUSEEVENTF_MIDDLEDOWN = 0x20;
    private const uint MOUSEEVENTF_MIDDLEUP = 0x40;
    private const uint KEYEVENTF_KEYUP = 0x0002;
    private const uint KEYEVENTF_KEYDOWN = 0x0000;

    public async Task SimulateMouseClickAsync(InputEvent input)
    {
        var mouseButtonFlags = GetMouseButtonFlag(input.Action);

        if (input.X.HasValue && input.Y.HasValue) 
            SetCursorPos(input.X.Value, input.Y.Value);
        

        var newInput = new INPUT
        {
            type = (uint)InputTypes.Mouse,
            u = new InputUnion
            {
                mi = new MOUSEINPUT
                {
                    dx = 0,
                    dy = 0,
                    mouseData = 0,
                    dwFlags = mouseButtonFlags,
                    time = 0,
                    dwExtraInfo = IntPtr.Zero
                }
            }
        };

        await Task.Delay(5);
        SendInput(1, [newInput], Marshal.SizeOf<INPUT>());
    }

    public void SimulateKeyPress(InputEvent input)
    {
        if (input.Key == null) return;

        var keyInput = new INPUT
        {
            type = (uint)InputTypes.Keyboard,
            u = new InputUnion
            {
                ki = new KEYBDINPUT
                {
                    wVk = (ushort)input.Key.Value,
                    wScan = 0,
                    dwFlags = input.Action == InputActions.KeyUp ? KEYEVENTF_KEYUP : KEYEVENTF_KEYDOWN,
                    time = 0,
                    dwExtraInfo = IntPtr.Zero
                }
            }
        };

        SendInput(1, [keyInput], Marshal.SizeOf<INPUT>());
    }

    private static uint GetMouseButtonFlag(InputActions action)
    {
        return action switch
        {
            InputActions.ClickLeftDown => MOUSEEVENTF_LEFTDOWN,
            InputActions.ClickRightDown => MOUSEEVENTF_RIGHTDOWN,
            InputActions.ClickMiddleDown => MOUSEEVENTF_MIDDLEDOWN,
            InputActions.ClickLeftUp => MOUSEEVENTF_LEFTUP,
            InputActions.ClickRightUp => MOUSEEVENTF_RIGHTUP,
            InputActions.ClickMiddleUp => MOUSEEVENTF_MIDDLEUP,
            _ => throw new ArgumentOutOfRangeException(nameof(action), action, null)
        };
    }
}
