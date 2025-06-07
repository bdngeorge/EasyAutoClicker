using EasyAutoClicker.Core;
using EasyAutoClicker.Core.Helpers;
using EasyAutoClicker.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Toolkit.Uwp.Notifications;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.Storage.Pickers;
using WinRT.Interop;
using static EasyAutoClicker.Services.Helpers.Win32ApiHelper;

namespace EasyAutoClicker.Views.Pages;

/// <summary>
/// The record and playback page.
/// </summary>
public sealed partial class RecordAndPlaybackPage : Page
{
    private readonly IWindowSizeService _windowSizeHelper;
    private readonly IInputSimulationService _inputHelper;

    private static readonly ToastContentBuilder _recordingFinishedNotif = new ToastContentBuilder()
        .AddHeader("EasyAutoClicker_0817", "EasyAutoClicker", "")
        .AddAudio(new Uri("ms-appx:///Assets/Sounds/Notification.wav"))
        .AddText("Your recording playback has finished!");

    private volatile bool _isRecording = false;
    private volatile bool _isPlaying = false;
    private string _logFilePath = "";

    public RecordAndPlaybackPage(IWindowSizeService windowSizeHelper, IInputSimulationService inputHelper)
    {
        _windowSizeHelper = windowSizeHelper;
        _inputHelper = inputHelper;

        InitializeComponent();

        var window = App.Services?.GetRequiredService<MainWindow>();
        _windowSizeHelper.ResizeWindow(window, 650, 330);
    }

