using System.Security.Permissions;
using AbsoluteFriends.Options;
using BepInEx;

#pragma warning disable CS0618
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618

namespace AbsoluteFriends.ChainedTogether;

[BepInPlugin(GUID, Name, Version)]
[BepInDependency(Core.Plugin.GUID)]
public class Plugin : AddonPlugin
{
    public const string GUID = "pjhgamma.absolutefriends.chainedtogether";

    public const string Name = "Chained Together";

    public const string Version = "0.1.0";

    public override string AddonDescription => "Chains tracked friends together.";

    public override string[] AddonDependencies => [Core.Plugin.GUID];

    public override string AbsoluteFriendsVersion => AbsoluteFriends.Plugin.Version;

    protected override void Bind(Addon addon) => ChainedTogether.Config.Bind(addon);

    protected override void BuildMenu(MenuBuilder menu) => Menu.Build(menu);

    protected override void Enable() => ScriptedPlayers.Register();

    protected override void Disable() => ScriptedPlayers.Unregister();
}
