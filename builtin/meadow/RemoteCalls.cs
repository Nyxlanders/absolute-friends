using AbsoluteFriends.Utils;
using RainMeadow;

namespace AbsoluteFriends.Meadow;

internal static class RemoteCalls
{
    internal static void Register() => AuthorityUtils.RegisterRemoteSender(Send);

    internal static void Unregister() => AuthorityUtils.UnregisterRemoteSender(Send);

    private static bool Send(AbstractPhysicalObject? abstractPhysicalObject, string channel, string data)
    {
        if (OnlineManager.lobby == null)
        {
            return false;
        }

        if (abstractPhysicalObject == null)
        {
            foreach (var player in OnlineManager.players)
            {
                if (!player.isMe && !player.hasLeft)
                {
                    player.InvokeRPC(Receive, channel, data);
                }
            }

            return true;
        }

        if (EntityRecords.TryGetLostAvatar(abstractPhysicalObject, out OnlinePlayer owner, out string id))
        {
            if (owner.isMe)
            {
                return false;
            }

            owner.InvokeRPC(ReceiveForAvatar, id, channel, data);

            return true;
        }

        if (abstractPhysicalObject.GetOnlineObject() is { } onlineObject)
        {
            if (onlineObject.isMine)
            {
                return false;
            }

            onlineObject.owner.InvokeRPC(ReceiveFor, onlineObject, channel, data);

            return true;
        }

        if (!EntityRecords.TryGetOwner(abstractPhysicalObject, out OnlinePlayer entityOwner, out string entityId) || entityOwner.isMe || entityOwner.hasLeft)
        {
            return false;
        }

        entityOwner.InvokeRPC(ReceiveForEntity, entityId, channel, data);

        return true;
    }

    [RPCMethod]
    private static void Receive(string channel, string data) => AuthorityUtils.ReceiveRemote(null, channel, data);

    [RPCMethod]
    private static void ReceiveForAvatar(string id, string channel, string data)
    {
        if (OnlineManager.lobby?.gameMode.avatars.FirstOrDefault(avatar => avatar.id.ToString() == id)?.abstractCreature is { } abstractCreature)
        {
            AuthorityUtils.ReceiveRemote(abstractCreature, channel, data);
        }
    }

    [RPCMethod]
    private static void ReceiveForEntity(string id, string channel, string data)
    {
        if (EntityRecords.FindLocal(id) is { } abstractPhysicalObject)
        {
            AuthorityUtils.ReceiveRemote(abstractPhysicalObject, channel, data);
        }
    }

    [RPCMethod]
    private static void ReceiveFor(OnlinePhysicalObject onlineObject, string channel, string data)
    {
        if (onlineObject?.apo is { } abstractPhysicalObject)
        {
            AuthorityUtils.ReceiveRemote(abstractPhysicalObject, channel, data);
        }
    }
}
