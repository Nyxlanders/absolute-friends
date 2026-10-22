using System.Runtime.CompilerServices;
using AbsoluteFriends.Options;
using AbsoluteFriends.Utils;

namespace AbsoluteFriends.Core;

public static partial class FriendUtils
{
    private static readonly ConditionalWeakTable<FriendTracker, SocialMemory.Relationship> _adoptedRelationships = new();

    private static readonly ConditionalWeakTable<FriendTracker, OriginalFriend> _originalFriends = new();

    private class OriginalFriend
    {
        public AbstractCreature? Creature;

        public SocialMemory.Relationship? Relationship;
    }

    extension(FriendTracker? tracker)
    {
        internal bool HasSlugcatFriend => tracker?.friend is { IsSlugcat: true, abstractCreature: { slatedForDeletion: false, state.dead: false } };

        internal void AdoptSlugcatFriend()
        {
            if (tracker?.AI?.creature is not { HasAuthority: true } abstractCreature)
            {
                return;
            }

            bool adopted = _adoptedRelationships.TryGetValue(tracker, out SocialMemory.Relationship adoptedRelationship)
                && ReferenceEquals(tracker.friendRel, adoptedRelationship);
            OriginalFriend original = _originalFriends.GetOrCreateValue(tracker);

            if (!adopted)
            {
                original.Creature = tracker.friend is { IsSlugcat: true } friend ? friend.abstractCreature : null;
                original.Relationship = original.Creature != null ? tracker.friendRel : null;
            }

            if (!Config.FriendSharing.IsActive || abstractCreature.IsDenied)
            {
                if (adopted)
                {
                    tracker.RestoreFriend(original);
                }

                return;
            }

            if (!adopted && tracker.friend is { IsSlugcat: false })
            {
                return;
            }

            if (
                adopted
                && original.Creature is { IsPlayer: true, slatedForDeletion: false, state.dead: false, realizedCreature.slatedForDeletetion: false } originalPlayer
                && abstractCreature.IsFriend(originalPlayer)
            )
            {
                tracker.RestoreFriend(original);

                return;
            }

            if (
                tracker.friend?.abstractCreature is { IsPlayer: true, slatedForDeletion: false, state.dead: false } currentFriend
                && abstractCreature.IsFriend(currentFriend)
            )
            {
                return;
            }

            if (abstractCreature.state?.socialMemory is { } socialMemory)
            {
                foreach (var abstractPlayer in Players.OrderBy(abstractPlayer => abstractPlayer.Room != abstractCreature.Room))
                {
                    if (
                        abstractPlayer is not { slatedForDeletion: false, state.dead: false, realizedCreature: { } player }
                        || abstractPlayer.world != abstractCreature.world
                        || !abstractCreature.IsFriend(abstractPlayer)
                        || !TryGetLikes(socialMemory, abstractPlayer.ID, out float like, out float tempLike)
                    )
                    {
                        continue;
                    }

                    SocialMemory.Relationship relationship = _adoptedRelationships.GetValue(tracker, _ => new(abstractPlayer.ID));

                    relationship.subjectID = abstractPlayer.ID;
                    relationship.like = like;
                    relationship.tempLike = tempLike;

                    tracker.friend = player;
                    tracker.friendRel = relationship;

                    return;
                }
            }

            if (adopted)
            {
                tracker.RestoreFriend(original);
            }
        }

        private void RestoreFriend(OriginalFriend original)
        {
            tracker!.friend = original.Creature?.realizedCreature is { slatedForDeletetion: false } creature ? creature : null;
            tracker.friendRel = tracker.friend != null ? original.Relationship : null;
        }
    }
}
