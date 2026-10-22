using System.Runtime.CompilerServices;
using AbsoluteFriends.Utils;

namespace AbsoluteFriendsExample;

// AuthorityUtils lets an addon work in Rain Meadow without referencing Rain Meadow.
// Each game controls only some objects in Rain Meadow, and only that game's changes to an object last; the others show a copy.
// Outside Rain Meadow every object is controlled here, and these calls do nothing extra.
internal static class HunterSync
{
    // Prefix channels and keys with your addon's ID so they do not collide with other addons.
    private const string LetGoChannel = Plugin.GUID + ".letGo";

    private const string HeldKey = Plugin.GUID + ".held";

    private static readonly ConditionalWeakTable<AbstractPhysicalObject, StrongBox<int>> _heldCounts = new();

    internal static void Register()
    {
        // A remote handler runs when another game sends a message on this channel.
        // SendToController passes the receiving game's own copy of the object; SendToOthers passes null and reaches every other game.
        AuthorityUtils.RegisterRemoteHandler(LetGoChannel, ReceiveLetGo);

        // A synced value is read in the game that controls an object and written into every other game's copy.
        // Return null from read to send nothing for an object. Rain Meadow resends a value only when it changes.
        AuthorityUtils.RegisterSyncedValue(HeldKey, ReadHeld, WriteHeld);
    }

    internal static void Unregister()
    {
        AuthorityUtils.UnregisterRemoteHandler(LetGoChannel, ReceiveLetGo);
        AuthorityUtils.UnregisterSyncedValue(HeldKey);
    }

    // Change an object only in the game that controls it. Otherwise ask that game to do it.
    // SendToController returns false when no other game controls the object or none can be reached.
    internal static void LetGo(AbstractCreature abstractCreature)
    {
        if (abstractCreature.HasAuthority)
        {
            Release(abstractCreature);

            return;
        }

        abstractCreature.SendToController(LetGoChannel, string.Empty);
    }

    // The controlling game counts the tentacles itself; another game reads the synced count.
    internal static int HeldCount(AbstractCreature abstractCreature)
    {
        if (abstractCreature.HasAuthority)
        {
            return CountHeld(abstractCreature);
        }

        return _heldCounts.TryGetValue(abstractCreature, out StrongBox<int> count) ? count.Value : 0;
    }

    private static void ReceiveLetGo(AbstractPhysicalObject? abstractPhysicalObject, string data)
    {
        // The object may have changed or left this game since the message was sent, so check it first.
        if (abstractPhysicalObject is AbstractCreature { IsHunterDaddy: true } abstractCreature)
        {
            Release(abstractCreature);
        }
    }

    private static int? ReadHeld(AbstractPhysicalObject abstractPhysicalObject) => abstractPhysicalObject is AbstractCreature { IsHunterDaddy: true } abstractCreature
        ? CountHeld(abstractCreature)
        : null;

    private static void WriteHeld(AbstractPhysicalObject abstractPhysicalObject, int value) => _heldCounts.GetValue(abstractPhysicalObject, _ => new()).Value = value;

    private static int CountHeld(AbstractCreature abstractCreature) => abstractCreature.realizedCreature is DaddyLongLegs daddy
        ? daddy.tentacles.Count(tentacle => tentacle.grabChunk != null)
        : 0;

    private static void Release(AbstractCreature abstractCreature)
    {
        if (abstractCreature.realizedCreature is not DaddyLongLegs daddy)
        {
            return;
        }

        foreach (var tentacle in daddy.tentacles)
        {
            if (tentacle.grabChunk != null)
            {
                tentacle.SwitchTask(DaddyTentacle.Task.Locomotion);
            }
        }
    }
}
