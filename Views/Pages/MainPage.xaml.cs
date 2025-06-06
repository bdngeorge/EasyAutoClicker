using EasyAutoClicker.Core;
using EasyAutoClicker.Core.Helpers;
using EasyAutoClicker.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading;
using static EasyAutoClicker.Services.Helpers.Win32ApiHelper;

namespace EasyAutoClicker.Views.Pages;

/// <summary>
/// The main page of the application.
/// </summary>
public sealed partial class MainPage : Page
{
    private readonly IInputReaderService _inputReaderService;
    private readonly IInputSimulationService _clickHelper;
    private readonly IWindowSizeService _windowSizeHelper;

    private readonly DispatcherTimer _clickTimer = new();
    private readonly DispatcherTimer _cursorTimer = new();

    private const int DELAY = 100; // Delay in milliseconds for mouse events
    private int _count = 0;
    private InputValues _inputValues = new();
    private bool _isClicking = false;

    public MainPage(
        IInputReaderService inputReaderService, 
        IInputSimulationService clickHelper,
        IWindowSizeService windowSizeHelper)
    {
        _inputReaderService = inputReaderService;
        _clickHelper = clickHelper;
        _windowSizeHelper = windowSizeHelper;

        InitializeComponent();

        _clickTimer.Tick += ClickLoop;
    }

    internal nint OnWindowMessage(nint wParam)
    {
        // Handle your hotkey here (e.g., start/stop clicker)
        if (wParam.ToInt32() == MainWindow.HOTKEY_ID_1)
        {
            if (_isClicking)
            {
                StopButton_Click(StopButton, new());
            }
            else
            {
                StartButton_Click(StartButton, new());
            }

            // Toggle recording state
            _isClicking = !_isClicking;
        }

        return nint.Zero;
    }

    private void StartButton_Click(object sender, RoutedEventArgs e)
    {
        StartButton.IsEnabled = false;
        StopButton.IsEnabled = true;

        _inputValues = _inputReaderService.ReadMainPageInputs();

        _count = 0;

        // Setup timer
        _clickTimer.Interval = _inputValues.IntervalType == IntervalTypes.Set
            ? TimeSpan.FromMilliseconds(_inputValues.ClickInterval)
            : TimeSpan.FromMilliseconds(
                new Random((int)DateTime.Now.TimeOfDay.TotalMilliseconds).Next(_inputValues.ClickIntervalMin, _inputValues.ClickIntervalMax)
              );

        _clickTimer.Start();

        // Immediately perform the first click
        ClickLoop(null, null);
    }

    private void StopButton_Click(object sender, RoutedEventArgs e)
    {
        StartButton.IsEnabled = true;
        StopButton.IsEnabled = false;

        _clickTimer?.Stop();
    }

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        var settingsPage = App.Services?.GetRequiredService<SettingsPage>();
        var window = App.Services?.GetRequiredService<MainWindow>();

        if (window != null)
            window.MainFrame.Content = settingsPage;
    }

    private void RecordButton_Click(object sender, RoutedEventArgs e)
    {
        var recordAndPlaybackPage = App.Services?.GetRequiredService<RecordAndPlaybackPage>();
        var window = App.Services?.GetRequiredService<MainWindow>();

        _windowSizeHelper.ResizeWindow(window, 650, 330);

        if (window != null)
            window.MainFrame.Content = recordAndPlaybackPage;
    }

    private void SetCursorPosition_Click(object sender, RoutedEventArgs e)
    {
        _cursorTimer.Interval = TimeSpan.FromMilliseconds(50);
        _cursorTimer.Tick += GetMousePosLoop;
        _cursorTimer.Start();

        InputRecorderHelper.StartMouseHook(this);
    }

    private void GetMousePosLoop(object? sender, object? e)
    {
        if (GetCursorPos(out POINT point))
        {
            SetCursorXBox.Text = $"{point.X}";
            SetCursorYBox.Text = $"{point.Y}";
        }
    }

    internal void EndMousePosLoop(POINT point)
    {
        _cursorTimer.Stop();
        _cursorTimer.Tick -= GetMousePosLoop;

        SetCursorXBox.Text = $"{point.X}";
        SetCursorYBox.Text = $"{point.Y}";

        InputRecorderHelper.StopMouseHook();
    }

    private void ClickLoop(object? sender, object? e)
    {
        if (_inputValues.LoopTypes == LoopTypes.Count) _count++;
        if (_inputValues.IntervalType == IntervalTypes.Random)
        {
            _clickTimer.Interval = TimeSpan.FromMilliseconds(
                new Random((int)DateTime.Now.TimeOfDay.TotalMilliseconds).Next(_inputValues.ClickIntervalMin, _inputValues.ClickIntervalMax)
            );
        }

        var input = new InputEvent
        {
            Action = _inputValues.MouseButton,
            X = _inputValues.PositionType == PositionTypes.Picked 
                ? _inputValues.CursorPosition.Item1 : null,
            Y = _inputValues.PositionType == PositionTypes.Picked 
                ? _inputValues.CursorPosition.Item2 : null,
        };

        // TODO: Handle right and middle clicks
        SendSingleClick(input);

        if (_inputValues.ClickType == ClickTypes.Double)
        {
            Thread.Sleep(DELAY); // Small delay for double click
            SendSingleClick(input);
        }

        if (_inputValues.LoopTypes == LoopTypes.Count && _count >= _inputValues.RepeatCount)
        {
            StartButton.IsEnabled = true;
            StopButton.IsEnabled = false;

            _clickTimer?.Stop();
            return;
        }
    }

    private void SendSingleClick(InputEvent input)
    {
        _clickHelper.SimulateMouseClick(input);
        input.Action = InputActions.ClickLeftUp;
        _clickHelper.SimulateMouseClick(input);
    }
}
