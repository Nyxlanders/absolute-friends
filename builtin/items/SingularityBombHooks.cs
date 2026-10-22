using AbsoluteFriends.Core;
using AbsoluteFriends.Hooks;
using MonoMod.Cil;
using MoreSlugcats;

namespace AbsoluteFriends.Items;

internal class SingularityBombHooks : DownpourHooks
{
    protected override Configurable<bool>[] Options => [Config.SingularityBomb];

    [HookPatch(typeof(IL.MoreSlugcats.SingularityBomb), nameof(IL.MoreSlugcats.SingularityBomb.HitByWeapon))]
    [HookTest([12], ["ldarg.0; call SingularityBomb::CreateFear"])]
    private static void IL_SingularityBomb_HitByWeapon(ILContext il) => il.WeaponHitByWeapon<SingularityBomb>("CreateFear");

    [HookPatch(typeof(IL.JokeRifle), nameof(IL.JokeRifle.Use))]
    [HookTest([810], ["callvirt PhysicalObject::get_firstChunk"])]
    private static void IL_JokeRifle_Use(ILContext il) => il.JokeRifleUse<SingularityBomb>();

    [HookPatch(typeof(IL.MoreSlugcats.SingularityBomb), nameof(IL.MoreSlugcats.SingularityBomb.Update))]
    [HookTest([494, 497, 512, 664, 667, 682], ["ldarg.0; ldfld PhysicalObject::abstractPhysicalObject", "beq.s; ldarg.0; ldfld UpdatableAndDeletable::room", "ldc.i4.0; stloc.s; br", "ldarg.0; ldfld PhysicalObject::abstractPhysicalObject", "beq.s; ldarg.0; ldfld UpdatableAndDeletable::room", "ldc.i4.0; stloc.s; br"])]
    private static void IL_SingularityBomb_Update(ILContext il) => il.FriendBranch();

    [HookPatch(typeof(IL.MoreSlugcats.SingularityBomb), nameof(IL.MoreSlugcats.SingularityBomb.Explode))]
    [HookTest([348, 351, 366], ["ldarg.0; ldfld PhysicalObject::abstractPhysicalObject", "beq.s; ldarg.0; ldfld UpdatableAndDeletable::room", "ldarg.0; ldfld UpdatableAndDeletable::room"])]
    private static void IL_SingularityBomb_Explode(ILContext il) => il.FriendBranch();
}
