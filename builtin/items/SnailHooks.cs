using AbsoluteFriends.Core;
using AbsoluteFriends.Diagnostics;
using AbsoluteFriends.Hooks;
using Mono.Cecil.Cil;
using MonoMod.Cil;

namespace AbsoluteFriends.Items;

internal class SnailHooks : BaseHooks
{
    protected override Configurable<bool>[] Options => [Config.Snail];

    [HookPatch(typeof(IL.Snail), nameof(IL.Snail.Click))]
    [HookTest([373, 376, 385], ["ldarg.0; ldfld PhysicalObject::abstractPhysicalObject", "beq.s; ldloc.s; ldfld PhysicalObject::abstractPhysicalObject", "ldloc.s; callvirt PhysicalObject::get_bodyChunks"])]
    private static void IL_Snail_Click(ILContext il) => il.FriendBranch();

    [HookPatch(typeof(On.Snail), nameof(On.Snail.Click))]
    private static void On_Snail_Click(On.Snail.orig_Click orig, Snail self)
    {
        orig(self);

        if (self.triggerTicker <= 0)
        {
            self.SetOwner();
        }
    }

    [HookPatch(typeof(On.Player), nameof(On.Player.TossObject))]
    private static void On_Player_TossObject(On.Player.orig_TossObject orig, Player self, int grasp, bool eu)
    {
        if (self.grasps[grasp]?.grabbed is Snail snail)
        {
            snail.SetOwner(self);
        }

        orig(self, grasp, eu);
    }

    [HookPatch(typeof(IL.Snail), nameof(IL.Snail.Violence))]
    [HookTest([15], ["ldarg.0; ldc.i4.1; stfld Snail::triggered"])]
    private static void IL_Snail_Violence(ILContext il)
    {
        ILCursor c = new(il);

        if (c.TryGotoNext(
            i => i.MatchLdarg(0),
            i => i.MatchLdcI4(1),
            i => i.MatchStfld<Snail>("triggered")
        ))
        {
            c.Emit(OpCodes.Ldarg_1);
            c.Emit(OpCodes.Ldarg_0);
            c.EmitGuarded((BodyChunk? bodyChunk, Snail snail) => bodyChunk?.owner.PropagateOwnerTo(snail));
        }
    }

    [HookPatch(typeof(On.Snail), nameof(On.Snail.Die))]
    private static void On_Snail_Die(On.Snail.orig_Die orig, Snail self)
    {
        if (self.killTag != null)
        {
            self.SetOwner(self.killTag);
        }

        orig(self);
    }
}
