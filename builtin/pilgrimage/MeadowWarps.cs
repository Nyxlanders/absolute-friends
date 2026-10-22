using AbsoluteFriends.Core;
using AbsoluteFriends.Options;
using AbsoluteFriends.Utils;
using RainMeadow;
using Watcher;

namespace AbsoluteFriends.Pilgrimage;

internal static class MeadowWarps
{
    private static readonly List<string> _carriedFriends = [];

    private static bool IsActive => MeadowOptions.IsInLobby && OnlineManager.lobby?.gameMode is StoryGameMode && Config.Gate.IsActive;

    internal static void Register()
    {
        On.Watcher.WarpPoint.NewWorldLoaded_Room -= On_WarpPoint_NewWorldLoaded_Room;
        On.Watcher.WarpPoint.NewWorldLoaded_Room += On_WarpPoint_NewWorldLoaded_Room;
        On.Watcher.WarpPoint.SpawnPendingObject -= On_WarpPoint_SpawnPendingObject;
        On.Watcher.WarpPoint.SpawnPendingObject += On_WarpPoint_SpawnPendingObject;
        On.RainWorldGame.Win -= On_RainWorldGame_Win;
        On.RainWorldGame.Win += On_RainWorldGame_Win;
    }

    internal static void Unregister()
    {
        On.Watcher.WarpPoint.NewWorldLoaded_Room -= On_WarpPoint_NewWorldLoaded_Room;
        On.Watcher.WarpPoint.SpawnPendingObject -= On_WarpPoint_SpawnPendingObject;
        On.RainWorldGame.Win -= On_RainWorldGame_Win;
        _carriedFriends.Clear();
    }

    private static void On_RainWorldGame_Win(On.RainWorldGame.orig_Win orig, RainWorldGame self, bool malnourished, bool fromWarpPoint)
    {
        if (IsActive && fromWarpPoint && OnlineManager.lobby.isOwner && self.manager.upcomingProcess == null && self.GetStorySession.saveState.warpPointTargetAfterWarpPointSave != null)
        {
            _carriedFriends.Clear();
            _carriedFriends.AddRange(FriendUtils.TrackedFriends
                .Where(abstractCreature => abstractCreature.HasAuthority && abstractCreature.state?.dead != true)
                .Select(SaveState.AbstractCreatureToStringStoryWorld));
        }

        orig(self, malnourished, fromWarpPoint);
    }

    private static void On_WarpPoint_NewWorldLoaded_Room(On.Watcher.WarpPoint.orig_NewWorldLoaded_Room orig, WarpPoint self, Room newRoom)
    {
        if (!IsActive)
        {
            orig(self, newRoom);

            return;
        }

        Room? room = self.room;
        AbstractCreature[] friends = room == null ? [] : [.. FriendUtils.TrackedFriends.Where(abstractCreature => abstractCreature.HasAuthority
            && abstractCreature.realizedCreature is { dead: false, grabbedBy.Count: 0 } creature
            && creature.room == room)];
        StoryGameSession session = newRoom.game.GetStorySession;

        orig(self, newRoom);

        List<AbstractPhysicalObject> pending = session.pendingWarpPointTransferObjects;

        if (room == null)
        {
            BringCarriedFriends(newRoom.world, session.saveState, pending);

            return;
        }

        if (session.saveState.warpPointTargetAfterWarpPointSave != null)
        {
            return;
        }

        foreach (var friend in friends)
        {
            if (pending.Contains(friend))
            {
                continue;
            }

            if (friend.realizedCreature is { room: { } friendRoom } creature)
            {
                friendRoom.RemoveObject(creature);
            }

            friend.Room?.RemoveEntity(friend);
            pending.Add(friend);
        }
    }

    private static bool On_WarpPoint_SpawnPendingObject(On.Watcher.WarpPoint.orig_SpawnPendingObject orig, WarpPoint self, AbstractPhysicalObject nextObject, bool immediateSpawn)
    {
        bool isSpawned = orig(self, nextObject, immediateSpawn);

        if (isSpawned && IsActive && nextObject.Room is { } abstractRoom && !abstractRoom.entities.Contains(nextObject))
        {
            abstractRoom.AddEntity(nextObject);
        }

        return isSpawned;
    }

    private static void BringCarriedFriends(World world, SaveState saveState, List<AbstractPhysicalObject> pending)
    {
        if (_carriedFriends.Count == 0 || !OnlineManager.lobby.isOwner)
        {
            return;
        }

        WorldCoordinate den = new(world.offScreenDen.index, -1, -1, -1);

        foreach (var data in _carriedFriends)
        {
            if (SaveState.AbstractCreatureFromString(world, data, false, den) is not { } friend)
            {
                continue;
            }

            string key = $"<cA>{friend.ID}<";

            if (saveState.pendingFriendCreatures.Concat(saveState.spawnedPendingFriendCreatures).Any(entry => entry.Contains(key)))
            {
                continue;
            }

            foreach (var regionState in saveState.regionStates)
            {
                regionState?.savedPopulation.RemoveAll(entry => entry.Contains(key));
            }

            pending.Add(friend);
        }

        _carriedFriends.Clear();
    }
}
