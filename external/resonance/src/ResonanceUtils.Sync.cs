using System.Globalization;
using AbsoluteFriends.Utils;

namespace AbsoluteFriends.Resonance;

internal static partial class ResonanceUtils
{
    private const string ApplyChannel = Plugin.GUID + ".apply";

    private const string EchoChannel = Plugin.GUID + ".echo";

    private const string ReleaseChannel = Plugin.GUID + ".release";

    private const int WarpTimeout = 10 * RainWorldUtils.Second;

    private static readonly List<PendingWarp> _pendingWarps = [];

    public static void RegisterRemoteHandlers()
    {
        AuthorityUtils.RegisterRemoteHandler(ApplyChannel, ReceiveApply);
        AuthorityUtils.RegisterRemoteHandler(EchoChannel, ReceiveEcho);
        AuthorityUtils.RegisterRemoteHandler(ReleaseChannel, ReceiveRelease);
    }

    public static void UnregisterRemoteHandlers()
    {
        AuthorityUtils.UnregisterRemoteHandler(ApplyChannel, ReceiveApply);
        AuthorityUtils.UnregisterRemoteHandler(EchoChannel, ReceiveEcho);
        AuthorityUtils.UnregisterRemoteHandler(ReleaseChannel, ReceiveRelease);
        _pendingWarps.Clear();
    }

    public static void UpdatePendingWarps(RainWorldGame game)
    {
        if (game.world is not { } world)
        {
            return;
        }

        for (int i = _pendingWarps.Count - 1; i >= 0; i--)
        {
            PendingWarp pendingWarp = _pendingWarps[i];

            if (++pendingWarp.Age > WarpTimeout || pendingWarp.Creature.world?.game != game || !world.IsRoomInRegion(pendingWarp.Destination.room))
            {
                _pendingWarps.RemoveAt(i);

                continue;
            }

            if (world.GetAbstractRoom(pendingWarp.Destination).realizedRoom is { fullyLoaded: true, shortCutsReady: true })
            {
                _pendingWarps.RemoveAt(i);
                pendingWarp.Creature.Warp(world, pendingWarp.Destination);
            }
        }
    }

    private static string Encode(params object?[] values)
    {
        return string.Join("|", values.Select(value => Convert.ToString(value, CultureInfo.InvariantCulture)));
    }

    private static int ParseInt(string value) => int.Parse(value, CultureInfo.InvariantCulture);

    private static float ParseFloat(string value) => float.Parse(value, CultureInfo.InvariantCulture);

    private static void ReceiveApply(AbstractPhysicalObject? abstractPhysicalObject, string data)
    {
        if (abstractPhysicalObject is not AbstractCreature abstractCreature || data.Split('|') is not { Length: 13 } parts)
        {
            return;
        }

        WorldCoordinate destination = new(ParseInt(parts[1]), ParseInt(parts[2]), ParseInt(parts[3]), ParseInt(parts[4]));
        ResonanceProfile profile = new(ParseFloat(parts[7]), ParseFloat(parts[8]), ParseFloat(parts[9]), bool.Parse(parts[10]));

        if (bool.Parse(parts[6]))
        {
            abstractCreature.Mend(profile);
        }

        if (bool.Parse(parts[5]) && abstractCreature.world?.game?.world is { } world && world.name == parts[0])
        {
            abstractCreature.WarpWhenReady(world, destination);
        }

        abstractCreature.Reflect(ParseInt(parts[11]), ParseInt(parts[12]));
    }

    private static void ReceiveEcho(AbstractPhysicalObject? abstractPhysicalObject, string data)
    {
        if (
            data.Split('|') is not { Length: 4 } parts
            || RainWorldUtils.CurrentGame is not { world: { } world }
            || world.name != parts[0]
            || ParseInt(parts[1]) is var index && !world.IsRoomInRegion(index)
            || world.GetAbstractRoom(index).realizedRoom is not { } room
        )
        {
            return;
        }

        HashSet<string> keys = [.. parts[3].Split([','], StringSplitOptions.RemoveEmptyEntries)];

        room.ShowEcho((ResonanceType)ParseInt(parts[2]), [.. room.abstractRoom.creatures.Where(abstractCreature => keys.Contains(abstractCreature.SharedKey))]);
    }

    private static void ReceiveRelease(AbstractPhysicalObject? abstractPhysicalObject, string data)
    {
        if (RainWorldUtils.CurrentGame?.world?.abstractRooms is not { } abstractRooms)
        {
            return;
        }

        foreach (var abstractRoom in abstractRooms)
        {
            if (abstractRoom?.creatures.FirstOrDefault(abstractCreature => abstractCreature.SharedKey == data) is { } held)
            {
                held.ReleaseFromHolders();

                return;
            }
        }
    }

    private class PendingWarp(AbstractCreature creature, WorldCoordinate destination)
    {
        public readonly AbstractCreature Creature = creature;

        public readonly WorldCoordinate Destination = destination;

        public int Age;
    }

    extension(AbstractCreature abstractCreature)
    {
        private bool SendApply(World world, WorldCoordinate destination, bool canWarp, bool canMend, ResonanceProfile profile, int duration, int cost)
        {
            return abstractCreature.SendToController(ApplyChannel, Encode(
                world?.name,
                destination.room,
                destination.x,
                destination.y,
                destination.abstractNode,
                canWarp,
                canMend,
                profile.Damage,
                profile.PermanentDamage,
                profile.Poison,
                profile.Dead,
                duration,
                cost
            ));
        }

        private void SendRelease() => AuthorityUtils.SendToOthers(ReleaseChannel, abstractCreature.SharedKey);

        private void WarpWhenReady(World world, WorldCoordinate destination)
        {
            if (!world.IsRoomInRegion(destination.room))
            {
                return;
            }

            AbstractRoom abstractRoom = world.GetAbstractRoom(destination);

            if (abstractRoom.realizedRoom is { fullyLoaded: true, shortCutsReady: true })
            {
                abstractCreature.Warp(world, destination);

                return;
            }

            if (abstractRoom.realizedRoom == null)
            {
                world.game.roomRealizer?.RealizeAndTrackRoom(abstractRoom, false);
            }

            _pendingWarps.RemoveAll(pendingWarp => pendingWarp.Creature == abstractCreature);
            _pendingWarps.Add(new PendingWarp(abstractCreature, destination));
        }
    }

    extension(Room room)
    {
        private void SendEcho(ResonanceType type, IEnumerable<AbstractCreature> resonated)
        {
            AuthorityUtils.SendToOthers(EchoChannel, Encode(
                room.world?.name,
                room.abstractRoom.index,
                (int)type,
                string.Join(",", resonated.Select(abstractCreature => abstractCreature.SharedKey))
            ));
        }
    }
}
