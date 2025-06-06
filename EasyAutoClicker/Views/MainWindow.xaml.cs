using EasyAutoClicker.Services;
using EasyAutoClicker.Views.Pages;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using WinRT.Interop;
using static EasyAutoClicker.Services.Helpers.Win32ApiHelper;
using static EasyAutoClicker.Core.Helpers.WindowsHotkeyHelper;

namespace EasyAutoClicker;

public sealed partial class MainWindow : Window
{
    private readonly IWindowSizeService _windowSizeHelper;

    internal const int HOTKEY_ID_1 = 9000;
    internal const int HOTKEY_ID_2 = 9001;
    private const uint VK_F9 = 0x78; // 'F9' key
    private const uint VK_F10 = 0x79; // 'F10' key

    private static readonly IntPtr HWND_TOPMOST = new(-1);
    private const uint SWP_NOMOVE = 0x0002;
    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_NOACTIVATE = 0x0010;

    private IntPtr _hwnd; 

    public MainWindow(IWindowSizeService windowSizeHelper)
    {
        _windowSizeHelper = windowSizeHelper;

        InitializeComponent();

        _windowSizeHelper.ResizeWindow(this, 500, 725);

        AppWindow.SetIcon("Assets/TitleBarLogo.scale-32.ico");

        var titleBar = AppWindow.TitleBar;
        titleBar.BackgroundColor = Colors.Black;
        titleBar.ForegroundColor = Colors.White;
        titleBar.ButtonBackgroundColor = Colors.Black;
        titleBar.ButtonForegroundColor = Colors.White;

        var mainPage = App.Services?.GetRequiredService<MainPage>();
        MainFrame.Content = mainPage;
    }

    internal void MakeWindowTopmost()
    {
        _hwnd = WindowNative.GetWindowHandle(this);
        SetWindowPos(_hwnd, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
    }

    internal void RegisterHotKeys()
    {
        _hwnd = WindowNative.GetWindowHandle(this);
        if (_hwnd == IntPtr.Zero)
            throw new InvalidOperationException("Window handle is not available.");

        if (!RegisterHotKey(_hwnd, HOTKEY_ID_1, 0, VK_F9))
            throw new InvalidOperationException("Failed to register hotkey F9.");
        if (!RegisterHotKey(_hwnd, HOTKEY_ID_2, 0, VK_F10))
            throw new InvalidOperationException("Failed to register hotkey F10.");

        Initialize(_hwnd, OnWindowMessage);
    }

    internal nint OnWindowMessage(nint hwnd, uint msg, nint wParam, nint lParam)
    {
        const int WM_HOTKEY = 0x0312;
        if (msg == WM_HOTKEY)
        {
            var currentPage = MainFrame.Content as Page;

            if (currentPage is MainPage mainPage)
            {
                mainPage.OnWindowMessage(wParam);
            }
            else if (currentPage is RecordAndPlaybackPage recordPage)
            {
                recordPage.OnWindowMessage(wParam);
            }
            else if (currentPage is SettingsPage settingsPage)
            {
                SettingsPage.OnWindowMessage(wParam);
            }

            return nint.Zero; // Return 0 to indicate the message was handled
        }

        return CallOriginal(hwnd, msg, wParam, lParam);
    }

    ~MainWindow()
    {
        UnregisterHotKey(_hwnd, HOTKEY_ID_1);
        UnregisterHotKey(_hwnd, HOTKEY_ID_2);
    }
}
