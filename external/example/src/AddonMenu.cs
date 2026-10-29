using AbsoluteFriends.Options;

namespace AbsoluteFriendsExample;

// A separate menu class is only an organizational choice; BuildMenu may compose the panel directly.
internal static class AddonMenu
{
    internal static void Build(MenuBuilder menu)
    {
        menu.SetColumns(2);
        menu.AddCheckBox(AddonConfig.Friendship, enabled: ModManager.MSC);
        menu.AddCheckBox(AddonConfig.AggressiveHunter, enabled: ModManager.MSC);
        menu.AddCheckBox(AddonConfig.HunterDaddyOwnership, enabled: ModManager.MSC);
        menu.AddCheckBox(AddonConfig.HunterName, enabled: ModManager.MSC);
        menu.AddCheckBox(AddonConfig.GreenNeuronOwnership, enabled: ModManager.MSC);
    }
}
