using System.Runtime.CompilerServices;
using AbsoluteFriends.Core;
using AbsoluteFriends.Options;
using AbsoluteFriends.Utils;
using UnityEngine;

namespace AbsoluteFriends.Resonance;

internal enum ResonanceType
{
    Automatic,
    Gathered,
    Ungathered
}

internal static partial class ResonanceUtils
{
    private static ConditionalWeakTable<AbstractCreature, ResonanceTracker> _trackers = new();

    private static float CostRatio => Config.ResonanceCost.IsActive ? Config.ResonanceCostRatio.Value : 0f;

    public static void ResetTrackers()
    {
        _trackers = new();
        _pendingWarps.Clear();
    }

    private static bool ShouldVibrate(float progress, int reflection) => progress >= 0.1f || reflection >= 5;

    extension(int cost)
    {
        public float GetProgress(int reflection)
        {
            return cost == 0 ? 1f : Mathf.InverseLerp(0, cost, reflection);
        }
    }

    extension(Player player)
    {
        public bool IsResonating
        {
            get
            {
                Player.InputPackage input = player.input[0];

                return input.x == 0 && input.y == 0 && input.jmp && !input.thrw && !input.pckp && !input.spec;
            }
        }
    }

    extension(Room room)
    {
        private bool IsGathered(List<AbstractCreature> resonators)
        {
            return Config.ResonanceGate.IsActive
                && resonators.Count > 0
                && resonators.All(abstractCreature => abstractCreature.realizedCreature is { } creature && room.IsInGate(creature));
        }

        private List<AbstractCreature> Resonators(Func<Creature, bool> predicate, bool isUngathered = false, bool isLocalOnly = false)
        {
            List<AbstractCreature> abstractPlayers = [.. room.game?.AllPlayers.Where(abstractPlayer => abstractPlayer.state is { dead: false } && (!isLocalOnly || abstractPlayer.HasAuthority)) ?? []];

            if (!Core.Config.FriendSharing.IsActive)
            {
                return [.. abstractPlayers.Where(abstractPlayer => abstractPlayer?.realizedCreature is Player player && predicate(player))];
            }

            if (!isUngathered && abstractPlayers.Any(abstractPlayer => abstractPlayer?.realizedCreature is not Player player || !predicate(player)))
            {
                return [];
            }

            return [.. abstractPlayers];
        }

        private IEnumerable<AbstractCreature> Candidates(List<AbstractCreature> resonators, Func<Creature, bool> predicate, ResonanceType type)
        {
            foreach (var abstractCreature in FriendUtils.TrackedFriendsIncludingPlayers)
            {
                if (!Core.Config.FriendSharing.IsActive && !resonators.Any(abstractPlayer => abstractCreature.IsTrackedFor(abstractPlayer)))
                {
                    continue;
                }

                if (type == ResonanceType.Ungathered && !abstractCreature.IsInRoom(room))
                {
                    continue;
                }

                if (abstractCreature.CanWarpOrMend(predicate, ResonanceProfile.Zero))
                {
                    yield return abstractCreature;
                }
            }
        }

        private List<AbstractCreature> Resonatees(List<AbstractCreature> resonators, Func<Creature, bool> predicate, ResonanceType type, out ResonanceProfile profile)
        {
            List<AbstractCreature> candidates = [.. room.Candidates(resonators, predicate, type)];
            ResonanceProfile baseProfile = candidates.Union(resonators).Profile;

            profile = baseProfile;

            return [.. candidates.Where(abstractCreature => abstractCreature.CanWarpOrMend(predicate, baseProfile))];
        }

        private int GetCost(IEnumerable<AbstractCreature> resonatees, Func<Creature, bool> predicate, ResonanceProfile profile)
        {
            return resonatees.Sum(abstractCreature => abstractCreature.GetCost(room, predicate, profile));
        }

        private void Echo(ResonanceType type, List<AbstractCreature> resonated)
        {
            room.ShowEcho(type, resonated);
            room.SendEcho(type, resonated);
        }

        private void ShowEcho(ResonanceType type, IEnumerable<AbstractCreature> resonated)
        {
            if (Config.ResonanceMelt.IsActive && type != ResonanceType.Ungathered)
            {
                room.AddObject(new ResonanceMelt(room));
            }

            if (Config.ResonanceShockwave.IsActive)
            {
                room.AddObject(new ResonanceShockwave(room, resonated));
            }
        }

        private bool Resonate(List<AbstractCreature> resonators, List<AbstractCreature> resonatees, ResonanceProfile profile, Func<Creature, bool> predicate, ResonanceType type)
        {
            if (
                resonators.Count == 0
                || resonatees.Count == 0
                || !resonators.Lead.HasAuthority
                || resonators.FirstOrDefault(abstractCreature => !abstractCreature.AwaitWarp(predicate)) is not { } origin
            )
            {
                return false;
            }

            List<AbstractCreature> resonatedCreatures = [.. resonatees.Union(resonators)];
            World world = origin.world;
            WorldCoordinate worldCoordinate = origin.pos;
            int cost = room.GetCost(resonatees, predicate, profile);
            int duration = cost / resonators.Count;
            int sharedCost = cost / resonatedCreatures.Count;

            foreach (var abstractCreature in resonatees)
            {
                abstractCreature.Apply(world, worldCoordinate, abstractCreature.CanWarp(predicate, profile), abstractCreature.CanMend(predicate, profile), profile, duration, sharedCost);
            }

            if (type != ResonanceType.Automatic)
            {
                foreach (var abstractPlayer in resonators.Except(resonatees))
                {
                    abstractPlayer.Apply(world, worldCoordinate, false, false, profile, duration, sharedCost);
                }
            }

            room.Echo(type, resonatedCreatures);

            return true;
        }

        private bool Resonate(Func<Creature, bool> predicate, ResonanceType type, bool isUngathered = false, bool isLocalOnly = false)
        {
            List<AbstractCreature> resonators = room.Resonators(predicate, isUngathered, isLocalOnly);

            return room.Resonate(resonators, room.Resonatees(resonators, predicate, type, out ResonanceProfile profile), profile, predicate, type);
        }
    }

    extension<T>(T gate) where T : UpdatableAndDeletable
    {
        public void Resonate(Func<T, Creature, bool> isInGate)
        {
            gate.room?.Resonate(creature => isInGate(gate, creature), ResonanceType.Automatic);
        }

        public void ResonateApart(Func<T, Creature, bool> isInGate)
        {
            gate.room?.Resonate(creature => isInGate(gate, creature), ResonanceType.Automatic, isUngathered: true);
        }

        public void ResonateHere(Func<T, Creature, bool> isInGate)
        {
            gate.room?.Resonate(creature => isInGate(gate, creature), ResonanceType.Automatic, isUngathered: true, isLocalOnly: true);
        }
    }
}
