using AbsoluteFriends.Options;
using UnityEngine;

namespace AbsoluteFriends.Manager;

internal static class Config
{
    public static Configurable<KeyCode> ManagerKey = null!;

    public static void Bind(Addon addon)
    {
        ManagerKey = addon.Bind("ManagerKey", KeyCode.F7, new ConfigurableInfo("Opens the friend manager during gameplay or while paused.", tags: ["Manager Key"]))
            .UnsyncInMeadow();
    }
}
