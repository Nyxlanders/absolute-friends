using AbsoluteFriends.Options;
using AbsoluteFriends.Utils;
using RWCustom;

namespace AbsoluteFriends.Core;

public static partial class FriendUtils
{
    public const float FriendLikeThreshold = 0.5f;

    public const float FriendReputationThreshold = 0f;

    private static readonly List<Func<AbstractCreature, AbstractCreature, bool?>> _friendshipRules = [];

    private static readonly List<Func<AbstractCreature, AbstractCreature, bool>> _sharingRules = [];

    public static bool IsFriendSession
    {
        get
        {
            if (RainWorldUtils.CurrentGame is not { } game)
            {
                return false;
            }

            if (Custom.rainWorld?.safariMode == true)
            {
                return Config.FriendSafari.IsActive;
            }

            if (game.IsArenaSession)
            {
                return Config.FriendArena.IsActive;
            }

            if (!game.IsStorySession)
            {
                return false;
            }

            return Custom.rainWorld?.ExpeditionMode == true ? Config.FriendExpedition.IsActive : Config.FriendStory.IsActive;
        }
    }

    internal static bool HasSharingRules => _sharingRules.Count > 0;

    public static void RegisterFriendshipRule(Func<AbstractCreature, AbstractCreature, bool?> rule)
    {
        if (rule != null && !_friendshipRules.Contains(rule))
        {
            _friendshipRules.Add(rule);
        }
    }

    public static void UnregisterFriendshipRule(Func<AbstractCreature, AbstractCreature, bool?> rule) => _friendshipRules.Remove(rule);

    public static void RegisterSharingRule(Func<AbstractCreature, AbstractCreature, bool> rule)
    {
        if (rule != null && !_sharingRules.Contains(rule))
        {
            _sharingRules.Add(rule);
        }
    }

    public static void UnregisterSharingRule(Func<AbstractCreature, AbstractCreature, bool> rule) => _sharingRules.Remove(rule);

    internal static bool CanShare(AbstractCreature abstractSlugcat, AbstractCreature sharedSlugcat) => _sharingRules.All(rule => rule(abstractSlugcat, sharedSlugcat));

    private static FriendshipType ClosestFriendship(AbstractPhysicalObject? source, IEnumerable<AbstractCreature> players)
    {
        FriendshipType result = FriendshipType.None;

        foreach (var abstractPlayer in players)
        {
            FriendshipType type = source.FriendshipWith(abstractPlayer);

            if (type > result)
            {
                result = type;
            }
        }

        return result;
    }

    extension(AbstractPhysicalObject? source)
    {
        public bool IsTracked => source is AbstractCreature { IsTracked: true };

        public bool IsFriendOfPlayer
        {
            get
            {
                foreach (var abstractPlayer in (source?.world?.game).AllPlayers)
                {
                    if (source.IsFriend(abstractPlayer))
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public FriendshipType FriendshipOfPlayer => ClosestFriendship(source, (source?.world?.game).AllPlayers);

        public FriendshipType FriendshipOfLocalPlayer => ClosestFriendship(source, (source?.world?.game).AllPlayers.Where(abstractPlayer => abstractPlayer.HasAuthority));

        public FriendshipType FriendshipWith(AbstractPhysicalObject? target, bool direct = false)
        {
            FriendshipType type = source.GetFriendshipType(target, direct, true);

            return type != FriendshipType.None && (source.IsTrackedWith(target) || target.IsTrackedWith(source)) ? FriendshipType.Tracked : type;
        }

        public bool IsFriend(AbstractPhysicalObject? target, bool direct = false) => source.IsFriend(target, direct, true);

        public bool IsFriend(PhysicalObject? target, bool direct = false) => source.IsFriend(target?.abstractPhysicalObject, direct);

        private bool IsFriend(AbstractPhysicalObject? target, bool direct, bool chaining) =>
            source.GetFriendshipType(target, direct, chaining) != FriendshipType.None;

        private bool IsTrackedWith(AbstractPhysicalObject? target) => ((target as AbstractCreature)?.Controller ?? target) is AbstractCreature { IsPlayer: true } abstractPlayer && source is AbstractCreature { IsPlayer: false } abstractCreature && abstractCreature.IsTrackedFor(abstractPlayer);

        private FriendshipType GetFriendshipType(AbstractPhysicalObject? target, bool direct, bool chaining)
        {
            AbstractCreature? sourceSelf = source as AbstractCreature;
            AbstractCreature? targetSelf = target as AbstractCreature;
            FriendshipType type = sourceSelf.GetFriendshipType(targetSelf, chaining);

            if (type != FriendshipType.None)
            {
                return type;
            }

            if (direct || sourceSelf.IsDenied || targetSelf.IsDenied)
            {
                return FriendshipType.None;
            }

            AbstractCreature? sourceOwner = source.Owner;
            AbstractCreature? targetOwner = target.Owner;

            return sourceSelf.GetFriendshipType(targetOwner, chaining) != FriendshipType.None
                || sourceOwner.GetFriendshipType(targetSelf, chaining) != FriendshipType.None
                || sourceOwner.GetFriendshipType(targetOwner, chaining) != FriendshipType.None ? FriendshipType.Indirect : FriendshipType.None;
        }
    }

    extension(PhysicalObject? source)
    {
        public bool IsTracked => (source?.abstractPhysicalObject).IsTracked;

        public bool IsFriendOfPlayer => (source?.abstractPhysicalObject).IsFriendOfPlayer;

        public bool IsFriend(AbstractPhysicalObject? target, bool direct = false) => (source?.abstractPhysicalObject).IsFriend(target, direct);

        public bool IsFriend(PhysicalObject? target, bool direct = false) => (source?.abstractPhysicalObject).IsFriend(target?.abstractPhysicalObject, direct);
    }

    extension(Room? room)
    {
        public IEnumerable<PhysicalObject> Objects
        {
            get
            {
                foreach (var physicalObjects in room?.physicalObjects ?? [])
                {
                    foreach (var physicalObject in physicalObjects)
                    {
                        if (physicalObject != null)
                        {
                            yield return physicalObject;
                        }
                    }
                }
            }
        }

        public IEnumerable<Creature> Creatures
        {
            get
            {
                foreach (var physicalObject in room.Objects)
                {
                    if (physicalObject is Creature creature)
                    {
                        yield return creature;
                    }
                }
            }
        }

        public IEnumerable<Creature> PlayerFriends
        {
            get
            {
                foreach (var creature in room.Creatures)
                {
                    if (creature.IsFriendOfPlayer)
                    {
                        yield return creature;
                    }
                }
            }
        }

        public IEnumerable<Creature> FriendsOf(PhysicalObject? source)
        {
            foreach (var creature in room.Creatures)
            {
                if (source.IsFriend(creature))
                {
                    yield return creature;
                }
            }
        }
    }
}
