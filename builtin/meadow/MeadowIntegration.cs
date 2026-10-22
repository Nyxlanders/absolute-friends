namespace AbsoluteFriends.Meadow;

internal static class MeadowIntegration
{
    internal static void Register()
    {
        MeadowRules.Register();
        RemoteCalls.Register();
        OriginIds.Register();
        AvatarIds.Register();
        OwnerSync.Register();
        ValueSync.Register();
        MemorySync.Register();
        LobbySync.Register();
        LostAvatars.Register();
        ArrivingAvatars.Register();
        StaleCopies.Register();
        CrossingFriends.Register();
        RevivedPlayers.Register();
        ForgetSync.Register();
        ArenaRules.Register();
    }

    internal static void Unregister()
    {
        ArenaRules.Unregister();
        ForgetSync.Unregister();
        RevivedPlayers.Unregister();
        CrossingFriends.Unregister();
        StaleCopies.Unregister();
        ArrivingAvatars.Unregister();
        LostAvatars.Unregister();
        LobbySync.Unregister();
        MemorySync.Unregister();
        ValueSync.Unregister();
        OwnerSync.Unregister();
        AvatarIds.Unregister();
        OriginIds.Unregister();
        RemoteCalls.Unregister();
        MeadowRules.Unregister();
        EntityRecords.Clear();
    }
}
