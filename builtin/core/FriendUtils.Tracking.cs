using System.Runtime.CompilerServices;
using AbsoluteFriends.Options;
using AbsoluteFriends.Utils;

namespace AbsoluteFriends.Core;

public static partial class FriendUtils
{
    private static ConditionalWeakTable<AbstractCreature, HashSet<EntityID>> _trackedPlayerIds = new();

    private static readonly HashSet<AbstractCreature> _trackedSlugpups = [];

    private static readonly HashSet<AbstractCreature> _trackedCreatures = [];

    public static IEnumerable<AbstractCreature> TrackedFriends
    {
        get
        {
            if (!IsFriendSession)
            {
                yield break;
            }

            foreach (var abstractCreature in _trackedCreatures)
            {
                if (abstractCreature.IsTracked)
                {
                    yield return abstractCreature;
                }
            }
        }
    }

    public static IEnumerable<AbstractCreature> TrackedFriendsIncludingPlayers
    {
        get
        {
            if (IsFriendSession && Config.FriendSlugcat.IsActive)
            {
                foreach (var abstractPlayer in Players)
                {
                    if (abstractPlayer.IsTracked)
                    {
                        yield return abstractPlayer;
                    }
                }
            }

            foreach (var abstractCreature in TrackedFriends)
            {
                yield return abstractCreature;
            }
        }
    }

    public static IEnumerable<AbstractCreature> KnownSlugcats
    {
        get
        {
            if (!IsFriendSession)
            {
                yield break;
            }

            foreach (var abstractPlayer in Players)
            {
                yield return abstractPlayer;
            }

            foreach (var abstractSlugpup in _trackedSlugpups)
            {
                yield return abstractSlugpup;
            }
        }
    }

    public static IEnumerable<AbstractCreature> TrackingCandidates => Players
        .Concat(_trackedCreatures)
        .Distinct()
        .Where(creature => (!creature.slatedForDeletion || creature.state?.dead == true) && creature.IsTrackingAllowed);

    internal static bool HasMultipleKnownSlugcats => Players.Count + _trackedSlugpups.Count > 1;

    private static IReadOnlyList<AbstractCreature> Players => RainWorldUtils.CurrentGame.AllPlayers;

    internal static void ClearTrackedFriends()
    {
        _trackedSlugpups.Clear();
        _trackedCreatures.Clear();
        _chainingSlugcats.Clear();
        _directFriendships.Clear();
        _cacheGame = null;
        _cacheClock = -1;
        _cacheFrame = -1;
        _trackedPlayerIds = new();
    }

    internal static void PruneTrackedFriends()
    {
        RainWorldGame? game = RainWorldUtils.CurrentGame;

        bool IsStale(AbstractCreature abstractCreature) =>
            (abstractCreature.slatedForDeletion && abstractCreature.state?.dead != true)
            || abstractCreature.world != game?.world;

        _trackedCreatures.RemoveWhere(IsStale);
        _trackedSlugpups.RemoveWhere(abstractCreature => !_trackedCreatures.Contains(abstractCreature));
    }

    extension(AbstractCreature? source)
    {
        public bool IsTracked => source != null && (_trackedCreatures.Contains(source) || Players.Contains(source)) && source.IsTrackingAllowed;

        internal bool IsCloseFriendOfPlayer => IsFriendSession
            && source is { } abstractCreature
            && !abstractCreature.IsDenied
            && Players.Any(abstractPlayer => abstractCreature.IsCloseFriendOf(abstractPlayer));

        internal AbstractCreature? FavoredPlayer => Players.FirstOrDefault(abstractPlayer =>
            abstractPlayer is { slatedForDeletion: false, state.dead: false }
            && source?.state?.socialMemory?.GetRelationship(abstractPlayer.ID) is { } relationship
            && IsLiked(relationship.like, relationship.tempLike));

        private bool IsTrackingAllowed
        {
            get
            {
                if (!IsFriendSession || source == null)
                {
                    return false;
                }

                if (source.IsSlugcat)
                {
                    return Config.FriendSlugcat.IsActive && source.SlugcatFriendshipVerdict != false;
                }

                return !source.IsDenied
                    && (source.SlugcatFriendshipVerdict ?? source.IsFriendlyAllowed);
            }
        }

        public void Untrack()
        {
            if (source is { } abstractCreature)
            {
                _trackedSlugpups.Remove(abstractCreature);
                _trackedCreatures.Remove(abstractCreature);
                _trackedPlayerIds.Remove(abstractCreature);
            }
        }

        public bool IsTrackedFor(AbstractCreature? abstractPlayer)
        {
            if (source is not { IsTracked: true } || abstractPlayer == null)
            {
                return false;
            }

            if (source.IsFriend(abstractPlayer))
            {
                return true;
            }

            return source.abstractAI?.RealAI == null
                && source.RememberedFriendshipWith(abstractPlayer) == true;
        }

        public void Track()
        {
            if (source is not { } abstractCreature || abstractCreature is AbstractOwner)
            {
                return;
            }

            if (abstractCreature.IsSlugpup)
            {
                _trackedSlugpups.Add(abstractCreature);
            }

            _trackedCreatures.Add(abstractCreature);

            if (!abstractCreature.IsSlugcat && abstractCreature.abstractAI?.RealAI == null)
            {
                return;
            }

            HashSet<EntityID> playerIds = _trackedPlayerIds.GetOrCreateValue(abstractCreature);

            playerIds.Clear();

            foreach (var abstractPlayer in Players)
            {
                if (abstractCreature.IsFriend(abstractPlayer))
                {
                    playerIds.Add(abstractPlayer.ID);
                }
            }
        }

        private bool? RememberedFriendshipWith(AbstractCreature target)
        {
            if (source == null)
            {
                return null;
            }

            if (source.IsDenied || target.IsDenied || source.GetFriendshipVerdict(target) == false)
            {
                return false;
            }

            return _trackedPlayerIds.TryGetValue(source, out HashSet<EntityID> playerIds)
                ? playerIds.Contains(target.ID)
                : null;
        }

        private bool IsCloseFriendOf(AbstractCreature abstractSlugcat) => !abstractSlugcat.IsDenied
            && source.GetCreatureFriendship(abstractSlugcat, allowNeutral: false) == true;
    }

    extension(EntityID id)
    {
        public bool IsKnownSlugcat
        {
            get
            {
                if (!IsFriendSession)
                {
                    return false;
                }

                foreach (var abstractPlayer in Players)
                {
                    if (abstractPlayer.ID == id)
                    {
                        return true;
                    }
                }

                foreach (var abstractSlugpup in _trackedSlugpups)
                {
                    if (abstractSlugpup.ID == id)
                    {
                        return true;
                    }
                }

                return false;
            }
        }
    }
}
