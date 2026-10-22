using System.Security.Permissions;
using AbsoluteFriends.Options;
using BepInEx;

#pragma warning disable CS0618
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618

namespace AbsoluteFriends.Meadow;

[BepInPlugin(GUID, Name, AbsoluteFriends.Plugin.Version)]
[BepInDependency(Core.Plugin.GUID)]
[BepInDependency(Manager.Plugin.GUID)]
[BepInDependency(MeadowOptions.MeadowGUID, BepInDependency.DependencyFlags.SoftDependency)]
public class Plugin : AddonPlugin
{
    public const string GUID = "pjhgamma.absolutefriends.meadow";

    public const string Name = "Absolute Friends: Meadow";

    public override string AddonDescription => "Lets Absolute Friends recognize other players and their creatures in Rain Meadow.";

    public override string AbsoluteFriendsVersion => AbsoluteFriends.Plugin.Version;

    protected override void Enable()
    {
        if (MeadowOptions.IsMeadowInstalled)
        {
            MeadowIntegration.Register();
        }
    }

    protected override void Disable()
    {
        if (MeadowOptions.IsMeadowInstalled)
        {
            MeadowIntegration.Unregister();
        }
    }
}
