using AbsoluteFriends.Options;
using AbsoluteFriends.Utils;
using RWCustom;
using UnityEngine;

namespace AbsoluteFriends.Resonance;

internal static partial class ResonanceUtils
{
    extension(AbstractCreature abstractCreature)
    {
        public ResonanceTracker Tracker => _trackers.GetOrCreateValue(abstractCreature);

        private ResonanceProfile Profile => ResonanceProfile.Read(abstractCreature);

        public ResonanceTracker? FindTracker()
        {
            return _trackers.TryGetValue(abstractCreature, out ResonanceTracker tracker) ? tracker : null;
        }

        private bool IsInRoom(Room room)
        {
            return abstractCreature.world == room.world && abstractCreature.Room == room.abstractRoom;
        }

        private bool AwaitWarp(Func<Creature, bool> predicate)
        {
            return abstractCreature.realizedCreature is not { } creature || !predicate(creature);
        }

        private bool AwaitMend(ResonanceProfile profile)
        {
            return abstractCreature.GetMendCost(profile) > 0;
        }

        private bool CanWarp(Func<Creature, bool> predicate, ResonanceProfile profile)
        {
            return Config.ResonanceWarp.IsActive && abstractCreature.AwaitWarp(predicate) && (!abstractCreature.AwaitMend(profile) || Config.ResonanceMend.IsActive);
        }

        private bool CanMend(Func<Creature, bool> predicate, ResonanceProfile profile)
        {
            return Config.ResonanceMend.IsActive && abstractCreature.AwaitMend(profile) && (!abstractCreature.AwaitWarp(predicate) || Config.ResonanceWarp.IsActive);
        }

        private bool CanWarpOrMend(Func<Creature, bool> predicate, ResonanceProfile profile)
        {
            return abstractCreature.CanWarp(predicate, profile) || abstractCreature.CanMend(predicate, profile);
        }

        private void Release(RainWorldGame? game)
        {
            if (abstractCreature.realizedCreature is { } creature)
            {
                creature.LoseAllGrasps();
                creature.AllGraspsLetGoOfThisObject(true);
                creature.ReleaseShortcuts(game);
                creature.CollideWithTerrain = true;
            }

            abstractCreature.LoseAllStuckObjects();
            abstractCreature.InDen = false;
            abstractCreature.remainInDenCounter = 0;
        }

        private void ReleaseFromHolders()
        {
            foreach (var grasp in abstractCreature.realizedCreature?.grabbedBy.ToList() ?? [])
            {
                if (grasp?.grabber is { HasAuthority: true })
                {
                    grasp.Release();
                }
            }

            foreach (var stick in abstractCreature.stuckObjects.ToList())
            {
                if (stick is AbstractPhysicalObject.CreatureGripStick && stick.B == abstractCreature && stick.A.HasAuthority)
                {
                    stick.Deactivate();
                }
            }
        }

        private void Warp(World world, WorldCoordinate worldCoordinate)
        {
            AbstractRoom? abstractRoom = world?.GetAbstractRoom(worldCoordinate);

            if (abstractRoom?.realizedRoom is not Room room)
            {
                return;
            }

            World previousWorld = abstractCreature.world;
            AbstractRoom previousAbstractRoom = abstractCreature.Room;

            if (abstractCreature.realizedCreature is { slatedForDeletetion: true })
            {
                abstractCreature.realizedCreature = null;
            }

            abstractCreature.Release(room.game);

            Creature? creature = abstractCreature.realizedCreature;

            if (creature?.room is { } previousRoom && previousRoom != room)
            {
                previousRoom.RemoveObject(creature);
            }

            previousAbstractRoom?.RemoveEntity(abstractCreature);

            abstractCreature.slatedForDeletion = false;
            abstractCreature.world = world;
            abstractCreature.pos = worldCoordinate;
            abstractCreature.timeSpentHere = 0;

            abstractRoom.AddEntity(abstractCreature);

            if (previousWorld != world && abstractCreature.creatureTemplate?.AI == true)
            {
                abstractCreature.abstractAI?.NewWorld(world);
                abstractCreature.InitiateAI();
            }

            if (creature == null)
            {
                abstractCreature.RealizeInRoom();
                abstractCreature.MoveFollowingCameras(room);
            }
            else
            {
                IntVector2 tile = abstractCreature.pos.Tile;

                creature.ClearShortcut();

                if (creature.room != room)
                {
                    creature.SpitOutOfShortCut(tile, room, true);
                    abstractCreature.MoveFollowingCameras(room);

                    return;
                }

                Vector2 position = room.MiddleOfTile(tile);

                foreach (var bodyChunk in creature.bodyChunks ?? [])
                {
                    bodyChunk.HardSetPosition(position + Custom.RNV());
                    bodyChunk.vel *= 0f;
                }
            }
        }

        private void MoveFollowingCameras(Room room)
        {
            foreach (var camera in room.game?.cameras ?? [])
            {
                if (camera?.followAbstractCreature == abstractCreature && camera.room != room)
                {
                    camera.MoveCamera(room, -1);
                }
            }
        }

