using System.Reflection;
using AbsoluteFriends.Diagnostics;
using AbsoluteFriends.Options;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;

namespace AbsoluteFriends.Pilgrimage;

internal static class MeadowShelters
{
    private static readonly AddonLogger _logger = Reporter.GetLogger(Plugin.Name);

    private static ILHook? _closeHook;

    internal static void Register()
    {
        _closeHook ??= new ILHook(
            typeof(RainMeadow.RainMeadow).GetMethod("ShelterDoorOnClose", BindingFlags.NonPublic | BindingFlags.Instance),
            IL_RainMeadow_ShelterDoorOnClose
        );
    }

    internal static void Unregister()
    {
        _closeHook?.Dispose();
        _closeHook = null;
    }

    private static void IL_RainMeadow_ShelterDoorOnClose(ILContext il)
    {
        ILCursor c = new(il);

        if (!ReplaceThreshold(c, "timeSinceInCorridorMode", 10) || !ReplaceThreshold(c, "touchedNoInputCounter", 80))
        {
            _logger.LogWarning("Could not patch Rain Meadow's shelter check");
        }
    }

    private static bool ReplaceThreshold(ILCursor c, string field, int threshold)
    {
        c.Index = 0;

        if (!c.TryGotoNext(MoveType.After, i => i.MatchLdfld<Player>(field), i => i.MatchLdcI4(threshold)))
        {
            return false;
        }

        c.EmitGuarded((int value) => Config.Gate.IsActive ? 0 : value, value => value);

        return true;
    }
}
