using AbsoluteFriends.Core;
using AbsoluteFriends.Hooks;
using MonoMod.Cil;

namespace AbsoluteFriends.Items;

internal class JellyFishHooks : BaseHooks
{
    protected override Configurable<bool>[] Options => [Config.JellyFish];

    [HookPatch(typeof(On.JellyFish), nameof(On.JellyFish.Update))]
    private static void On_JellyFish_Update(On.JellyFish.orig_Update orig, JellyFish self, bool eu)
    {
        orig(self, eu);

        if (self.Grabber == null && !self.Electric)
        {
            self.SetOwner();
        }
    }

    [HookPatch(typeof(On.JellyFish), nameof(On.JellyFish.Tossed))]
    private static void On_JellyFish_Tossed(On.JellyFish.orig_Tossed orig, JellyFish self, Creature tossedBy)
    {
        if (tossedBy != null)
        {
            self.SetOwner();
            self.SetOwner(tossedBy);
        }

        orig(self, tossedBy);
    }

    [HookPatch(typeof(IL.JellyFish), nameof(IL.JellyFish.Update))]
    [HookTest([850, 853, 1300, 1303, 1316], ["ldarg.0; ldfld PhysicalObject::abstractPhysicalObject", "beq.s; ldarg.0; ldfld JellyFish::latchOnToBodyChunks", "ldarg.0; ldfld PhysicalObject::abstractPhysicalObject", "beq.s; ldarg.0; ldfld UpdatableAndDeletable::room", "ldarg.0; ldfld PhysicalObject::grabbedBy"])]
    private static void IL_JellyFish_Update(ILContext il) => il.FriendBranch();

    [HookPatch(typeof(IL.JellyFish), nameof(IL.JellyFish.Collide))]
    [HookTest([7, 20, 47, 60, 63], ["brfalse; ldarg.1; ldarg.0", "ldarg.0; call PhysicalObject::get_firstChunk", "callvirt Creature::get_Template", "ldc.i4.0; ldarg.1; isinst Creature", "callvirt Creature::get_stun"])]
    private static void IL_JellyFish_Collide(ILContext il) => il.NullCreature<JellyFish>();
}
