using MonoMod.RuntimeDetour;
using RainMeadow;

namespace AbsoluteFriends.Meadow;

internal static class MemorySync
{
    private static Hook? _registerHook;

    private static Hook? _readHook;

    internal static void Register()
    {
        _registerHook ??= new Hook(
            typeof(OnlinePhysicalObject).GetMethod(nameof(OnlinePhysicalObject.NewFromApo)),
            On_OnlinePhysicalObject_NewFromApo
        );

        _readHook ??= new Hook(
            typeof(CreatureStateState).GetMethod(nameof(CreatureStateState.ReadTo), [typeof(AbstractCreature)]),
            On_CreatureStateState_ReadTo
        );
    }

    internal static void Unregister()
    {
        _registerHook?.Dispose();
        _registerHook = null;

        _readHook?.Dispose();
        _readHook = null;
    }

    private static OnlinePhysicalObject On_OnlinePhysicalObject_NewFromApo(Func<AbstractPhysicalObject, OnlinePhysicalObject> orig, AbstractPhysicalObject apo)
    {
        OnlinePhysicalObject onlineObject = orig(apo);

        if (apo is AbstractCreature { creatureTemplate.socialMemory: true } && !onlineObject.TryGetData<MemoryData>(out _))
        {
            onlineObject.AddData(new MemoryData());
        }

        return onlineObject;
    }

    private static void On_CreatureStateState_ReadTo(Action<CreatureStateState, AbstractCreature> orig, CreatureStateState self, AbstractCreature abstractCreature)
    {
        orig(self, abstractCreature);

        if (abstractCreature.GetOnlineObject() is { } onlineObject && onlineObject.TryGetData(out MemoryData memoryData))
        {
            memoryData.Translate(abstractCreature);
        }
    }
}
