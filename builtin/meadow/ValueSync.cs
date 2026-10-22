using MonoMod.RuntimeDetour;
using RainMeadow;

namespace AbsoluteFriends.Meadow;

internal static class ValueSync
{
    private static Hook? _registerHook;

    internal static void Register()
    {
        _registerHook ??= new Hook(
            typeof(OnlinePhysicalObject).GetMethod(nameof(OnlinePhysicalObject.NewFromApo)),
            On_OnlinePhysicalObject_NewFromApo
        );
    }

    internal static void Unregister()
    {
        _registerHook?.Dispose();
        _registerHook = null;
    }

    private static OnlinePhysicalObject On_OnlinePhysicalObject_NewFromApo(Func<AbstractPhysicalObject, OnlinePhysicalObject> orig, AbstractPhysicalObject apo)
    {
        OnlinePhysicalObject onlineObject = orig(apo);

        if (!onlineObject.TryGetData<ValueData>(out _))
        {
            onlineObject.AddData(new ValueData());
        }

        return onlineObject;
    }
}
