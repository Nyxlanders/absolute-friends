using System.Runtime.CompilerServices;
using AbsoluteFriends.Core;
using AbsoluteFriends.Hooks;
using AbsoluteFriends.Options;

namespace AbsoluteFriendsExample;

internal class OwnershipHooks : DownpourHooks
{
    private static readonly ConditionalWeakTable<DaddyTentacle, PhysicalObject> _ownedObjects = new();

    protected override Configurable<bool>[] Options => [AddonConfig.HunterDaddyOwnership];

    private static bool IsHeldByHunterDaddy(PhysicalObject physicalObject, DaddyTentacle daddyTentacle)
    {
        foreach (var creature in physicalObject.room?.Creatures ?? [])
        {
            if (creature is not DaddyLongLegs daddy || !daddy.abstractCreature.IsHunterDaddy)
            {
                continue;
            }

            foreach (var tentacle in daddy.tentacles)
            {
                if (tentacle != daddyTentacle && tentacle.grabChunk?.owner == physicalObject)
                {
                    return true;
                }
            }
        }

        return false;
    }

    [HookPatch(typeof(On.DaddyTentacle), nameof(On.DaddyTentacle.Update))]
    private static void On_DaddyTentacle_Update(On.DaddyTentacle.orig_Update orig, DaddyTentacle self)
    {
        orig(self);

        _ownedObjects.TryGetValue(self, out PhysicalObject previous);

        PhysicalObject? current = AddonConfig.HunterDaddyOwnership.IsActive && self.daddy.abstractCreature.IsHunterDaddy
            ? self.grabChunk?.owner
            : null;

        if (previous != null && previous != current && !IsHeldByHunterDaddy(previous, self))
        {
            previous.SetOwner();
            _ownedObjects.Remove(self);
        }

        if (current == null || current == previous)
        {
            return;
        }

        // In Rain Meadow, the Meadow addon sends each object's owner from the game that controls the object, so no extra work is needed here.
        current.SetOwner(self.daddy);
        _ownedObjects.Remove(self);
        _ownedObjects.Add(self, current);
    }
}
