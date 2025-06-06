namespace EasyAutoClicker.Core;

public sealed class InputEvent
{
    public InputTypes Type { get; set; }
    public InputActions Action { get; set; }
    public int? X { get; set; }
    public int? Y { get; set; }
    public int? Delta { get; set; }
    public uint? Key { get; set; }
    public int Timestamp { get; set; }
}
