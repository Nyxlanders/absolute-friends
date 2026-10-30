using System.Runtime.CompilerServices;
using AbsoluteFriends.Diagnostics;
using AbsoluteFriends.Options;
using AbsoluteFriends.Utils;
using RWCustom;
using UnityEngine;

namespace AbsoluteFriends.Core;

public static partial class FriendUtils
{
    private static readonly AddonLogger _logger = Reporter.GetLogger(Plugin.Name);

    private static readonly List<AbstractCreature> _noSlugcats = [];

    private static readonly Dictionary<AbstractCreature, List<AbstractCreature>> _chainingSlugcats = [];

    private static readonly Dictionary<(AbstractCreature, AbstractCreature, bool), FriendshipType?> _directFriendships = [];

    private static readonly ConditionalWeakTable<RelationshipTracker.DynamicRelationship, StrongBox<CreatureTemplate.Relationship>> _vanillaRelationships = new();

    private static RainWorldGame? _cacheGame;

    private static int _cacheClock = -1;

    private static int _cacheFrame = -1;

    private static bool UseTickCache()
    {
        if (RainWorldUtils.CurrentGame is not { } game)
        {
            return false;
        }

        int frame = game.GamePaused ? Time.frameCount : -1;

        if (_cacheGame != game || _cacheClock != game.clock || _cacheFrame != frame)
        {
            _chainingSlugcats.Clear();
            _directFriendships.Clear();
            _controllers.Clear();
            _controllersFound = false;
            _cacheGame = game;
            _cacheClock = game.clock;
            _cacheFrame = frame;
        }

        return true;
    }

    private static bool IsLiked(float like, float tempLike) => like > FriendLikeThreshold && (tempLike == 0f ? like : tempLike) > FriendLikeThreshold;

    private static bool TryGetLikes(SocialMemory socialMemory, EntityID subjectID, out float like, out float tempLike)
    {
        like = socialMemory.GetLike(subjectID);
        tempLike = socialMemory.GetTempLike(subjectID);

        if (tempLike == 0f)
        {
            tempLike = like;
        }

        return IsLiked(like, tempLike);
    }

    extension(AbstractCreature? source)
    {
        private bool IsFriendlyLizard =>
            ModManager.CoopAvailable
            && Custom.rainWorld?.options?.friendlyLizards == true
            && source?.creatureTemplate?.IsLizard == true
            && source.abstractAI?.RealAI?.friendTracker is { } tracker
            && tracker.HasSlugcatFriend
            && tracker.friendRel is { like: > FriendLikeThreshold, tempLike: > FriendLikeThreshold };

        private bool? SlugcatFriendshipVerdict
        {
            get
            {
                if (_friendshipRules.Count == 0)
                {
                    return null;
                }

                bool unresolved = false;
                bool? ruled = null;

                foreach (var abstractSlugcat in KnownSlugcats)
                {
                    if (source.GetFriendshipVerdict(abstractSlugcat) is not { } verdict)
                    {
                        unresolved = true;

                        continue;
                    }

                    if (verdict)
                    {
                        return true;
                    }

                    ruled = false;
                }

                return unresolved ? null : ruled;
            }
        }

        private List<AbstractCreature> ChainingSlugcats
        {
            get
            {
                if (source == null)
                {
                    return _noSlugcats;
                }

                if (!UseTickCache())
                {
                    return source.FindChainingSlugcats();
                }

                if (!_chainingSlugcats.TryGetValue(source, out List<AbstractCreature> slugcats))
                {
                    slugcats = source.FindChainingSlugcats();
                    _chainingSlugcats[source] = slugcats;
                }

                return slugcats;
            }
        }

        public bool Like(AbstractCreature? target)
        {
            if (source?.state?.socialMemory is not { } socialMemory || target == null)
            {
                return false;
            }

            return TryGetLikes(socialMemory, target.ID, out _, out _);
        }

        private float? GetReputation(AbstractCreature? target)
        {
            if (
                source?.creatureTemplate?.communityID is not { } communityID
                || communityID == CreatureCommunities.CommunityID.None
                || target?.state is not PlayerState playerState
                || source.world?.game?.session?.creatureCommunities is not { } communities
            )
            {
                return null;
            }

            return communities.LikeOfPlayer(communityID, source.world.RegionNumber, playerState.playerNumber);
        }

        private bool? GetFriendshipVerdict(AbstractCreature abstractSlugcat)
        {
            if (source is not { } subject)
            {
                return null;
            }

            bool? granted = null;

            for (int index = 0; index < _friendshipRules.Count; index++)
            {
                try
                {
                    if (_friendshipRules[index](subject, abstractSlugcat) is not { } verdict)
                    {
                        continue;
                    }

                    if (!verdict)
                    {
                        return false;
                    }

                    granted = true;
                }
                catch (Exception exception)
                {
                    _logger.LogError("A friendship rule threw an exception and was disabled", exception);

                    _friendshipRules.RemoveAt(index--);
                }
            }

            return granted;
        }

        private bool? GetCreatureFriendship(AbstractCreature abstractSlugcat, bool allowNeutral = true) =>
            source.GetCreatureFriendshipType(abstractSlugcat, allowNeutral) is { } type ? type != FriendshipType.None : null;

