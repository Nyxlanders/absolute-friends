using MonoMod.RuntimeDetour;
using RainMeadow;

namespace AbsoluteFriends.Meadow;

internal static class LostAvatars
{
    private static Hook? _enteringWorldHook;

    internal static void Register()
    {
        _enteringWorldHook ??= new Hook(
            typeof(WorldSession).GetMethod(nameof(WorldSession.ApoEnteringWorld)),
            On_WorldSession_ApoEnteringWorld
        );
    }

    internal static void Unregister()
    {
        _enteringWorldHook?.Dispose();
        _enteringWorldHook = null;
    }

    private static void On_WorldSession_ApoEnteringWorld(Action<WorldSession, AbstractPhysicalObject> orig, WorldSession self, AbstractPhysicalObject abstractPhysicalObject)
    {
        if (
            OnlineManager.lobby?.gameMode.avatars.FirstOrDefault(avatar => avatar.apo == abstractPhysicalObject) is { } avatar
            && !OnlinePhysicalObject.map.TryGetValue(abstractPhysicalObject, out _)
            && !OnlineManager.recentEntities.ContainsKey(avatar.id)
        )
        {
            OnlineManager.recentEntities.Add(avatar.id, avatar);
            OnlinePhysicalObject.map.Add(abstractPhysicalObject, avatar);
        }

        orig(self, abstractPhysicalObject);
    }
}
