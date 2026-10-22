using AbsoluteFriends.Core;
using AbsoluteFriends.Options;
using AbsoluteFriends.Utils;

namespace AbsoluteFriends.Resonance;

internal static partial class ResonanceUtils
{
    private static readonly Func<Creature, bool> _isHeld = _ => true;

    extension(ResonanceAnchor resonanceAnchor)
    {
        public bool ResonateRoom()
        {
            Room room = resonanceAnchor.room;
            Func<Creature, bool> predicate = resonanceAnchor.Predicate;
            List<AbstractCreature> resonators = room.Resonators(predicate);

            if (!Core.Config.FriendSharing.IsActive)
            {
                resonators.RemoveAll(abstractPlayer => abstractPlayer.realizedCreature is not Player player || !player.IsResonating);
            }

            ResonanceType type = room.IsGathered(resonators) ? ResonanceType.Gathered : ResonanceType.Ungathered;
            List<AbstractCreature> resonatees = room.Resonatees(resonators, predicate, type, out ResonanceProfile profile);

            if (resonators.Count == 0 || resonatees.Count == 0)
            {
                resonanceAnchor.Players.Clear();
                resonanceAnchor.Reflection = 0;

                return false;
            }

            HashSet<Player> players = [.. resonators.Select(abstractPlayer => abstractPlayer.realizedCreature).OfType<Player>()];

            if (!resonanceAnchor.Players.SetEquals(players) || !players.All(player => player.IsResonating))
            {
                resonanceAnchor.Players = players;
                resonanceAnchor.Reflection = 0;

                return false;
            }

            int cost = room.GetCost(resonatees, predicate, profile);
            float progress = cost.GetProgress(++resonanceAnchor.Reflection * players.Count);

            if (progress >= 1f)
            {
                resonanceAnchor.Reflection = 0;

                return room.Resonate(resonators, resonatees, profile, predicate, type);
            }

            if (ShouldVibrate(progress, resonanceAnchor.Reflection))
            {
                foreach (var player in players)
                {
                    player.Vibrate(progress);
                }
            }

            return false;
        }

        public bool ResonateGrab()
        {
            Room room = resonanceAnchor.room;
            Dictionary<AbstractCreature, int> grasps = resonanceAnchor.Grasps;
            HashSet<AbstractCreature> held = [];

            foreach (var abstractPlayer in room.game?.AllPlayers.Where(abstractPlayer => abstractPlayer.state is { dead: false }) ?? [])
            {
                if (abstractPlayer?.realizedCreature is not Player player || player.room != room || !player.IsResonating)
                {
                    continue;
                }

                foreach (var grabbed in player.HeldObjects)
                {
                    if (
                        grabbed.abstractPhysicalObject is not AbstractCreature abstractCreature
                        || !(Core.Config.FriendSharing.IsActive ? abstractCreature.IsTracked : abstractCreature.IsTrackedFor(abstractPlayer))
                        || !abstractCreature.IsInRoom(room)
                        || !held.Add(abstractCreature))
                    {
                        continue;
                    }

                    ResonanceProfile profile = abstractPlayer.Profile.Min(abstractCreature.Profile);

                    if (!abstractCreature.CanMend(_isHeld, profile))
                    {
                        continue;
                    }

                    int reflection = (grasps.TryGetValue(abstractCreature, out int value) ? value : 0) + 1;
                    float progress = abstractCreature.GetCost(room, _isHeld, profile).GetProgress(reflection);

                    grasps[abstractCreature] = reflection;

                    if (progress >= 1f)
                    {
                        grasps.Remove(abstractCreature);

                        if (room.Resonate([abstractPlayer], [abstractCreature], profile, _isHeld, ResonanceType.Ungathered))
                        {
                            return true;
                        }
                    }
                    else if (ShouldVibrate(progress, reflection))
                    {
                        player.Vibrate(progress);

                        abstractCreature.realizedCreature?.Vibrate(progress);
                    }
                }
            }

            return false;
        }
    }
}
