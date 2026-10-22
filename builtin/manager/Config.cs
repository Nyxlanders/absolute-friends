using AbsoluteFriends.Options;
using UnityEngine;

namespace AbsoluteFriends.Manager;

internal static class Config
{
    public static Configurable<KeyCode> ManagerKey = null!;

    public static Configurable<bool> TrackNewFriends = null!;

    public static void Bind(Addon addon)
    {
        ManagerKey = addon.Bind("ManagerKey", KeyCode.F7, new ConfigurableInfo("Opens the friend manager during gameplay or while paused.", tags: ["Manager Key"]))
            .UnsyncInMeadow();
        TrackNewFriends = addon.Bind("TrackNewFriends", true, new ConfigurableInfo("Tracks newly tamed friends. When off, new friends start untracked.", tags: ["Track New Friends"]));

        addon.Group("Track New Friends", "Tracks newly tamed friends. When off, new friends start untracked.", TrackNewFriends);
    }
}
