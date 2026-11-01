using AbsoluteFriends.Options;
using AbsoluteFriends.Utils;
using Menu.Remix.MixedUI;

namespace AbsoluteFriends.ChainedTogether;

internal static class Menu
{
    public static void Build(MenuBuilder menu)
    {
        menu.SetColumns(4);
        menu.AddIntSlider(Config.ChainLength, "Chain Length", span: 4f);
        menu.AddFloatSlider(Config.ChainElasticity, "Chain Elasticity", span: 4f);
        menu.AddComboBox(Config.Style, [.. ChainStyle.Available.Select((style, index) => new ListItem(style.Key, Translation.Of(style.Key), index))], "Chain Style", span: 4f);
        menu.AddFloatSlider(Config.SoundVolume, "Chain Sound Volume", span: 4f);
    }
}
