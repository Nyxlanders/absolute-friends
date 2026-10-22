using AbsoluteFriends.Diagnostics;
using AbsoluteFriends.Hooks;
using AbsoluteFriends.Options;
using AbsoluteFriends.Utils;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using Watcher;

namespace AbsoluteFriends.Resonance;

internal class ResonanceHooks : BaseHooks
{
    protected override Configurable<bool>[] Options => [Config.ResonanceGate];

    [HookPatch(typeof(IL.ShelterDoor), nameof(IL.ShelterDoor.Close))]
    [HookTest([26], ["brfalse; ldarg.0; ldfld UpdatableAndDeletable::room"])]
    private static void IL_ShelterDoor_Close(ILContext il)
    {
        ILCursor c = new(il);

        if (c.TryGotoNext(
            MoveType.After,
            i => i.MatchLdsfld<ModManager>("CoopAvailable")
        ))
        {
            c.Emit(OpCodes.Ldarg_0);
            c.EmitGuarded((ShelterDoor shelterDoor) =>
            {
                if (Config.ResonanceGate.IsActive && shelterDoor.closedFac == shelterDoor.closeSpeed)
                {
                    shelterDoor.Resonate(GateZoneUtils.IsInShelterDoor);
                }
            });
        }
    }

    [HookPatch(typeof(IL.RegionGate), nameof(IL.RegionGate.Update))]
    [HookTest([72], ["ldarg.0; ldsfld Mode::ClosingAirLock"])]
    private static void IL_RegionGate_Update(ILContext il)
    {
        ILCursor c = new(il);

        if (c.TryGotoNext(
            MoveType.AfterLabel,
            i => i.MatchLdarg(0),
            i => i.MatchLdsfld<RegionGate.Mode>("ClosingAirLock"),
            i => i.MatchStfld<RegionGate>("mode")
        ))
        {
            c.Emit(OpCodes.Ldarg_0);
            c.EmitGuarded((RegionGate regionGate) =>
            {
                if (Config.ResonanceGate.IsActive)
                {
                    regionGate.Resonate(GateZoneUtils.IsInRegionGate);
                }
            });
        }
    }

    [HookPatch(typeof(On.RoomSpecificScript.SB_A14KarmaIncrease), nameof(On.RoomSpecificScript.SB_A14KarmaIncrease.Update))]
    private static void On_SB_A14KarmaIncrease_Update(On.RoomSpecificScript.SB_A14KarmaIncrease.orig_Update orig, RoomSpecificScript.SB_A14KarmaIncrease self, bool eu)
    {
        bool addKarma = self.addKarma;

        orig(self, eu);

        if (addKarma && !self.addKarma)
        {
            self.ResonateApart(GateZoneUtils.IsInKarmaIncrease);
        }
    }
}

internal class WatcherResonanceHooks : WatcherHooks
{
    protected override Configurable<bool>[] Options => [Config.ResonanceGate];

    [HookPatch(typeof(IL.Watcher.WarpPoint), nameof(IL.Watcher.WarpPoint.Update))]
    [HookTest([1102], ["ldarg.0; ldloc.s; stfld WarpPoint::playerTriggeredWarpPoint"])]
    private static void IL_WarpPoint_Update(ILContext il)
    {
        ILCursor c = new(il);

        if (c.TryGotoNext(
            MoveType.After,
            i => i.MatchLdarg(0),
            i => i.MatchLdfld<WarpPoint>("triggerTime"),
            i => i.MatchLdarg(0),
            i => i.MatchLdfld<WarpPoint>("triggerActivationTime"),
            i => i.MatchBltUn(out _)
        ))
        {
            c.Emit(OpCodes.Ldarg_0);
            c.EmitGuarded((WarpPoint warpPoint) =>
            {
                if (Config.ResonanceGate.IsActive)
                {
                    warpPoint.ResonateApart(GateZoneUtils.IsInWarpPoint);
                }
            });
        }
    }

    [HookPatch(typeof(On.Watcher.WarpPoint), nameof(On.Watcher.WarpPoint.WarpPrecast))]
    private static void On_WarpPoint_WarpPrecast(On.Watcher.WarpPoint.orig_WarpPrecast orig, WarpPoint self)
    {
        if (
            MeadowOptions.IsInLobby
            && self.room?.game?.session is StoryGameSession session
            && session.spinningTopWarpsLeadingToRippleScreen.Contains(self.MyIdentifyingString())
        )
        {
            self.ResonateHere((warpPoint, creature) => creature.room == warpPoint.room);
        }

        orig(self);
    }
}

internal class ResonanceAnchorHooks : BaseHooks
{
    protected override Configurable<bool>[] Options => [Config.ResonanceRoom, Config.ResonanceGrab];

    protected override string? Subject => "Resonance Anchor";

    [HookPatch(typeof(On.Room), nameof(On.Room.Loaded))]
    private static void On_Room_Loaded(On.Room.orig_Loaded orig, Room self)
    {
        orig(self);

        self.AddObject(new ResonanceAnchor(self));
    }
}
