using AbsoluteFriends.Utils;
using UnityEngine;

namespace AbsoluteFriends.Core;

public static partial class FriendUtils
{
    private const float ControllerReach = 20f;

    private static readonly Dictionary<AbstractCreature, AbstractCreature> _controllers = [];

    private static readonly Dictionary<AbstractCreature, AbstractCreature> _noControllers = [];

    private static bool _controllersFound;

    private static Dictionary<AbstractCreature, AbstractCreature> Controllers
    {
        get
        {
            if (!UseTickCache())
            {
                return _noControllers;
            }

            if (!_controllersFound)
            {
                _controllersFound = true;

                FindControllers();
            }

            return _controllers;
        }
    }

    private static void FindControllers()
    {
        foreach (var abstractPlayer in Players)
        {
            if (abstractPlayer.realizedCreature is not Player { room: { } room } player || player.bodyChunks.Any(bodyChunk => bodyChunk.collideWithObjects))
            {
                continue;
            }

            if ((abstractPlayer.StuckControlled ?? player.NearestControlled(room)) is { } abstractCreature && !_controllers.ContainsKey(abstractCreature))
            {
                _controllers[abstractCreature] = abstractPlayer;
            }
        }
    }

    extension(AbstractCreature? source)
    {
        internal AbstractCreature? Controller => source is { controlled: true } abstractCreature
            && Controllers.TryGetValue(abstractCreature, out AbstractCreature controller) ? controller : null;

        private AbstractCreature? StuckControlled
        {
            get
            {
                foreach (var stick in source?.stuckObjects ?? [])
                {
                    if (
                        stick.GetType().Assembly != typeof(AbstractPhysicalObject).Assembly
                        && (stick.A == source ? stick.B : stick.A) is AbstractCreature { controlled: true } abstractCreature
                        && !abstractCreature.IsSlugcat
                    )
                    {
                        return abstractCreature;
                    }
                }

                return null;
            }
        }
    }

    extension(Player player)
    {
        private AbstractCreature? NearestControlled(Room room)
        {
            AbstractCreature? nearest = null;
            float nearestDistance = ControllerReach;

            foreach (var creature in room.Creatures)
            {
                if (creature == player || creature.abstractCreature is not { controlled: true } abstractCreature || abstractCreature.IsSlugcat)
                {
                    continue;
                }

                foreach (var bodyChunk in creature.bodyChunks)
                {
                    float distance = Vector2.Distance(bodyChunk.pos, player.mainBodyChunk.pos);

                    if (distance <= nearestDistance)
                    {
                        nearest = abstractCreature;
                        nearestDistance = distance;
                    }
                }
            }

            return nearest;
        }
    }
}
