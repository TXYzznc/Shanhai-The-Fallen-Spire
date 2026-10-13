using Shanhai.Demo.Configuration;

namespace Shanhai.Demo.UI
{
    public interface IDemoRewardPresenter
    {
        void Open(int slot = 0);
        void OpenBuffs();
        bool OpenDrop(DemoDropConfig drop, bool isBoss);
        void Hide();
    }
}
