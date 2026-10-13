namespace Shanhai.Demo.Input
{
    /// <summary>
    /// Demo 输入 provider 的最小契约。设备读取只允许出现在该边界之后。
    /// </summary>
    public interface IDemoInputSource
    {
        DemoInputState ReadState();
    }
}
