using System.Runtime.CompilerServices;
using AbsoluteFriends.Core;
using AbsoluteFriends.Options;
using AbsoluteFriends.Utils;

namespace AbsoluteFriends.Pilgrimage;

internal static class GateUtils
{
    private static readonly ConditionalWeakTable<Player, GateIdleState> _idleStates = new();

    internal static void UpdatePlayerIdle(Player player)
    {
        GateIdleState idleState = _idleStates.GetOrCreateValue(player);
        int noInputCounter = player.touchedNoInputCounter;

        if (!player.Consious)
        {
            idleState.WasUnconscious = true;
        }
        else if (idleState.WasUnconscious)
        {
            idleState.WasUnconscious = false;
            idleState.ResetCounter = noInputCounter;
        }
        else if (noInputCounter < idleState.ResetCounter)
        {
            idleState.ResetCounter = 0;
        }
    }

    internal static bool IsAwaitedInShelter(AbstractCreature abstractCreature) => IsLocalPlayersFriend(abstractCreature) && !IsInAnyShelter(abstractCreature);

    private static bool IsLocalPlayersFriend(AbstractCreature abstractCreature) =>
        abstractCreature.world?.game?.AlivePlayers.Any(abstractPlayer => abstractCreature.IsTrackedFor(abstractPlayer)) == true;

    private static bool IsInAnyShelter(AbstractCreature abstractCreature) => abstractCreature.realizedCreature is { room.shelterDoor: { } shelterDoor } creature
        ? shelterDoor.IsInShelterDoor(creature)
        : abstractCreature.Room?.shelter == true;

    private static bool IsIdleForGate(Player player, float seconds)
    {
        return player.dead
            || (player.Consious
                && _idleStates.TryGetValue(player, out GateIdleState idleState)
                && !idleState.WasUnconscious
                && player.touchedNoInputCounter - idleState.ResetCounter > seconds * RainWorldUtils.Second);
    }

    private class GateIdleState
    {
        public bool WasUnconscious;

        public int ResetCounter;
    }

    extension<T>(T gate) where T : UpdatableAndDeletable
    {
        public bool CanActivate(IEnumerable<AbstractCreature>? abstractPlayers, Func<T, Creature, bool> isInGate, Func<AbstractCreature, bool>? isAwaited = null)
        {
            bool isFriendOutside = FriendUtils.TrackedFriends.Any(abstractCreature => (isAwaited == null || isAwaited(abstractCreature))
                && (abstractCreature.realizedCreature is { } creature
                    ? !creature.dead && !isInGate(gate, creature)
                    : abstractCreature.state?.dead != true));

            if (isFriendOutside && !Config.GateForce.IsActive)
            {
                return false;
            }

            float idleTime = Config.GateTime.Value + (isFriendOutside ? Config.GateForceTime.Value : 0f);

            foreach (var abstractPlayer in abstractPlayers ?? [])
            {
                if (abstractPlayer.realizedCreature is not Player player || !player.IsPlayer || (player.dead && Config.Perseverance.IsActive))
                {
                    continue;
                }

                if (!isInGate(gate, player) || (player.onBack == null && !IsIdleForGate(player, idleTime)))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
