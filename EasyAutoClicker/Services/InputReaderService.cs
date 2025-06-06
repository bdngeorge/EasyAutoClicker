using EasyAutoClicker.Core;
using EasyAutoClicker.Views.Pages;
using Microsoft.Extensions.DependencyInjection;

namespace EasyAutoClicker.Services;

public interface IInputReaderService
{
    /// <summary>
    /// Reads the input values for the auto clicker.
    /// </summary>
    /// <returns>The input values.</returns>
    InputValues ReadMainPageInputs();
}

public sealed class InputReaderService : IInputReaderService
{
    public InputValues ReadMainPageInputs()
    {
        var currentPage= App.Services?.GetRequiredService<MainPage>();
        if (currentPage == null)
            return new InputValues();

        // Determine interval type based on which radio is checked
        var intervalType = currentPage.SetTimeClick.IsChecked == true
            ? IntervalTypes.Set
            : IntervalTypes.Random;

        // Mouse button group
        InputActions mouseButton = InputActions.ClickLeftDown;
        if (currentPage.MiddleClick.IsChecked == true)
            mouseButton = InputActions.ClickMiddleDown;
        else if (currentPage.RightClick.IsChecked == true)
            mouseButton = InputActions.ClickRightDown;

        // Click type group
        ClickTypes clickType = currentPage.SingleClick.IsChecked == true
            ? ClickTypes.Single
            : ClickTypes.Double;

        // Loop type group
        LoopTypes loopType = currentPage.SetCountRepeat.IsChecked == true
            ? LoopTypes.Count
            : LoopTypes.Infinite;

        // Position type group
        PositionTypes positionType = currentPage.CurrentCurserPosition.IsChecked == true
            ? PositionTypes.Current
            : PositionTypes.Picked;

        // Parse values with fallback
        static int ParseInt(string? text, int fallback) =>
            int.TryParse(text, out var v) ? v : fallback;

        return new InputValues
        {
            ClickInterval = ParseInt(currentPage.SetIntervalBox.Text, 1000),
            ClickIntervalMin = ParseInt(currentPage.RandomIntervalStartBox.Text, 100),
            ClickIntervalMax = ParseInt(currentPage.RandomIntervalEndBox.Text, 5000),
            RepeatCount = ParseInt(currentPage.SetRepeatCountBox.Text, 1),
            CursorPosition = (
                ParseInt(currentPage.SetCursorXBox.Text, 0),
                ParseInt(currentPage.SetCursorYBox.Text, 0)
            ),
            MouseButton = mouseButton,
            ClickType = clickType,
            IntervalType = intervalType,
            LoopTypes = loopType,
            PositionType = positionType
        };
    }
}
