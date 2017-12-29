using System.Windows.Forms;
using PoeHUD.Hud.Settings;
using PoeHUD.Plugins;

namespace MapUpgrade
{
    internal class Settings : SettingsBase
    {
        public Settings()
        {
            Enable = true;
            HotKey = Keys.F6;
            Speed = new RangeNode<int>(20, 0, 100);
            Tier = new RangeNode<int>(5, 1, 15);
        }

        [Menu("Hotkey")]
        public HotkeyNode HotKey { get; set; }

        [Menu("Speed")]
        public RangeNode<int> Speed { get; set; }

        [Menu("Tier")]
        public RangeNode<int> Tier { get; set; }
    }
}