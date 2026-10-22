using System.Security.Permissions;
using AbsoluteFriends.Options;
using BepInEx;

#pragma warning disable CS0618
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618

namespace AbsoluteFriends.Players;

[BepInPlugin(GUID, Name, AbsoluteFriends.Plugin.Version)]
[BepInDependency(Core.Plugin.GUID)]
public class Plugin : AddonPlugin
{
    public const string GUID = "pjhgamma.absolutefriends.players";

    public const string Name = "Absolute Friends: Players";

    public override string AddonDescription => "Controls how players and their abilities treat friends.";

    public override string[] AddonDependencies => [Core.Plugin.GUID];

    public override string AbsoluteFriendsVersion => AbsoluteFriends.Plugin.Version;

    protected override void Bind(Addon addon) => Players.Config.Bind(addon);

    protected override void BuildMenu(MenuBuilder menu) => Menu.Build(menu);

    protected override void Enable() => RippleSync.Register();

    protected override void Disable() => RippleSync.Unregister();
}
