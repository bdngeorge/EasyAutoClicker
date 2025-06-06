namespace EasyAutoClicker.Core;

public sealed class InputValues
{
    public int ClickInterval { get; set; } = 1000; // Default to 1000 milliseconds (1 second)
    public int ClickIntervalMin { get; set; } = 100; // Minimum interval in milliseconds
    public int ClickIntervalMax { get; set; } = 5000; // Maximum interval in milliseconds
    public int RepeatCount { get; set; } = 1; // Default to 1 click
    public (int, int) CursorPosition { get; set; } = (0, 0); // Default to current cursor position (0, 0)

    public InputActions MouseButton { get; set; } = InputActions.ClickLeftDown; // Default to left click
    public ClickTypes ClickType { get; set; } = ClickTypes.Single; // Default to single click
    public IntervalTypes IntervalType { get; set; } = IntervalTypes.Set; // Default to repeat
    public LoopTypes LoopTypes { get; set; } = LoopTypes.Count; // Default to count
    public PositionTypes PositionType { get; set; } = PositionTypes.Current; // Default to current position
}
