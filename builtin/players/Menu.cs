using AbsoluteFriends.Options;

namespace AbsoluteFriends.Players;

internal static class Menu
{
    public static void Build(MenuBuilder menu)
    {
        menu.SetColumns(4);
        menu.AddCheckBox(Config.GrabbingPlayer);
        menu.AddFloatSlider(Config.GrabbingPlayerTime, span: 3f);
        menu.AddCheckBox(Config.Wiggle);
        menu.AddCheckBox(Config.Carry);
        menu.AddCheckBox(Config.CarryStun);
        menu.AddCheckBox(Config.Mauling);
        menu.AddCheckBox(Config.GourmandSlam, enabled: ModManager.MSC);
        menu.AddCheckBox(Config.ArtificerParry, enabled: ModManager.MSC);
        menu.AddCheckBox(Config.SaintTongue, enabled: ModManager.MSC);
        menu.AddCheckBox(Config.SaintAttunement, enabled: ModManager.MSC);
        menu.SetColumns(2);
        menu.AddCheckBox(Config.WatcherCamouflage, enabled: ModManager.Watcher);
        menu.AddCheckBox(Config.RippleSpace, enabled: ModManager.Watcher);
    }
}