        private void Mend(ResonanceProfile profile)
        {
            bool shouldRevive = !profile.Dead;

            abstractCreature.slatedForDeletion = false;

            if (abstractCreature.state is CreatureState creatureState)
            {
                if (shouldRevive)
                {
                    creatureState.alive = true;
                }
                if (creatureState is PlayerState playerState)
                {
                    if (shouldRevive)
                    {
                        playerState.permaDead = false;
                    }
                    playerState.permanentDamageTracking = Mathf.Min((float)playerState.permanentDamageTracking, profile.PermanentDamage);
                }
                if (creatureState is HealthState healthState)
                {
                    healthState.health = Mathf.Max(healthState.health, 1f - profile.Damage);
                }
            }

            if (abstractCreature.realizedCreature is { } creature)
            {
                if (shouldRevive)
                {
                    creature.dead = false;
                    creature.killTag = null;
                    creature.killTagCounter = 0;
                }
                creature.injectedPoison = Mathf.Min(creature.injectedPoison, profile.Poison);
            }
        }

        private void Apply(World world, WorldCoordinate worldCoordinate, bool canWarp, bool canMend, ResonanceProfile profile, int duration, int cost)
        {
            if (!abstractCreature.HasAuthority)
            {
                if (canWarp)
                {
                    abstractCreature.ReleaseFromHolders();
                    abstractCreature.SendRelease();
                }

                if (abstractCreature.SendApply(world, worldCoordinate, canWarp, canMend, profile, duration, cost))
                {
                    abstractCreature.Shake(duration);
                }

                return;
            }

            if (canMend)
            {
                abstractCreature.Mend(profile);
            }
            if (canWarp)
            {
                abstractCreature.Warp(world, worldCoordinate);
            }

            abstractCreature.Reflect(duration, cost);
        }

        private int GetWarpCost(Room room)
        {
            int cost = 0;

            if (abstractCreature.world == null || abstractCreature.world != room.world)
            {
                cost += 10 * 60 * RainWorldUtils.Second;
            }
            else if (abstractCreature.Room?.realizedRoom != room)
            {
                cost += 60 * RainWorldUtils.Second;
            }
            else
            {
                cost += 10 * RainWorldUtils.Second;
            }

            if (abstractCreature.realizedCreature is not { } creature || abstractCreature.slatedForDeletion)
            {
                cost += 60 * RainWorldUtils.Second;
            }
            else if (creature.inShortcut || creature.enteringShortCut != null)
            {
                cost += 10 * RainWorldUtils.Second;
            }

            if (abstractCreature.InDen)
            {
                cost += 60 * RainWorldUtils.Second;
            }
            if (abstractCreature.stuckObjects?.Count > 0)
            {
                cost += abstractCreature.stuckObjects.Count * 10 * RainWorldUtils.Second;
            }

            return (int)(cost * (abstractCreature.creatureTemplate?.bodySize ?? 1f));
        }

        private int GetMendCost(ResonanceProfile profile)
        {
            ResonanceProfile abstractCreatureProfile = abstractCreature.Profile;
            float damage = 0f;
            int cost = 0;

            damage += Mathf.Max(abstractCreatureProfile.Damage - profile.Damage, 0f);
            damage += Mathf.Max(abstractCreatureProfile.PermanentDamage - profile.PermanentDamage, 0f);
            damage += Mathf.Max(abstractCreatureProfile.Poison - profile.Poison, 0f);
            cost += (int)(damage * (abstractCreature.creatureTemplate?.baseDamageResistance ?? 1f) * 60 * RainWorldUtils.Second);
            if (abstractCreatureProfile.Dead && !profile.Dead)
            {
                cost += Mathf.Max(abstractCreature.Tracker.Death, 1);
            }

            return cost;
        }

        private int GetCost(Room room, Func<Creature, bool> predicate, ResonanceProfile profile)
        {
            int cost = 0;

            if (abstractCreature.CanWarp(predicate, profile))
            {
                cost += abstractCreature.GetWarpCost(room);
            }

            if (abstractCreature.CanMend(predicate, profile))
            {
                cost += abstractCreature.GetMendCost(profile);
            }

            return (int)Mathf.Sqrt(cost * CostRatio);
        }

    }

    extension(IEnumerable<AbstractCreature> abstractCreatures)
    {
        private AbstractCreature Lead => abstractCreatures.OrderBy(abstractCreature => abstractCreature.SharedKey, StringComparer.Ordinal).First();

        private ResonanceProfile Profile => abstractCreatures.Select(abstractCreature => abstractCreature.Profile).DefaultIfEmpty(ResonanceProfile.Zero).Aggregate((profile, other) => profile.Min(other));
    }

    extension(Creature creature)
    {
        private void ClearShortcut()
        {
            creature.inShortcut = false;
            creature.inShortcutVessel = null;
            creature.enteringShortCut = null;
        }

        private void ReleaseShortcuts(RainWorldGame? game)
        {
            bool shortcutRemoved = false;

            if (game?.shortcuts is { } shortcutHandler)
            {
                shortcutRemoved |= shortcutHandler.transportVessels?.RemoveAll(vessel => vessel?.creature == creature) > 0;
                shortcutRemoved |= shortcutHandler.betweenRoomsWaitingLobby?.RemoveAll(vessel => vessel?.creature == creature) > 0;
                shortcutRemoved |= shortcutHandler.borderTravelVessels?.RemoveAll(vessel => vessel?.creature == creature) > 0;
            }

            creature.ClearShortcut();

            if (!shortcutRemoved)
            {
                return;
            }

            foreach (var abstractPhysicalObject in creature.abstractCreature?.GetAllConnectedObjects() ?? [])
            {
                if (abstractPhysicalObject?.realizedObject is Creature connectedCreature && connectedCreature != creature)
                {
                    connectedCreature.ClearShortcut();
                }
            }
        }
    }
}
