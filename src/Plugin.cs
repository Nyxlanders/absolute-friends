using System.Security.Permissions;
using AbsoluteFriends.Diagnostics;
using AbsoluteFriends.Hooks;
using BepInEx;

#pragma warning disable CS0618
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618

namespace AbsoluteFriends;

[BepInPlugin(GUID, Name, Version)]
public class Plugin : BaseUnityPlugin
{
    public const string GUID = "pjhgamma.absolutefriends";

    public const string Name = "Absolute Friends: Refriended";

    public const string Version = "0.3.0";

    private bool _isInit;

    private void OnEnable()
    {
        On.RainWorld.OnModsInit -= On_RainWorld_OnModsInit;
        On.RainWorld.PostModsInit -= On_RainWorld_PostModsInit;

        On.RainWorld.OnModsInit += On_RainWorld_OnModsInit;
        On.RainWorld.PostModsInit += On_RainWorld_PostModsInit;
    }

    private void OnDisable()
    {
        On.RainWorld.OnModsInit -= On_RainWorld_OnModsInit;
        On.RainWorld.PostModsInit -= On_RainWorld_PostModsInit;

        if (!_isInit)
        {
            return;
        }

        _isInit = false;

        HookManager.OnDisable();
    }

    private void On_RainWorld_OnModsInit(On.RainWorld.orig_OnModsInit orig, RainWorld self)
    {
        orig(self);

        try
        {
            if (_isInit)
            {
                return;
            }

            _isInit = true;

            MachineConnector.SetRegisteredOI(GUID, Options.RemixMenu.Instance);

            Reporter.LogInfo("Plugin initialized");
        }
        catch (Exception exception)
        {
            Reporter.LogError("Plugin initialization failed", exception);
        }
    }

    private void On_RainWorld_PostModsInit(On.RainWorld.orig_PostModsInit orig, RainWorld self)
    {
        orig(self);

        HookManager.OnEnable();
    }
}
