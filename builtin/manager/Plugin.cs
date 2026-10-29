using System.Security.Permissions;
using AbsoluteFriends.Options;
using BepInEx;

#pragma warning disable CS0618
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618

namespace AbsoluteFriends.Manager;

[BepInPlugin(GUID, Name, AbsoluteFriends.Plugin.Version)]
[BepInDependency(Core.Plugin.GUID)]
public class Plugin : AddonPlugin
{
    public const string GUID = "pjhgamma.absolutefriends.manager";

    public const string Name = "Absolute Friends: Manager";

    public override string AddonDescription => "Lists friends during gameplay, shows their relationships, and manages nicknames.";

    public override string[] AddonDependencies => [Core.Plugin.GUID];

    public override string AbsoluteFriendsVersion => AbsoluteFriends.Plugin.Version;

    protected override void Bind(Addon addon) => Manager.Config.Bind(addon);

    protected override void BuildMenu(MenuBuilder menu) => Menu.Build(menu);

    protected override void Enable()
    {
        FriendNicknames.Register();
        InputBlocker.Register();
    }

    protected override void Disable()
    {
        FriendNicknames.Unregister();
        InputBlocker.Unregister();
    }
}
