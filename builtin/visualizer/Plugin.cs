using System.Security.Permissions;
using AbsoluteFriends.Options;
using BepInEx;

#pragma warning disable CS0618
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618

namespace AbsoluteFriends.Visualizer;

[BepInPlugin(GUID, Name, AbsoluteFriends.Plugin.Version)]
[BepInDependency(Core.Plugin.GUID)]
[BepInDependency(MeadowOptions.MeadowGUID, BepInDependency.DependencyFlags.SoftDependency)]
public class Plugin : AddonPlugin
{
    public const string GUID = "pjhgamma.absolutefriends.visualizer";

    public const string Name = "Absolute Friends: Visualizer";

    public override string AddonDescription => "Shows friends and owners on screen.";

    public override string[] AddonDependencies => [Core.Plugin.GUID];

    public override string AbsoluteFriendsVersion => AbsoluteFriends.Plugin.Version;

    protected override void Bind(Addon addon) => Visualizer.Config.Bind(addon);

    protected override void BuildMenu(MenuBuilder menu) => Menu.Build(menu);
}
