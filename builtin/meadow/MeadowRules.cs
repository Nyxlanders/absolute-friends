using AbsoluteFriends.Core;
using AbsoluteFriends.Manager;
using AbsoluteFriends.Utils;
using RainMeadow;

namespace AbsoluteFriends.Meadow;

internal static class MeadowRules
{
    internal static void Register()
    {
        PlayerUtils.RegisterPlayerSource(Avatars);
        AuthorityUtils.RegisterRemoteRule(IsRemote);
        FriendDisplayNames.RegisterPlayerNameRule(PlayerName);
        FriendManager.RegisterSharedIdRule(SharedId);
        AuthorityUtils.RegisterSharedKeyRule(SharedKey);
    }

    internal static void Unregister()
    {
        PlayerUtils.UnregisterPlayerSource(Avatars);
        AuthorityUtils.UnregisterRemoteRule(IsRemote);
        FriendDisplayNames.UnregisterPlayerNameRule(PlayerName);
        FriendManager.UnregisterSharedIdRule(SharedId);
        AuthorityUtils.UnregisterSharedKeyRule(SharedKey);
    }

    private static IEnumerable<AbstractCreature> Avatars(RainWorldGame game)
    {
        if (OnlineManager.lobby is not { } lobby)
        {
            yield break;
        }

        foreach (var avatar in lobby.playerAvatars)
        {
            if (avatar.Value is null || avatar.Key.isMe)
            {
                continue;
            }

            string id = avatar.Value.ToString();

            if (avatar.Value.FindEntity(true) is OnlineCreature { abstractCreature: { } abstractCreature } && abstractCreature.world?.game == game)
            {
                EntityRecords.RememberAvatar(abstractCreature, avatar.Key, id);

                yield return abstractCreature;
            }
            else if (EntityRecords.FindLostAvatar(id, avatar.Key, game) is { } lostAvatar)
            {
                yield return lostAvatar;
            }
        }
    }

    private static bool IsRemote(AbstractPhysicalObject abstractPhysicalObject)
    {
        if (OnlineManager.lobby == null)
        {
            return false;
        }

        if (abstractPhysicalObject.GetOnlineObject() is { } onlineObject)
        {
            EntityRecords.Remember(abstractPhysicalObject, onlineObject);

            return !onlineObject.isMine;
        }

        return EntityRecords.IsRemote(abstractPhysicalObject);
    }

    private static string? PlayerName(AbstractCreature abstractCreature)
    {
        if (OnlineManager.lobby == null || !abstractCreature.IsPlayer)
        {
            return null;
        }

        if (abstractCreature.GetOnlineObject() is OnlineCreature { isAvatar: true } onlineCreature)
        {
            return onlineCreature.owner?.id?.DisplayName;
        }

        if (EntityRecords.TryGetLostAvatar(abstractCreature, out OnlinePlayer owner, out _))
        {
            return owner.id?.DisplayName;
        }

        return OnlineManager.lobby.gameMode.avatars.Any(avatar => avatar.abstractCreature == abstractCreature) ? OnlineManager.mePlayer.id?.DisplayName : null;
    }

    private static string? SharedId(AbstractCreature abstractCreature) => SharedKey(abstractCreature);

    private static string? SharedKey(AbstractPhysicalObject abstractPhysicalObject) => OnlineManager.lobby != null ? abstractPhysicalObject.GetOnlineObject()?.id.ToString() : null;
}
