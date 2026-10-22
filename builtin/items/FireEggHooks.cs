using AbsoluteFriends.Core;
using AbsoluteFriends.Hooks;
using MonoMod.Cil;
using MoreSlugcats;

namespace AbsoluteFriends.Items;

internal class FireEggHooks : DownpourHooks
{
    protected override Configurable<bool>[] Options => [Config.FireEgg];

    [HookPatch(typeof(On.MoreSlugcats.FireEgg), nameof(On.MoreSlugcats.FireEgg.Tossed))]
    private static void On_FireEgg_Tossed(On.MoreSlugcats.FireEgg.orig_Tossed orig, FireEgg self, Creature tosser)
    {
        self.SetOwner(tosser);

        orig(self, tosser);
    }

    [HookPatch(typeof(IL.JokeRifle), nameof(IL.JokeRifle.Use))]
    [HookTest([886], ["callvirt PhysicalObject::get_firstChunk"])]
    private static void IL_JokeRifle_Use(ILContext il) => il.JokeRifleUse<FireEgg>();

    [HookPatch(typeof(IL.MoreSlugcats.FireEgg), nameof(IL.MoreSlugcats.FireEgg.Update))]
    [HookTest([679, 760, 765], ["brfalse; ldloc.s; ldfld CollisionResult::chunk", "brfalse.s; ldarg.0; call FireEgg::get_stuckInChunk", "ldflda Creature::enteringShortCut"])]
    private static void IL_FireEgg_Update(ILContext il) => il.NullCreature<FireEgg>();

    [HookPatch(typeof(IL.MoreSlugcats.FireEgg), nameof(IL.MoreSlugcats.FireEgg.Collide))]
    [HookTest([7], ["brfalse; ldarg.0; ldfld FireEgg::mode"])]
    private static void IL_FireEgg_Collide(ILContext il) => il.NullCreature<FireEgg>();
}
