using AbsoluteFriends.Options;

namespace AbsoluteFriends.Manager;

internal static class Menu
{
    public static void Build(MenuBuilder menu)
    {
        menu.AddKeyBinder(Config.ManagerKey, "Manager Key");
    }
}
