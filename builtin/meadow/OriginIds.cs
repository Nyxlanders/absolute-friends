using AbsoluteFriends.Manager;
using MonoMod.RuntimeDetour;
using RainMeadow;

namespace AbsoluteFriends.Meadow;

internal static class OriginIds
{
    private static Hook? _createHook;

    private static Hook? _saveHook;

    internal static void Register()
    {
        _createHook ??= new Hook(
            typeof(OnlineCreature).GetMethod(nameof(OnlineCreature.AbstractCreatureFromString)),
            On_OnlineCreature_AbstractCreatureFromString
        );
        _saveHook ??= new Hook(
            typeof(SaveState).GetMethod(nameof(SaveState.AbstractCreatureToStringStoryWorld), [typeof(AbstractCreature), typeof(WorldCoordinate)]),
            On_SaveState_AbstractCreatureToStringStoryWorld
        );

        FriendManager.RegisterOriginIdRule(EntityRecords.OriginId);
    }

    internal static void Unregister()
    {
        _createHook?.Dispose();
        _createHook = null;
        _saveHook?.Dispose();
        _saveHook = null;

        FriendManager.UnregisterOriginIdRule(EntityRecords.OriginId);
    }

    private static AbstractCreature? On_OnlineCreature_AbstractCreatureFromString(Func<World, string, WorldCoordinate, AbstractCreature?> orig, World world, string creatureString, WorldCoordinate pos)
    {
        AbstractCreature? abstractCreature = orig(world, creatureString, pos);

        if (abstractCreature != null)
        {
            EntityRecords.RecordOrigin(abstractCreature);
        }

        return abstractCreature;
    }

    private static string On_SaveState_AbstractCreatureToStringStoryWorld(Func<AbstractCreature, WorldCoordinate, string> orig, AbstractCreature critter, WorldCoordinate pos)
    {
        if (OnlineManager.lobby is not { isOwner: true } || EntityRecords.OriginId(critter) is not { } origin || origin == critter.ID)
        {
            return orig(critter, pos);
        }

        EntityID id = critter.ID;

        critter.ID = origin;

        try
        {
            return orig(critter, pos);
        }
        finally
        {
            critter.ID = id;
        }
    }
}
