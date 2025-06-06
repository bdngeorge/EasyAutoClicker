using EasyAutoClicker.Services;
using EasyAutoClicker.Views.Pages;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace EasyAutoClicker;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App : Application
{
    public static IServiceProvider? Services { get; private set; }

    internal MainWindow? m_window;

    /// <summary>
    /// Initializes the singleton application object.  This is the first line of authored code
    /// executed, and as such is the logical equivalent of main() or WinMain().
    /// </summary>
    public App()
    {
        InitializeComponent();

        var services = new ServiceCollection();

        ConfigureServices(services);

        Services = services.BuildServiceProvider();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        // Register your services here
        services.AddTransient<IInputReaderService, InputReaderService>();
        services.AddTransient<IInputSimulationService, InputSimulationService>();
        services.AddTransient<IWindowSizeService, WindowSizeService>();

        services.AddSingleton<MainWindow>();
        services.AddSingleton<MainPage>();
        services.AddSingleton<RecordAndPlaybackPage>();
        services.AddSingleton<SettingsPage>();
    }

    /// <summary>
    /// Invoked when the application is launched.
    /// </summary>
    /// <param name="args">Details about the launch request and process.</param>
    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        m_window = Services?.GetRequiredService<MainWindow>();
        m_window?.Activate();
        m_window?.RegisterHotKeys();
        m_window?.MakeWindowTopmost();
    }
}

