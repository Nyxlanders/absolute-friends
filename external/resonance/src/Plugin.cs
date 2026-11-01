using System.Security.Permissions;
using AbsoluteFriends.Options;
using BepInEx;

#pragma warning disable CS0618
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618

namespace AbsoluteFriends.Resonance;

[BepInPlugin(GUID, Name, Version)]
[BepInDependency(Core.Plugin.GUID)]
public class Plugin : AddonPlugin
{
    public const string GUID = "pjhgamma.absolutefriends.resonance";

    public const string Name = "Resonance";

    public const string Version = "0.2.1";

    public override string AddonDescription => "Warps and mends tracked friends through resonance.";

    public override string[] AddonDependencies => [Core.Plugin.GUID];

    public override string AbsoluteFriendsVersion => AbsoluteFriends.Plugin.Version;

    protected override void Bind(Addon addon) => Resonance.Config.Bind(addon);

    protected override void BuildMenu(MenuBuilder menu) => Menu.Build(menu);

    protected override void Enable() => ResonanceUtils.RegisterRemoteHandlers();

    protected override void Disable() => ResonanceUtils.UnregisterRemoteHandlers();
}
