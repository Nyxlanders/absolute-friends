using AbsoluteFriends.Manager;
using RainMeadow;

namespace AbsoluteFriends.Meadow;

internal static class ForgetSync
{
    internal static void Register() => FriendManager.RegisterRemoteForget(Forget);

    internal static void Unregister() => FriendManager.UnregisterRemoteForget(Forget);

    private static void Forget(AbstractCreature abstractCreature)
    {
        if (abstractCreature.GetOnlineObject() is { isMine: false } onlineObject)
        {
            onlineObject.owner.InvokeRPC(ForgetFriend, onlineObject);
        }
    }

    [RPCMethod]
    private static void ForgetFriend(RPCEvent rpcEvent, OnlinePhysicalObject onlineObject)
    {
        if (rpcEvent.from != OnlineManager.lobby?.owner || onlineObject?.apo is not AbstractCreature abstractCreature)
        {
            return;
        }

        FriendManager.ClearFriendship(abstractCreature);
    }
}