        private FriendshipType? GetCreatureFriendshipType(AbstractCreature abstractSlugcat, bool allowNeutral = true)
        {
            if (source.GetFriendshipVerdict(abstractSlugcat) is { } ruled)
            {
                return ruled ? FriendshipType.Friendly : FriendshipType.None;
            }

            if (!Config.FriendChaining.IsActive && !source.IsSlugcat && !source.IsFriendlyAllowed)
            {
                return FriendshipType.None;
            }

            FriendshipType? direct = source.GetDirectFriendshipType(abstractSlugcat, allowNeutral);

            if (direct is FriendshipType.Friendly or FriendshipType.Neutral)
            {
                return direct;
            }

            if (!Config.FriendSharing.IsActive)
            {
                return direct;
            }

            foreach (var sharedSlugcat in KnownSlugcats)
            {
                if (sharedSlugcat == abstractSlugcat || !CanShare(abstractSlugcat, sharedSlugcat))
                {
                    continue;
                }

                bool accepted = source.GetFriendshipVerdict(sharedSlugcat)
                    ?? (source.GetDirectFriendshipType(sharedSlugcat, allowNeutral) is FriendshipType.Friendly or FriendshipType.Neutral);

                if (accepted)
                {
                    return FriendshipType.Indirect;
                }
            }

            return direct;
        }

        private FriendshipType? GetDirectFriendshipType(AbstractCreature abstractSlugcat, bool allowNeutral = true)
        {
            if (source == null || !UseTickCache())
            {
                return source.FindDirectFriendshipType(abstractSlugcat, allowNeutral);
            }

            var key = (source, abstractSlugcat, allowNeutral);

            if (!_directFriendships.TryGetValue(key, out FriendshipType? type))
            {
                type = source.FindDirectFriendshipType(abstractSlugcat, allowNeutral);
                _directFriendships[key] = type;
            }

            return type;
        }

        private FriendshipType? FindDirectFriendshipType(AbstractCreature abstractSlugcat, bool allowNeutral)
        {
            if (source.IsSlugcat)
            {
                return FriendshipType.Friendly;
            }

            ArtificialIntelligence? aiSource = source?.abstractAI?.RealAI;

            if (aiSource?.friendTracker != null && source.IsFriendlyAllowed && (source.IsFriendlyLizard || source.Like(abstractSlugcat)))
            {
                return FriendshipType.Friendly;
            }

            if (aiSource?.tracker == null)
            {
                return null;
            }

            CreatureTemplate.Relationship relationship = aiSource.tracker.RepresentationForCreature(abstractSlugcat, false)?.dynamicRelationship is { } dynamicRelationship
                && _vanillaRelationships.TryGetValue(dynamicRelationship, out StrongBox<CreatureTemplate.Relationship> vanillaRelationship)
                ? vanillaRelationship.Value
                : aiSource.DynamicRelationship(abstractSlugcat);

            if (relationship.type == CreatureTemplate.Relationship.Type.Pack)
            {
                return source.IsFriendlyAllowed ? FriendshipType.Friendly : null;
            }

            return allowNeutral
                && source.IsNeutralAllowed
                && relationship.type == CreatureTemplate.Relationship.Type.Ignores
                && source.GetReputation(abstractSlugcat) is not < FriendReputationThreshold ? FriendshipType.Neutral : null;
        }

        private List<AbstractCreature> FindChainingSlugcats()
        {
            List<AbstractCreature>? slugcats = null;

            foreach (var abstractSlugcat in KnownSlugcats)
            {
                if (source.IsFriend(abstractSlugcat, true, false))
                {
                    (slugcats ??= []).Add(abstractSlugcat);
                }
            }

            return slugcats ?? _noSlugcats;
        }

        private bool IsChainedFriend(AbstractCreature target)
        {
            List<AbstractCreature> sourceSlugcats = source.ChainingSlugcats;

            if (sourceSlugcats.Count == 0)
            {
                return false;
            }

            List<AbstractCreature> targetSlugcats = target.ChainingSlugcats;

            return targetSlugcats.Count > 0 && sourceSlugcats.Any(targetSlugcats.Contains);
        }

        private FriendshipType GetFriendshipType(AbstractCreature? target, bool chaining)
        {
            if (source.Controller is { } sourceController)
            {
                return sourceController.GetFriendshipType(target, chaining);
            }

            if (target.Controller is { } targetController)
            {
                return source.GetFriendshipType(targetController, chaining);
            }

            if (
                source == null
                || target == null
                || source == target
                || !IsFriendSession
                || source.IsDenied
                || target.IsDenied
            )
            {
                return FriendshipType.None;
            }

            AbstractCreature? abstractSlugcat = null;
            AbstractCreature? abstractCreature = null;

            if (source.IsSlugcat)
            {
                abstractSlugcat = source;
                abstractCreature = target;
            }
            else if (target.IsSlugcat)
            {
                abstractSlugcat = target;
                abstractCreature = source;
            }

            if (abstractSlugcat != null && abstractCreature != null)
            {
                if (!Config.FriendSlugcat.IsActive && abstractCreature.IsSlugcat)
                {
                    return FriendshipType.None;
                }

                if (abstractCreature.GetCreatureFriendshipType(abstractSlugcat) is { } type)
                {
                    return type;
                }
            }

            return Config.FriendChaining.IsActive
                && chaining
                && !source.IsPlayer
                && !target.IsPlayer
                && (Config.FriendSlugcat.IsActive || (!source.IsSlugcat && !target.IsSlugcat))
                && source.IsChainedFriend(target) ? FriendshipType.Indirect : FriendshipType.None;
        }
    }

    extension(RelationshipTracker.DynamicRelationship dynamicRelationship)
    {
        internal void RecordVanillaRelationship(CreatureTemplate.Relationship relationship) =>
            _vanillaRelationships.GetValue(dynamicRelationship, _ => new()).Value = relationship;
    }
}
