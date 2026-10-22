using AbsoluteFriends.Core;
using AbsoluteFriends.Hooks;
using MonoMod.Cil;

namespace AbsoluteFriends.Items;

internal class ScavengerBombHooks : BaseHooks
{
    protected override Configurable<bool>[] Options => [Config.ScavengerBomb];

    [HookPatch(typeof(IL.ScavengerBomb), nameof(IL.ScavengerBomb.HitByWeapon))]
    [HookTest([18], ["ldarg.0; call ScavengerBomb::InitiateBurn"])]
    private static void IL_ScavengerBomb_HitByWeapon(ILContext il) => il.WeaponHitByWeapon<ScavengerBomb>("InitiateBurn");

    [HookPatch(typeof(IL.ScavengerBomb), nameof(IL.ScavengerBomb.HitByExplosion))]
    [HookTest([17], ["ldarg.0; call ScavengerBomb::InitiateBurn"])]
    private static void IL_ScavengerBomb_HitByExplosion(ILContext il) => il.WeaponHitByExplosion<ScavengerBomb>("InitiateBurn");
}

internal class DownpourScavengerBombHooks : DownpourHooks
{
    protected override Configurable<bool>[] Options => [Config.ScavengerBomb];

    [HookPatch(typeof(IL.JokeRifle), nameof(IL.JokeRifle.Use))]
    [HookTest([957], ["callvirt PhysicalObject::get_firstChunk"])]
    private static void IL_JokeRifle_Use(ILContext il) => il.JokeRifleUse<ScavengerBomb>();
}
