using System.Reflection;
using AbsoluteFriends.Manager;
using AbsoluteFriends.Options;
using MonoMod.RuntimeDetour;
using RainMeadow;

namespace AbsoluteFriends.Meadow;

internal static class LobbySync
{
    private static Hook? _activateHook;

    internal static void Register()
    {
        _activateHook ??= new Hook(
            typeof(Lobby).GetMethod("ActivateImpl", BindingFlags.NonPublic | BindingFlags.Instance),
            On_Lobby_ActivateImpl
        );

        On.ProcessManager.PostSwitchMainProcess -= On_ProcessManager_PostSwitchMainProcess;
        On.ProcessManager.PostSwitchMainProcess += On_ProcessManager_PostSwitchMainProcess;

        OnlineResource.OnNewOwner -= On_OnlineResource_NewOwner;
        OnlineResource.OnNewOwner += On_OnlineResource_NewOwner;
    }

    internal static void Unregister()
    {
        _activateHook?.Dispose();
        _activateHook = null;

        On.ProcessManager.PostSwitchMainProcess -= On_ProcessManager_PostSwitchMainProcess;
        OnlineResource.OnNewOwner -= On_OnlineResource_NewOwner;

        Leave();
    }

    private static void Leave()
    {
        MeadowOptions.RestoreLocal();
        MeadowOptions.SetInLobby(false);
        FriendManager.ApplyHostState(null);
    }

    private static void On_Lobby_ActivateImpl(Action<Lobby> orig, Lobby self)
    {
        orig(self);

        OptionData.ClearCache();
        EntityRecords.Clear();
        ManagerData.ClearCache();

        if (!self.TryGetData<OptionData>(out _))
        {
            self.AddData(new OptionData());
        }

        if (!self.TryGetData<ManagerData>(out _))
        {
            self.AddData(new ManagerData());
        }

        MeadowOptions.SetInLobby(true);
    }

    private static void On_ProcessManager_PostSwitchMainProcess(On.ProcessManager.orig_PostSwitchMainProcess orig, ProcessManager self, ProcessManager.ProcessID ID)
    {
        orig(self, ID);

        if (OnlineManager.lobby == null)
        {
            Leave();
        }
    }

    private static void On_OnlineResource_NewOwner(OnlineResource resource, OnlinePlayer newOwner)
    {
        if (resource is Lobby && newOwner?.isMe == true)
        {
            MeadowOptions.RestoreLocal();
            FriendManager.ApplyHostState(null);
        }
    }
}
