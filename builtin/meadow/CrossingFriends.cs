using AbsoluteFriends.Core;
using AbsoluteFriends.Utils;
using RainMeadow;

namespace AbsoluteFriends.Meadow;

internal static class CrossingFriends
{
    internal static void Register()
    {
        On.RegionGate.Update -= On_RegionGate_Update;
        On.RegionGate.Update += On_RegionGate_Update;
    }

    internal static void Unregister() => On.RegionGate.Update -= On_RegionGate_Update;

    private static void On_RegionGate_Update(On.RegionGate.orig_Update orig, RegionGate self, bool eu)
    {
        orig(self, eu);

        if (
            OnlineManager.lobby?.gameMode is not StoryGameMode
            || !self.waitingForWorldLoader
            || (self.mode != RegionGate.Mode.ClosingAirLock && self.mode != RegionGate.Mode.Waiting)
            || self.room?.game is not { } game
            || game.clock % RainWorldUtils.Second != 0
        )
        {
            return;
        }

        foreach (var abstractCreature in self.room.abstractRoom.creatures)
        {
            if (
                abstractCreature is { IsPlayer: false, IsTracked: true, stuckObjects.Count: 0 }
                && abstractCreature.GetOnlineObject() is { isMine: false, isTransferable: true, isPending: false, currentlyJoinedResource.isAvailable: true } onlineObject
                && FriendOf(abstractCreature, game) is { } friend
                && game.Players.Contains(friend)
            )
            {
                onlineObject.Request();
            }
        }
    }

    private static AbstractCreature? FriendOf(AbstractCreature abstractCreature, RainWorldGame game)
    {
        if (abstractCreature.abstractAI?.RealAI?.friendTracker?.friend?.abstractCreature is { IsPlayer: true, state.dead: false } followed)
        {
            return followed;
        }

        return game.AllPlayers
            .Where(abstractPlayer => abstractPlayer.state is { dead: false })
            .Select(abstractPlayer => (Player: abstractPlayer, Like: abstractCreature.state?.socialMemory?.GetRelationship(abstractPlayer.ID)?.like ?? 0f))
            .Where(liked => liked.Like > FriendUtils.FriendLikeThreshold)
            .OrderByDescending(liked => liked.Like)
            .Select(liked => liked.Player)
            .FirstOrDefault();
    }
}
