using AbsoluteFriends.Utils;
using MonoMod.RuntimeDetour;
using RainMeadow;

namespace AbsoluteFriends.Pilgrimage;

internal static class MeadowSpectating
{
    private static Hook? _spectatorOverlayUpdateHook;

    private static EntityID? _lostSpectatee;

    internal static void Register()
    {
        _spectatorOverlayUpdateHook ??= new Hook(
            typeof(SpectatorOverlay).GetMethod(nameof(SpectatorOverlay.Update)),
            On_SpectatorOverlay_Update
        );
    }

    internal static void Unregister()
    {
        _spectatorOverlayUpdateHook?.Dispose();
        _spectatorOverlayUpdateHook = null;
        _lostSpectatee = null;
    }

    internal static void Update(StoryGameMode self)
    {
        RehomeLostPlayers(self);
        KeepSpectating(self);
    }

    private static void RehomeLostPlayers(StoryGameMode self)
    {
        if (
            self.storyClientData is not { isDead: true }
            || RainWorldUtils.CurrentGame is not { world: { } world } game
            || RainWorldUtils.MainCamera(game)?.room?.abstractRoom is not { } cameraRoom
            || cameraRoom.world != world
        )
        {
            return;
        }

        foreach (var abstractPlayer in game.Players)
        {
            if (abstractPlayer.world == world && world.IsRoomInRegion(abstractPlayer.pos.room))
            {
                continue;
            }

            abstractPlayer.world = world;
            abstractPlayer.pos = new WorldCoordinate(cameraRoom.index, -1, -1, 0);
        }
    }

    private static void KeepSpectating(StoryGameMode self)
    {
        if (
            self.storyClientData is not { isDead: true }
            || RainWorldUtils.CurrentGame is not { world: { } world } game
            || RainWorldUtils.MainCamera(game)?.hud?.parts.OfType<SpectatorHud>().FirstOrDefault() is not { } spectatorHud
            || game.Players.FirstOrDefault(abstractPlayer => abstractPlayer.world == world) is not { } ownPlayer
        )
        {
            return;
        }

        if (spectatorHud.Spectatee is { } spectatee && spectatee.world != world)
        {
            _lostSpectatee = spectatee.ID;
            spectatorHud.Spectatee = ownPlayer;
            return;
        }

        if (_lostSpectatee is not { } lostSpectatee)
        {
            return;
        }

        if (spectatorHud.Spectatee != ownPlayer)
        {
            _lostSpectatee = null;
            return;
        }

        if (game.AllPlayers.FirstOrDefault(abstractPlayer => abstractPlayer.ID == lostSpectatee && abstractPlayer.world == world && abstractPlayer.Room != null) is { } found)
        {
            _lostSpectatee = null;
            spectatorHud.Spectatee = found;
        }
    }

    private static void On_SpectatorOverlay_Update(Action<SpectatorOverlay> orig, SpectatorOverlay self)
    {
        if (MeadowGates.IsActive && self.playerScroller != null)
        {
            List<OnlineCreature> avatars = [.. OnlineManager.lobby.playerAvatars.Select(avatar => avatar.Value.FindEntity(quiet: true)).OfType<OnlineCreature>()];

            foreach (var button in self.PlayerButtons)
            {
                button.avatars = [.. avatars.Where(avatar => avatar.owner == button.player)];
            }
        }

        orig(self);
    }
}
