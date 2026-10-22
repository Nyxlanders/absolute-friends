using System.Reflection;
using AbsoluteFriends.Options;
using AbsoluteFriends.Utils;
using MonoMod.RuntimeDetour;
using RainMeadow;

namespace AbsoluteFriends.Pilgrimage;

internal static class MeadowGates
{
    private static readonly FieldInfo? _gateRoom = typeof(StoryGameMode).GetField("gateRoom", BindingFlags.NonPublic | BindingFlags.Instance);

    private static Hook? _lobbyTickHook;

    private static Hook? _meetRequirementHook;

    private static bool _isHoldingGate;

    internal static bool IsActive => MeadowOptions.IsInLobby && OnlineManager.lobby?.gameMode is StoryGameMode && Config.Perseverance.IsActive;

    internal static void Register()
    {
        _lobbyTickHook ??= new Hook(
            typeof(StoryGameMode).GetMethod(nameof(StoryGameMode.LobbyTick)),
            On_StoryGameMode_LobbyTick
        );
        _meetRequirementHook ??= new Hook(
            typeof(RegionGate).GetProperty(nameof(RegionGate.MeetRequirement)).GetGetMethod(),
            On_RegionGate_get_MeetRequirement
        );

        On.RegionGate.PlayersInZone -= On_RegionGate_PlayersInZone;
        On.RegionGate.PlayersInZone += On_RegionGate_PlayersInZone;
        On.RegionGate.Update -= On_RegionGate_Update;
        On.RegionGate.Update += On_RegionGate_Update;
        On.RoomRealizer.CanAbstractizeRoom -= On_RoomRealizer_CanAbstractizeRoom;
        On.RoomRealizer.CanAbstractizeRoom += On_RoomRealizer_CanAbstractizeRoom;
    }

    internal static void Unregister()
    {
        _lobbyTickHook?.Dispose();
        _lobbyTickHook = null;
        _meetRequirementHook?.Dispose();
        _meetRequirementHook = null;

        On.RegionGate.PlayersInZone -= On_RegionGate_PlayersInZone;
        On.RegionGate.Update -= On_RegionGate_Update;
        On.RoomRealizer.CanAbstractizeRoom -= On_RoomRealizer_CanAbstractizeRoom;
    }

    private static int On_RegionGate_PlayersInZone(On.RegionGate.orig_PlayersInZone orig, RegionGate self)
    {
        if (!IsActive || self.room?.game is not { } game)
        {
            return orig(self);
        }

        int zone = -1;

        foreach (var abstractPlayer in game.AllPlayers)
        {
            if (abstractPlayer.state is { dead: true })
            {
                continue;
            }

            int playerZone = self.DetectZone(abstractPlayer);

            if (playerZone < 0 || (zone >= 0 && playerZone != zone))
            {
                return -1;
            }

            zone = playerZone;
        }

        return zone;
    }

    private static void On_RegionGate_Update(On.RegionGate.orig_Update orig, RegionGate self, bool eu)
    {
        if (
            !IsActive
            || OnlineManager.lobby?.gameMode is not StoryGameMode story
            || story.storyClientData is not { isDead: true }
            || story.readyForTransition == StoryGameMode.ReadyForTransition.Closed
            || !self.room.game.Players.Any(abstractPlayer => HasBody(abstractPlayer) && abstractPlayer.pos.room != self.room.abstractRoom.index)
        )
        {
            orig(self, eu);
            return;
        }

        StoryGameMode.ReadyForTransition readyForTransition = story.readyForTransition;
        story.readyForTransition = StoryGameMode.ReadyForTransition.Closed;
        _isHoldingGate = true;

        try
        {
            orig(self, eu);
        }
        finally
        {
            _isHoldingGate = false;
            story.readyForTransition = readyForTransition;
        }
    }

    private static bool On_RegionGate_get_MeetRequirement(Func<RegionGate, bool> orig, RegionGate self)
    {
        return !_isHoldingGate && orig(self);
    }

    private static bool On_RoomRealizer_CanAbstractizeRoom(On.RoomRealizer.orig_CanAbstractizeRoom orig, RoomRealizer self, RoomRealizer.RealizedRoomTracker tracker)
    {
        return orig(self, tracker)
            && !(
                IsActive
                && tracker.room.gate
                && OnlineManager.lobby.gameMode is StoryGameMode { storyClientData.isDead: true } story
                && story.readyForTransition >= StoryGameMode.ReadyForTransition.Opening
            );
    }

