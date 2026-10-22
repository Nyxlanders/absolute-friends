using AbsoluteFriends.Core;
using AbsoluteFriends.Utils;
using RainMeadow;
using RainMeadow.Arena.ArenaOnlineGameModes.TeamBattle;

namespace AbsoluteFriends.Meadow;

internal static class ArenaRules
{
    internal static void Register()
    {
        FriendUtils.RegisterFriendshipRule(IsTeammate);
        FriendUtils.RegisterSharingRule(CanShare);
    }

    internal static void Unregister()
    {
        FriendUtils.UnregisterFriendshipRule(IsTeammate);
        FriendUtils.UnregisterSharingRule(CanShare);
    }

    private static bool? IsTeammate(AbstractCreature abstractCreature, AbstractCreature abstractSlugcat) =>
        abstractCreature.IsPlayer && abstractSlugcat.IsPlayer && abstractCreature != abstractSlugcat ? SameTeam(abstractCreature, abstractSlugcat) : null;

    private static bool CanShare(AbstractCreature abstractSlugcat, AbstractCreature sharedSlugcat) => SameTeam(abstractSlugcat, sharedSlugcat) != false;

    private static bool? SameTeam(AbstractCreature first, AbstractCreature second)
    {
        if (
            !TeamBattleMode.IsTeamBattleMode(out _)
            || first.GetOnlineCreature()?.owner is not { } firstPlayer
            || second.GetOnlineCreature()?.owner is not { } secondPlayer
        )
        {
            return null;
        }

        return ArenaHelpers.CheckSameTeam(firstPlayer, secondPlayer);
    }
}
