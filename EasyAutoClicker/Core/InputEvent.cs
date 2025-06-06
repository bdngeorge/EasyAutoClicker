namespace EasyAutoClicker.Core;

public sealed class InputEvent
{
    public InputEvent() { }

    public InputEvent(InputEvent input)
    {
        Type = input.Type;
        Action = input.Action;
        X = input.X;
        Y = input.Y;
        Delta = input.Delta;
        Key = input.Key;
        Timestamp = input.Timestamp;
    }

    public static InputEvent GetFlippedMouseInput(InputEvent input)
    {
        var flippedInput = new InputEvent(input);

        flippedInput.Action = input.Action switch
        {
            InputActions.ClickLeftDown => InputActions.ClickLeftUp,
            InputActions.ClickLeftUp => InputActions.ClickLeftDown,
            InputActions.ClickRightDown => InputActions.ClickRightUp,
            InputActions.ClickRightUp => InputActions.ClickRightDown,
            InputActions.ClickMiddleDown => InputActions.ClickMiddleUp,
            InputActions.ClickMiddleUp => InputActions.ClickMiddleDown,
            _ => input.Action
        };

        return flippedInput;
    }

    public InputTypes Type { get; set; }
    public InputActions Action { get; set; }
    public int? X { get; set; }
    public int? Y { get; set; }
    public int? Delta { get; set; }
    public uint? Key { get; set; }
    public int Timestamp { get; set; }
}
