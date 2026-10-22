using AbsoluteFriends.Utils;

namespace AbsoluteFriends.Players;

internal static class RippleSync
{
    private const string Key = Plugin.GUID + ".rippleLayer";

    internal static void Register() => AuthorityUtils.RegisterSyncedValue(Key, Read, Write);

    internal static void Unregister() => AuthorityUtils.UnregisterSyncedValue(Key);

    private static int? Read(AbstractPhysicalObject abstractPhysicalObject) => ModManager.Watcher && abstractPhysicalObject is AbstractCreature { IsSlugcat: false }
        ? abstractPhysicalObject.rippleLayer
        : null;

    private static void Write(AbstractPhysicalObject abstractPhysicalObject, int rippleLayer)
    {
        if (abstractPhysicalObject.rippleLayer != rippleLayer)
        {
            abstractPhysicalObject.rippleLayer = rippleLayer;
        }
    }
}
