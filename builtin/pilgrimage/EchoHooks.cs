using AbsoluteFriends.Core;
using AbsoluteFriends.Diagnostics;
using AbsoluteFriends.Hooks;
using Mono.Cecil.Cil;
using MonoMod.Cil;

namespace AbsoluteFriends.Pilgrimage;

internal class EchoHooks : BaseHooks
{
    protected override Configurable<bool>[] Options => [Config.Echo];

    private static void SpareTrackedFriends(ILContext il)
    {
        ILCursor c = new(il);

        while (c.TryGotoNext(
            i => i.MatchLdfld<AbstractCreature>("creatureTemplate"),
            i => i.MatchLdfld<CreatureTemplate>("ghostSedationImmune")
        ))
        {
            c.Emit(OpCodes.Dup);
            c.Index += 2;
            c.EmitGuarded((AbstractCreature abstractCreature, bool isImmune) => isImmune || abstractCreature.IsTracked, (_, isImmune) => isImmune);
        }
    }

    [HookPatch(typeof(IL.GhostCreatureSedater), nameof(IL.GhostCreatureSedater.ctor))]
    [HookTest([52, 54], ["ldfld AbstractCreature::creatureTemplate", "brtrue; ldarg.1; callvirt Room::get_abstractRoom"])]
    private static void IL_GhostCreatureSedater_ctor(ILContext il) => SpareTrackedFriends(il);

    [HookPatch(typeof(IL.GhostCreatureSedater), nameof(IL.GhostCreatureSedater.Update))]
    [HookTest([130, 132], ["ldfld AbstractCreature::creatureTemplate", "brtrue; ldsfld ModManager::MMF"])]
    private static void IL_GhostCreatureSedater_Update(ILContext il) => SpareTrackedFriends(il);
}
