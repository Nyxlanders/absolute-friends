using AbsoluteFriends.Options;

namespace AbsoluteFriends.Pilgrimage;

internal static class Menu
{
    public static void Build(MenuBuilder menu)
    {
        menu.SetColumns(4);
        menu.AddCheckBox(Config.Gate);
        menu.AddFloatSlider(Config.GateTime, span: 3f);
        menu.AddCheckBox(Config.GateForce);
        menu.AddFloatSlider(Config.GateForceTime, span: 3f);
        menu.SetColumns(5);
        menu.AddCheckBox(Config.AnyShelter);
        menu.AddCheckBox(Config.Perseverance);
        menu.AddCheckBox(Config.Echo);
        menu.AddCheckBox(Config.TempleGuard);
        menu.AddCheckBox(Config.VoidSea);
    }
}
