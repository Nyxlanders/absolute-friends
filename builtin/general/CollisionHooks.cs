using System.Reflection;
using AbsoluteFriends.Core;
using AbsoluteFriends.Diagnostics;
using AbsoluteFriends.Hooks;
using Mono.Cecil.Cil;
using MonoMod.Cil;

namespace AbsoluteFriends.General;

internal class CollisionHooks : BaseHooks
{
    private static readonly MethodInfo _getObject = typeof(List<PhysicalObject>).GetProperty("Item").GetGetMethod();

    protected override Configurable<bool>[] Options => [Config.Collision];

    private static bool MatchObject(Instruction[] instructions, int start, out int layer, out int index)
    {
        layer = index = -1;

        return start + 4 < instructions.Length
            && instructions[start].MatchLdfld<Room>("physicalObjects")
            && instructions[start + 1].MatchLdloc(out layer)
            && instructions[start + 2].MatchLdelemRef()
            && instructions[start + 3].MatchLdloc(out index)
            && instructions[start + 4].MatchCallvirt(_getObject);
    }

    [HookPatch(typeof(IL.Room), nameof(IL.Room.Update))]
    [HookTest([1157], ["ldc.i4.0; stloc.s; ldc.i4.0"])]
    private static void IL_Room_Update(ILContext il)
    {
        ILCursor c = new(il);
        ILLabel l = c.DefineLabel();
        int layer = -1;
        int first = -1;
        int second = -1;

        if (!c.TryGotoNext(i => i.MatchLdfld<AbstractPhysicalObject>("rippleLayer")))
        {
            return;
        }

        Instruction[] instructions = [.. il.Instrs];
        int rippleIndex = c.Index;

        if (!MatchObject(instructions, rippleIndex - 6, out layer, out first)
            || !c.TryGotoNext(i => i.MatchLdfld<AbstractPhysicalObject>("rippleLayer"))
            || !MatchObject(instructions, c.Index - 6, out int secondLayer, out second)
            || secondLayer != layer)
        {
            return;
        }

        for (int axis = 0; axis < 2; axis++)
        {
            if (!c.TryGotoNext(
                MoveType.After,
                i => i.MatchLdfld<PhysicalObject>("collisionRange"),
                i => i.MatchAdd(),
                i => i.MatchBgeUn(out l)
            ))
            {
                return;
            }
        }

        c.Emit(OpCodes.Ldarg_0);
        c.Emit(OpCodes.Ldloc, il.Body.Variables[layer]);
        c.Emit(OpCodes.Ldloc, il.Body.Variables[first]);
        c.Emit(OpCodes.Ldloc, il.Body.Variables[second]);
        c.EmitGuarded(
            (Room room, int objectLayer, int firstIndex, int secondIndex) => room.physicalObjects[objectLayer][firstIndex].IsFriend(room.physicalObjects[objectLayer][secondIndex]),
            (_, _, _, _) => false
        );
        c.Emit(OpCodes.Brtrue, l);
    }
}
