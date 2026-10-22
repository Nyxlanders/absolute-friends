using AbsoluteFriends.Core;
using AbsoluteFriends.Hooks;
using MonoMod.Cil;

namespace AbsoluteFriends.Players;

internal class SaintAttunementHooks : DownpourHooks
{
    protected override Configurable<bool>[] Options => [Config.SaintAttunement];

    [HookPatch(typeof(IL.Player), nameof(IL.Player.ClassMechanicsSaint))]
    [HookTest([1235, 1238, 1247], ["ldarg.0; ldfld PhysicalObject::abstractPhysicalObject", "beq.s; ldloc.s; ldfld PhysicalObject::abstractPhysicalObject", "ldloc.s; callvirt PhysicalObject::get_bodyChunks"])]
    private static void IL_Player_ClassMechanicsSaint(ILContext il) => il.FriendBranch();
}
