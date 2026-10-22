using AbsoluteFriends.Core;
using MonoMod.RuntimeDetour;
using RainMeadow;

namespace AbsoluteFriends.Meadow;

internal static class OwnerSync
{
    private static Hook? _registerHook;

    internal static void Register()
    {
        _registerHook ??= new Hook(
            typeof(OnlinePhysicalObject).GetMethod(nameof(OnlinePhysicalObject.NewFromApo)),
            On_OnlinePhysicalObject_NewFromApo
        );

        OwnerUtils.RegisterRemoteOwnerSource(RemoteOwner);
    }

    internal static void Unregister()
    {
        _registerHook?.Dispose();
        _registerHook = null;

        OwnerUtils.UnregisterRemoteOwnerSource(RemoteOwner);
    }

    private static OnlinePhysicalObject On_OnlinePhysicalObject_NewFromApo(Func<AbstractPhysicalObject, OnlinePhysicalObject> orig, AbstractPhysicalObject apo)
    {
        OnlinePhysicalObject onlineObject = orig(apo);

        if (!onlineObject.TryGetData<OwnerData>(out _))
        {
            onlineObject.AddData(new OwnerData());
        }

        return onlineObject;
    }

    private static AbstractCreature? RemoteOwner(PhysicalObject physicalObject) =>
        physicalObject.abstractPhysicalObject.GetOnlineObject() is { } onlineObject && onlineObject.TryGetData(out OwnerData ownerData)
            ? ownerData.Owner
            : null;
}
