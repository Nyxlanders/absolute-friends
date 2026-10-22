using AbsoluteFriends.Diagnostics;
using AbsoluteFriends.Hooks;
using MonoMod.Cil;

namespace AbsoluteFriends.Pilgrimage;

internal class PerseveranceHooks : BaseHooks
{
    protected override Configurable<bool>[] Options => [Config.Perseverance];

    private static void ProgressWithAlivePlayers(ILContext il)
    {
        ILCursor c = new(il);

        while (c.TryGotoNext(i => i.MatchCallOrCallvirt<RainWorldGame>("get_PlayersToProgressOrWin")))
        {
            c.Remove();
            c.EmitGuarded((RainWorldGame game) => game.AlivePlayers, game => game.PlayersToProgressOrWin);
        }
    }

    [HookPatch(typeof(IL.RainWorldGame), nameof(IL.RainWorldGame.JollyGameOverEvaluator))]
    [HookTest([137], ["brfalse.s; ldstr; ldc.i4.0"])]
    private static void IL_RainWorldGame_JollyGameOverEvaluator(ILContext il)
    {
        ILCursor c = new(il);

        c.GotoNext(MoveType.After, i => i.MatchLdsfld<global::Options.JollyDifficulty>("EASY"), i => i.MatchCall(out _));
        c.EmitGuarded((bool isEasy) => true, isEasy => isEasy);
    }

    [HookPatch(typeof(IL.RegionGate), nameof(IL.RegionGate.PlayersInZone))]
    [HookTest([16, 57], ["callvirt List`1::GetEnumerator", "callvirt List`1::GetEnumerator"])]
    private static void IL_RegionGate_PlayersInZone(ILContext il) => ProgressWithAlivePlayers(il);

    [HookPatch(typeof(IL.RegionGate), nameof(IL.RegionGate.ListOfPlayersInZone))]
    [HookTest([11], ["callvirt List`1::GetEnumerator"])]
    private static void IL_RegionGate_ListOfPlayersInZone(ILContext il) => ProgressWithAlivePlayers(il);

    [HookPatch(typeof(IL.RegionGate), nameof(IL.RegionGate.PlayersStandingStill))]
    [HookTest([9], ["ldarg.0; ldfld UpdatableAndDeletable::room"])]
    private static void IL_RegionGate_PlayersStandingStill(ILContext il) => ProgressWithAlivePlayers(il);

    [HookPatch(typeof(IL.ShelterDoor), nameof(IL.ShelterDoor.Close))]
    [HookTest([34], ["ldarg.0; ldfld UpdatableAndDeletable::room"])]
    private static void IL_ShelterDoor_Close(ILContext il) => ProgressWithAlivePlayers(il);

    [HookPatch(typeof(IL.ShelterDoor), nameof(IL.ShelterDoor.DoorClosed))]
    [HookTest([50], ["callvirt List`1::get_Count"])]
    private static void IL_ShelterDoor_DoorClosed(ILContext il) => ProgressWithAlivePlayers(il);
}
