using EasyAutoClicker.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;

namespace EasyAutoClicker.Views.Pages;

/// <summary>
/// Settings page.
/// </summary>

// TODO: Implement when I think of settings
public sealed partial class SettingsPage : Page
{
    private readonly IWindowSizeService _windowSizeHelper;

    public SettingsPage(IWindowSizeService windowSizeHelper)
    {
        _windowSizeHelper = windowSizeHelper;

        InitializeComponent();

        var window = App.Services?.GetRequiredService<MainWindow>();
        _windowSizeHelper.ResizeWindow(window, 500, 305);
    }

    internal static nint OnWindowMessage(nint wParam)
    {
        // Handle your hotkey here (e.g., start/stop clicker)
        if (wParam.ToInt32() == MainWindow.HOTKEY_ID_1)
        {
            // Ctrl + 1 hotkey pressed
        }
        else if (wParam.ToInt32() == MainWindow.HOTKEY_ID_2)
        {
            // Ctrl + 3 hotkey pressed
        }

        return nint.Zero;
    }
}
