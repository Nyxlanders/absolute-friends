using AbsoluteFriends.Core;
using AbsoluteFriends.Options;
using AbsoluteFriends.Utils;

namespace AbsoluteFriendsExample;

internal static class RelationshipRules
{
    internal static void Register()
    {
        // Return null to defer to other rules and then Core's built-in decision; false vetoes friendship.
        FriendUtils.RegisterFriendshipRule(IsFriendHunterDaddy);
        FriendUtils.RegisterFriendshipRule(IsGreenNeuronFriend);

        // Self-owned objects participate in ownership checks without belonging to a creature.
        OwnerUtils.RegisterSelfOwnershipRule(IsGreenNeuronSelfOwned);

        // A sharing rule decides whether Friend Sharing may pass one slugcat's friendships to another, and every rule must agree.
        // The Meadow addon uses one to share only between teammates in Team Battle. This example needs none:
        // FriendUtils.RegisterSharingRule((abstractSlugcat, sharedSlugcat) => true);
    }

    internal static void Unregister()
    {
        FriendUtils.UnregisterFriendshipRule(IsFriendHunterDaddy);
        FriendUtils.UnregisterFriendshipRule(IsGreenNeuronFriend);
        OwnerUtils.UnregisterSelfOwnershipRule(IsGreenNeuronSelfOwned);
    }

    private static bool? IsFriendHunterDaddy(AbstractCreature source, AbstractCreature target)
    {
        if (!AddonConfig.Friendship.IsActive || !source.IsHunterDaddy || !target.IsSlugcat)
        {
            return null;
        }

        return !AddonConfig.AggressiveHunter.IsActive || !target.IsRed;
    }

    private static bool? IsGreenNeuronFriend(AbstractCreature abstractCreature, AbstractCreature abstractSlugcat)
    {
        if (abstractCreature is not AbstractOwner abstractOwner)
        {
            return null;
        }

        return AddonConfig.GreenNeuronOwnership.IsActive && abstractOwner.PhysicalObject is NSHSwarmer ? true : null;
    }

    private static bool IsGreenNeuronSelfOwned(PhysicalObject physicalObject) => AddonConfig.GreenNeuronOwnership.IsActive && physicalObject is NSHSwarmer;
}