    private void PlaybackSpeedDropdown_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem item)
        {
            PlaybackSpeedDropdown.Content = item.Text;
            PlaybackSpeedDropdown.Tag = item.Tag;
        }
    }

    private void PlaybackRepititionsDropdown_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem item)
        {
            PlaybackRepititionsDropdown.Content = item.Text;
            PlaybackRepititionsDropdown.Tag = item.Tag;
        }
    }

    internal nint OnWindowMessage(nint wParam)
    {
        // Handle your hotkey here (e.g., start/stop clicker)
        if (!_isPlaying && wParam.ToInt32() == MainWindow.HOTKEY_ID_1)
        {
            StartRecordingButton_Click(StartRecordingButton, new());
        }
        else if (!_isRecording && wParam.ToInt32() == MainWindow.HOTKEY_ID_2)
        {
            PlayRecordingButton_Click(PlayRecordingButton, new());
        }

        return nint.Zero;
    }

    internal async Task SaveInputEvents(List<InputEvent> inputEvents)
    {
        var hWnd = WindowNative.GetWindowHandle(App.Services?.GetRequiredService<MainWindow>());
        var filePicker = new FileSavePicker
        {
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
            SuggestedFileName = "ClickerInputEvents.json"
        };

        InitializeWithWindow.Initialize(filePicker, hWnd);

        filePicker.FileTypeChoices.Add("JSON File", [".json"]);
        var file = await filePicker.PickSaveFileAsync();

        if (file != null)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            string json = JsonSerializer.Serialize(inputEvents, options);

            Windows.Storage.CachedFileManager.DeferUpdates(file);
            await File.WriteAllTextAsync(file.Path, json);

            Windows.Storage.Provider.FileUpdateStatus status =
                await Windows.Storage.CachedFileManager.CompleteUpdatesAsync(file);

            if (status == Windows.Storage.Provider.FileUpdateStatus.Complete)
            {
                FileName.Text = file.Name;
                _logFilePath = file.Path;
                FileName.TextTrimming = TextTrimming.CharacterEllipsis;
                FileName.TextWrapping = TextWrapping.NoWrap;
            }
            else
            {
                FileName.Text = "File couldn't be saved.";
                FileName.TextTrimming = TextTrimming.None;
                FileName.TextWrapping = TextWrapping.Wrap;
            }
        }
        else
        {
            FileName.Text = "File couldn't be opened.";
            FileName.TextTrimming = TextTrimming.None;
            FileName.TextWrapping = TextWrapping.Wrap;
        }
    }

    private async void StartRecordingButton_Click(object sender, RoutedEventArgs e)
    {
        if (_isRecording)
        {
            _isRecording = !_isRecording;

            StartRecordingButton.Content = "Start Recording (F9)";
            StartRecordingButton.Style = (Style)Application.Current.Resources["CustomGreenButtonStyle"];
            StartRecordingButton.IsEnabled = false;

            await InputRecorderHelper.StopAllHook();

            StartRecordingButton.IsEnabled = true;
            PlayRecordingButton.IsEnabled = true;

            return;
        }

        StartRecordingButton.Content = "Stop Recording (F9)";
        StartRecordingButton.Style = (Style)Application.Current.Resources["CustomRedButtonStyle"];

        int recordingStart = Environment.TickCount;

        InputRecorderHelper.StartAllHook(this, recordingStart);

        _isRecording = !_isRecording;
        PlayRecordingButton.IsEnabled = false;
    }

    private void PlayRecordingButton_Click(object sender, RoutedEventArgs e)
    {
        if (_isPlaying)
        {
            _isPlaying = !_isPlaying;
            StartRecordingButton.IsEnabled = true;

            UpdateUIOnRecordingEnd();

            return;
        }

        if (string.IsNullOrWhiteSpace(_logFilePath)) return;

        PlayRecordingButton.Content = "End Recording (F10)";
        PlayRecordingButton.Style = (Style)Application.Current.Resources["CustomRedButtonStyle"];

        var json = File.ReadAllText(_logFilePath);
        var inputEvents = JsonSerializer.Deserialize<List<InputEvent>>(json);

        var playbackSpeed = double.Parse(PlaybackSpeedDropdown.Tag?.ToString() ?? "1.0");
        var playbackRepititions = int.Parse(PlaybackRepititionsDropdown.Tag?.ToString() ?? "1");

        if (inputEvents == null || inputEvents.Count == 0)
        {
            FileName.Text = "No input events found in the file.";
            FileName.TextTrimming = TextTrimming.None;
            FileName.TextWrapping = TextWrapping.Wrap;
            return;
        }
        else
        {
            _isPlaying = !_isPlaying;
            StartRecordingButton.IsEnabled = false;
            PlaybackRepitionsText.Text = $"Repitions Remaining: {playbackRepititions}";

            // Let this run on a background thread to avoid blocking the UI
            _ = PlayInputEventsAsync(inputEvents, playbackRepititions, playbackSpeed);
        }
    }

    private async void LoadFileButton_Click(object sender, RoutedEventArgs e)
    {
        var hWnd = WindowNative.GetWindowHandle(App.Services?.GetRequiredService<MainWindow>());
        var filePicker = new FileOpenPicker
        {
            ViewMode = PickerViewMode.List,
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary
        };

        InitializeWithWindow.Initialize(filePicker, hWnd);

        filePicker.FileTypeFilter.Add(".json");
        var file = await filePicker.PickSingleFileAsync();

        if (file != null)
        {
            FileName.Text = file.Name;
            _logFilePath = file.Path;
            FileName.TextTrimming = TextTrimming.CharacterEllipsis;
            FileName.TextWrapping = TextWrapping.NoWrap;
        }
        else
        {
            FileName.Text = "File couldn't be opened.";
            FileName.TextTrimming = TextTrimming.None;
            FileName.TextWrapping = TextWrapping.Wrap;
        }
    }

    private void ReturnButton_Click(object sender, RoutedEventArgs e)
    {
        if (_isPlaying || _isRecording) return;

        var mainPage = App.Services?.GetRequiredService<MainPage>();
        var window = App.Services?.GetRequiredService<MainWindow>();

        if (window != null)
            window.MainFrame.Content = mainPage;

        _windowSizeHelper.ResizeWindow(window, 500, 725);
    }

    private async Task PlayInputEventsAsync(List<InputEvent> inputEvents, int repititions, double speedMultiplier)
    {
        if (inputEvents == null || inputEvents.Count == 0)
        {
            UpdateUIOnRecordingEnd();
            return;
        }

        inputEvents.Sort((a, b) => a.Timestamp.CompareTo(b.Timestamp));

        var stopWatch = Stopwatch.StartNew();
        var rng = new Random(DateTime.Now.Millisecond);

        var maxOffset = 32;
        var randomDelayCheck = RandomDelayCheck.IsChecked ?? false;

        while (repititions > 0 && _isPlaying)
        {
            stopWatch.Start();
            int lastTimestamp = 0;

            for (int i = 0; i < inputEvents.Count; i++)
            {
                var input = inputEvents[i];

                int offset = rng.Next(0, maxOffset);
                int currentTimestamp = randomDelayCheck
                    ? (int)((input.Timestamp + offset) / speedMultiplier) 
                    : (int)((input.Timestamp) / speedMultiplier);

                if (randomDelayCheck)
                {
                    if (i == 0)
                        currentTimestamp = Math.Max(0, currentTimestamp);
                    else
                        currentTimestamp = Math.Max(lastTimestamp + maxOffset, currentTimestamp);
                }

                while (stopWatch.ElapsedMilliseconds < currentTimestamp)
                {
                    await Task.Yield();
                }

                lastTimestamp = currentTimestamp;

                if (!_isPlaying) return; // Check right before sending input
                switch (input.Type)
                {
                    case InputTypes.Mouse:
                        await SimulateMouse(input);
                        break;
                    case InputTypes.Keyboard:
                        if (KeyboardCheck.IsChecked ?? false)
                            _inputHelper.SimulateKeyPress(input);
                        break;
                }
            }

            stopWatch.Stop();
            stopWatch.Reset();

            repititions--;

            DispatcherQueue.TryEnqueue(() =>
            {
                PlaybackRepitionsText.Text = $"Repitions Remaining: {repititions}";
            });
        }

        UpdateUIOnRecordingEnd();
    }

    private async Task SimulateMouse(InputEvent input)
    {
        if (input.X == null || input.Y == null) return;

        if (input.Action == InputActions.Move)
        {
            SetCursorPos((int)input.X, (int)input.Y);
        }
        else
        {
            await _inputHelper.SimulateMouseClickAsync(input);
        }
    }

    private void UpdateUIOnRecordingEnd()
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            _isPlaying = false;
            PlayRecordingButton.Content = "Play Recording (F10)";
            PlayRecordingButton.Style = (Style)Application.Current.Resources["CustomBlueButtonStyle"];

            PlaybackRepitionsText.Text = $"Playback Repitions";

            StartRecordingButton.IsEnabled = true;

            _recordingFinishedNotif.Show();
        });
    }
}
