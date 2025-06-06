using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using WinRT.Interop;

namespace EasyAutoClicker.Services;

public interface IWindowSizeService
{
    /// <summary>
    /// Resizes the given window to the given width and height.
    /// </summary>
    /// <param name="window">The window.</param>
    /// <param name="width">The desired width.</param>
    /// <param name="height">The desired height./param>
    void ResizeWindow(Window? window, int width, int height);
}

public sealed class WindowSizeService : IWindowSizeService
{
    public void ResizeWindow(Window? window, int width, int height)
    {
        if (window == null) return;

        nint hwnd = WindowNative.GetWindowHandle(window);
        WindowId windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
        AppWindow appWindow = AppWindow.GetFromWindowId(windowId);

        appWindow.Resize(new Windows.Graphics.SizeInt32 { Width = width, Height = height });
        appWindow.SetPresenter(AppWindowPresenterKind.Overlapped);
        if (appWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.IsResizable = false;
            presenter.IsMaximizable = false;
        }
    }
}
