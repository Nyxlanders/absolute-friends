using System.Security.Permissions;
using AbsoluteFriends.Addons;
using AbsoluteFriends.Options;
using BepInEx;

#pragma warning disable CS0618
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618

namespace AbsoluteFriends.FriendshipEditor;

[BepInPlugin(GUID, Name, Version)]
[BepInDependency(Core.Plugin.GUID)]
[BepInDependency(Manager.Plugin.GUID)]
public class Plugin : AddonPlugin
{
    public const string GUID = "pjhgamma.absolutefriends.friendshipeditor";

    public const string Name = "Absolute Friends: Friendship Editor";

    public const string Version = "0.2.0";

    public override string AddonDescription => "Sets rules for players' reputation and creatures' like and relationships toward friends.";

    public override string AbsoluteFriendsVersion => AbsoluteFriends.Plugin.Version;

    public override string[] AddonDependencies => [Core.Plugin.GUID, Manager.Plugin.GUID];

    protected override void Bind(Addon addon) => FriendshipEditor.Config.Bind(addon);

    protected override void BuildMenu(MenuBuilder menu) => Menu.Build(menu);

    protected override void Enable()
    {
        RuleStore.Register();
        EditorTab.Register();
    }

    protected override void Disable()
    {
        EditorTab.Unregister();
        RuleStore.Unregister();
    }
}
