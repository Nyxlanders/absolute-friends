using AbsoluteFriends.Hooks;
using AbsoluteFriends.Utils;

namespace AbsoluteFriends.Core;

internal class FriendTrackerHooks : BaseHooks
{
    private const int RemoteInterval = 10;

    private static int _remoteTimer;

    protected override Configurable<bool>[] Options => [Config.FriendSlugcat, Config.FriendCreature, Config.FriendNeutralCreature, Config.FriendSharing, Config.FriendChaining];

    protected override bool IsOptionEnabled => base.IsOptionEnabled || CreatureRules.HasConfiguredRules;

    protected override string? Subject => "Friend Tracker";

    [HookPatch(typeof(On.RainWorldGame), nameof(On.RainWorldGame.ctor))]
    private static void On_RainWorldGame_ctor(On.RainWorldGame.orig_ctor orig, RainWorldGame self, ProcessManager manager)
    {
        orig(self, manager);

        FriendUtils.ClearTrackedFriends();
    }

    [HookPatch(typeof(On.RainWorldGame), nameof(On.RainWorldGame.Update))]
    private static void On_RainWorldGame_Update(On.RainWorldGame.orig_Update orig, RainWorldGame self)
    {
        orig(self);

        FriendUtils.PruneTrackedFriends();

        if (AuthorityUtils.HasRemoteRules && ++_remoteTimer >= RemoteInterval)
        {
            _remoteTimer = 0;

            UpdateRemoteTracking(self);
        }
    }

    [HookPatch(typeof(On.FriendTracker), nameof(On.FriendTracker.Update))]
    private static void On_FriendTracker_Update(On.FriendTracker.orig_Update orig, FriendTracker self)
    {
        orig(self);

        if (self.AI?.creature is not { IsPlayer: false } abstractCreature)
        {
            return;
        }

        self.AdoptSlugcatFriend();

        UpdateTracking(self, abstractCreature);
    }

    private static void UpdateRemoteTracking(RainWorldGame game)
    {
        foreach (var room in game.world?.activeRooms ?? [])
        {
            foreach (var abstractCreature in room.abstractRoom.creatures)
            {
                if (abstractCreature is { IsPlayer: false, HasAuthority: false } && abstractCreature.abstractAI?.RealAI?.friendTracker is { } tracker)
                {
                    UpdateTracking(tracker, abstractCreature);
                }
            }
        }
    }

    private static void UpdateTracking(FriendTracker self, AbstractCreature abstractCreature)
    {
        if (abstractCreature.IsSlugpup)
        {
            AbstractCreature? abstractPlayer = self.friend?.abstractCreature is { IsPlayer: true, slatedForDeletion: false, state.dead: false } followedPlayer
                ? followedPlayer
                : abstractCreature.FavoredPlayer;

            if (abstractPlayer == null)
            {
                abstractCreature.Untrack();
            }
            else if (abstractPlayer.IsPlayer)
            {
                abstractCreature.Track();
            }

            return;
        }

        if (
            (self.HasSlugcatFriend && abstractCreature.IsFriend(self.friend?.abstractCreature))
            || abstractCreature.IsCloseFriendOfPlayer
        )
        {
            abstractCreature.Track();
        }
        else
        {
            abstractCreature.Untrack();
        }
    }
}
