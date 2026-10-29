using AbsoluteFriends.Addons;
using AbsoluteFriends.Options;
using BepInEx;

namespace AbsoluteFriendsExample;

[BepInPlugin(GUID, Name, Version)]
// Declare installed Absolute Friends plugins as dependencies instead of bundling their DLLs.
// Duplicate plugin IDs can load stale or incompatible code.
[BepInDependency(AbsoluteFriends.Core.Plugin.GUID)]
// The Manager plugin is needed for its tab API. It is not listed in AddonDependencies, because the example's other options work with the Manager addon off.
[BepInDependency(AbsoluteFriends.Manager.Plugin.GUID)]
public class Plugin : AddonPlugin
{
    public const string GUID = "pjhgamma.absolutefriends.example";

    public const string Name = "Absolute Friends: Example";

    public const string Version = "0.2.0";

    public override string AddonDescription => "Adds friendship, ownership, tracking, and name rules for Hunter Long Legs and the green Neuron.";

    // Declare the Absolute Friends version this addon was built against for compatibility checks.
    public override string AbsoluteFriendsVersion => AbsoluteFriends.Plugin.Version;

    // List the Absolute Friends addon IDs that must be applied before this addon's controls can be enabled.
    public override string[] AddonDependencies => [AbsoluteFriends.Core.Plugin.GUID];

    // Override this property when the plugin needs its own independent Remix menu in addition to its addon panel.
    protected override OptionInterface RemixMenu => AbsoluteFriendsExample.RemixMenu.Instance;

    // Bind addon options here so profiles and the main Addons tab can discover them.
    protected override void Bind(Addon addon) => AddonConfig.Bind(addon);

    // Build this addon's options inside its card in the shared Addons tab; omit the override when it has no controls.
    protected override void BuildMenu(MenuBuilder menu) => AddonMenu.Build(menu);

    // Register gameplay behavior when Absolute Friends enables this addon.
    protected override void Enable()
    {
        // Register external rules here when they need matching cleanup in Disable.
        RelationshipRules.Register();
        NameRules.Register();
        ManagerTab.Register();
        HunterSync.Register();

        Logger.LogInfo($"Hunter Long Legs example enabled with {AddonContext.OptionCount} options");
    }

    // Unregister gameplay behavior when Absolute Friends disables this addon.
    protected override void Disable()
    {
        RelationshipRules.Unregister();
        NameRules.Unregister();
        ManagerTab.Unregister();
        HunterSync.Unregister();

        Logger.LogInfo("Hunter Long Legs example disabled");
    }
}
