namespace EasyAutoClicker.Core;

public enum InputTypes
{
    Mouse,
    Keyboard
}

public enum InputActions
{
    None, // used to null check
    ClickLeftDown,
    ClickLeftUp,
    ClickRightDown,
    ClickRightUp,
    ClickMiddleDown,
    ClickMiddleUp,
    Move,
    Scroll, 
    KeyDown,
    KeyUp
}

public enum ClickTypes
{
    Single,
    Double
}

public enum IntervalTypes
{
    Set,
    Random
}

public enum LoopTypes
{
    Count,
    Infinite
}

public enum PositionTypes
{
    Current,
    Picked
}
