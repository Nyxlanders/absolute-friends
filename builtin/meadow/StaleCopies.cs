using AbsoluteFriends.Core;
using AbsoluteFriends.Utils;
using RainMeadow;

namespace AbsoluteFriends.Meadow;

internal static class StaleCopies
{
    private const int Interval = 40;

    private static int _counter;

    internal static void Register()
    {
        On.RainWorldGame.Update -= On_RainWorldGame_Update;
        On.RainWorldGame.Update += On_RainWorldGame_Update;
    }

    internal static void Unregister() => On.RainWorldGame.Update -= On_RainWorldGame_Update;

    private static void On_RainWorldGame_Update(On.RainWorldGame.orig_Update orig, RainWorldGame self)
    {
        orig(self);

        if (OnlineManager.lobby == null || ++_counter < Interval)
        {
            return;
        }

        _counter = 0;

        List<AbstractCreature> candidates = [.. FriendUtils.TrackingCandidates];
        HashSet<EntityID> onlineOrigins = [.. candidates.Where(abstractCreature => abstractCreature.GetOnlineObject() != null).Select(EntityRecords.OriginOf)];

        foreach (var abstractCreature in candidates)
        {
            if (
                !abstractCreature.IsPlayer
                && abstractCreature.GetOnlineObject() == null
                && EntityRecords.TryGetOwner(abstractCreature, out _, out _)
                && onlineOrigins.Contains(EntityRecords.OriginOf(abstractCreature))
            )
            {
                abstractCreature.Untrack();
            }
        }
    }
}
