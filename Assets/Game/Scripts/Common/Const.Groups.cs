/// <summary>
/// Framework-level default groups. Projects may extend this partial class with
/// their own groups without modifying framework code.
/// </summary>
public static partial class Const
{
    public enum EntityGroup
    {
        Default,
        Effect,
        Persistent
    }

    public enum UIGroup
    {
        Default,
        Dialog,
        Overlay
    }

    public enum SoundGroup
    {
        Music,
        Sound
    }
}
