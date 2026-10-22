using AbsoluteFriends.Utils;
using MonoMod.RuntimeDetour;
using RainMeadow;

namespace AbsoluteFriends.Meadow;

internal static class RevivedPlayers
{
    private static Hook? _lobbyTickHook;

    private static bool _wasDead;

    internal static void Register()
    {
        _lobbyTickHook ??= new Hook(
            typeof(StoryGameMode).GetMethod(nameof(StoryGameMode.LobbyTick)),
            On_StoryGameMode_LobbyTick
        );
    }

    internal static void Unregister()
    {
        _lobbyTickHook?.Dispose();
        _lobbyTickHook = null;
        _wasDead = false;
    }

    private static void On_StoryGameMode_LobbyTick(Action<StoryGameMode, uint> orig, StoryGameMode self, uint tick)
    {
        orig(self, tick);

        ReturnHuds();

        bool isDead = self.storyClientData?.isDead == true;

        if (_wasDead && !isDead)
        {
            ReturnCamera();
        }

        _wasDead = isDead;
    }

    private static void ReturnHuds()
    {
        foreach (var camera in RainWorldUtils.CurrentGame?.cameras ?? [])
        {
            if (camera?.hud is { owner: Player { abstractCreature.realizedCreature: Player body } owner } hud && body != owner)
            {
                hud.owner = body;
            }
        }
    }

    private static void ReturnCamera()
    {
        if (
            RainWorldUtils.CurrentGame is not { } game
            || RainWorldUtils.MainCamera(game)?.hud?.parts.OfType<SpectatorHud>().FirstOrDefault() is not { Spectatee: { } spectatee } spectatorHud
            || game.Players.Contains(spectatee)
        )
        {
            return;
        }

        spectatorHud.Spectatee = null;
    }
}
