using System.Security.Permissions;
using AbsoluteFriends.Options;
using BepInEx;

#pragma warning disable CS0618
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618

namespace AbsoluteFriends.Pilgrimage;

[BepInPlugin(GUID, Name, AbsoluteFriends.Plugin.Version)]
[BepInDependency(Core.Plugin.GUID)]
[BepInDependency(MeadowOptions.MeadowGUID, BepInDependency.DependencyFlags.SoftDependency)]
public class Plugin : AddonPlugin
{
    public const string GUID = "pjhgamma.absolutefriends.pilgrimage";

    public const string Name = "Absolute Friends: Pilgrimage";

    public override string AddonDescription => "Helps tracked friends join the pilgrimage.";

    public override string[] AddonDependencies => [Core.Plugin.GUID];

    public override string AbsoluteFriendsVersion => AbsoluteFriends.Plugin.Version;

    protected override void Bind(Addon addon) => Pilgrimage.Config.Bind(addon);

    protected override void BuildMenu(MenuBuilder menu) => Menu.Build(menu);

    protected override void Enable()
    {
        if (MeadowOptions.IsMeadowInstalled)
        {
            MeadowGates.Register();
            MeadowSpectating.Register();
            MeadowShelters.Register();
            MeadowWarps.Register();
        }
    }

    protected override void Disable()
    {
        if (MeadowOptions.IsMeadowInstalled)
        {
            MeadowWarps.Unregister();
            MeadowShelters.Unregister();
            MeadowSpectating.Unregister();
            MeadowGates.Unregister();
        }
    }
}