    private static void On_StoryGameMode_LobbyTick(Action<StoryGameMode, uint> orig, StoryGameMode self, uint tick)
    {
        StoryGameMode.ReadyForTransition readyForTransition = self.readyForTransition;

        orig(self, tick);

        if (!IsActive)
        {
            return;
        }

        KeepOpenForLivingPlayers(self, readyForTransition);
        OpenForLivingPlayers(self);
        LeaveDeadBodies(self);
        MeadowSpectating.Update(self);
    }

    private static bool HasBody(AbstractCreature abstractPlayer)
    {
        return !abstractPlayer.slatedForDeletion
            && abstractPlayer.realizedCreature is not { slatedForDeletetion: true }
            && abstractPlayer.world.IsRoomInRegion(abstractPlayer.pos.room)
            && abstractPlayer.Room.entities.Contains(abstractPlayer);
    }

    private static List<ClientSettings> LivingClients(Lobby lobby)
    {
        return [.. lobby.clientSettings.Values.Where(settings => settings.inGame
            && settings.TryGetData(out StoryClientSettingsData data)
            && !data.isDead)];
    }

    private static void KeepOpenForLivingPlayers(StoryGameMode self, StoryGameMode.ReadyForTransition readyForTransition)
    {
        if (
            readyForTransition != StoryGameMode.ReadyForTransition.Crossed
            || self.readyForTransition != StoryGameMode.ReadyForTransition.Closed
            || OnlineManager.lobby is not { isOwner: true } lobby
            || !LivingClients(lobby).Any(settings => settings.GetData<StoryClientSettingsData>().readyForTransition)
        )
        {
            return;
        }

        self.readyForTransition = StoryGameMode.ReadyForTransition.Crossed;
    }

    private static void OpenForLivingPlayers(StoryGameMode self)
    {
        if (self.readyForTransition != StoryGameMode.ReadyForTransition.MeetRequirement || OnlineManager.lobby is not { isOwner: true } lobby)
        {
            return;
        }

        List<ClientSettings> living = LivingClients(lobby);

        if (living.Count == 0 || !living.All(settings => settings.GetData<StoryClientSettingsData>().readyForTransition))
        {
            return;
        }

        List<int> rooms = [.. living
            .SelectMany(settings => settings.avatars)
            .Select(id => id.FindEntity(quiet: true))
            .OfType<OnlinePhysicalObject>()
            .Where(onlineObject => onlineObject.apo is AbstractCreature { state.dead: false })
            .Select(onlineObject => onlineObject.apo.pos.room)
            .Distinct()];

        if (rooms.Count != 1)
        {
            return;
        }

        RainWorld.roomIndexToName.TryGetValue(rooms[0], out string gateRoom);
        _gateRoom?.SetValue(self, gateRoom);
        self.readyForTransition = StoryGameMode.ReadyForTransition.Opening;
    }

    private static void LeaveDeadBodies(StoryGameMode self)
    {
        if (
            self.readyForTransition != StoryGameMode.ReadyForTransition.Opening
            || self.storyClientData is not { isDead: true }
            || RainWorldUtils.CurrentGame is not { world: { } world } game
        )
        {
            return;
        }

        List<AbstractCreature> living = [.. game.AllPlayers.Where(abstractPlayer => abstractPlayer.state is { dead: false })];

        if (living.Count == 0 || living.Select(abstractPlayer => abstractPlayer.pos.room).Distinct().Count() != 1 || !world.IsRoomInRegion(living[0].pos.room))
        {
            return;
        }

        AbstractRoom gateRoom = world.GetAbstractRoom(living[0].pos.room);

        if (!gateRoom.gate)
        {
            return;
        }

        if (gateRoom.realizedRoom == null)
        {
            game.roomRealizer?.MeadowRealizeAndTrackRoom(gateRoom, false);
        }

        if (gateRoom.realizedRoom?.regionGate is not { } regionGate || living.Any(abstractPlayer => abstractPlayer.realizedCreature is not { } creature || !regionGate.IsInRegionGate(creature)))
        {
            return;
        }

        foreach (var body in game.Players.Where(abstractPlayer => HasBody(abstractPlayer) && abstractPlayer.Room != gateRoom).ToList())
        {
            if (body.realizedCreature is { } creature)
            {
                creature.Destroy();
            }
            else
            {
                body.Destroy();
                body.Room.RemoveEntity(body);
            }
        }
    }
}
