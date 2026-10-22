using AbsoluteFriends.Core;
using AbsoluteFriends.Diagnostics;
using AbsoluteFriends.Hooks;
using AbsoluteFriends.Options;
using AbsoluteFriends.Utils;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using Watcher;

namespace AbsoluteFriends.Pilgrimage;

internal class GateHooks : BaseHooks
{
    protected override Configurable<bool>[] Options => [Config.Gate];

    [HookPatch(typeof(On.Player), nameof(On.Player.Update))]
    private static void On_Player_Update(On.Player.orig_Update orig, Player self, bool eu)
    {
        orig(self, eu);

        if (self.IsPlayer)
        {
            GateUtils.UpdatePlayerIdle(self);
        }
    }

    [HookPatch(typeof(IL.Player), nameof(IL.Player.Update))]
    [HookTest([4485, 4499], ["ldc.i4.s; ble; ldarg.0", "ldarg.0; ldfld Player::touchedNoInputCounter"])]
    private static void IL_Player_Update(ILContext il)
    {
        ILCursor c = new(il);
        ILLabel l = c.DefineLabel();

        if (c.TryGotoNext(
            MoveType.After,
            i => i.MatchLdfld<Player>("timeSinceInCorridorMode"),
            i => i.MatchLdcI4(10)
        ))
        {
            c.Prev.Operand = 0;

            if (c.TryGotoNext(
                MoveType.After,
                i => i.MatchLdcI4(20),
                i => i.MatchBr(out _),
                i => i.MatchLdcI4(40),
                i => i.MatchBle(out _)
            ))
            {
                l = c.MarkLabel();

                if (c.TryGotoPrev(
                    MoveType.After,
                    i => i.MatchLdarg(0),
                    i => i.MatchLdfld<Player>("readyForWin"),
                    i => i.MatchBrfalse(out _)
                ))
                {
                    c.Emit(OpCodes.Br, l);

                    if (c.TryGotoPrev(
                        MoveType.After,
                        i => i.MatchLdarg(0),
                        i => i.MatchLdfld<Player>("readyForWin"),
                        i => i.MatchBrfalse(out l)
                    ))
                    {
                        c.Emit(OpCodes.Ldarg_0);
                        c.EmitGuarded(
                            (Player player) => player.room?.shelterDoor is ShelterDoor shelterDoor
                                && shelterDoor.CanActivate(
                                    ShelterDoorUtils.SleepersIn(shelterDoor, shelterDoor.room?.game?.AlivePlayers),
                                    GateZoneUtils.IsInShelterDoor,
                                    MeadowOptions.IsInLobby || Config.AnyShelter.IsActive ? GateUtils.IsAwaitedInShelter : null
                                ),
                            _ => true
                        );
                        c.Emit(OpCodes.Brfalse, l);
                    }
                }
            }
        }
    }

    [HookPatch(typeof(On.RegionGate), nameof(On.RegionGate.PlayersStandingStill))]
    private static bool On_RegionGate_PlayersStandingStill(On.RegionGate.orig_PlayersStandingStill orig, RegionGate self)
    {
        self.startCounter = 60;

        if (MeadowOptions.IsInLobby)
        {
            return orig(self) && self.CanActivate((self.room?.game).AllPlayers, GateZoneUtils.IsInRegionGate);
        }

        return self.CanActivate(ModManager.CoopAvailable ? self.room?.game?.PlayersToProgressOrWin : self.room?.game?.Players, GateZoneUtils.IsInRegionGate);
    }

    [HookPatch(typeof(On.RegionGate), nameof(On.RegionGate.AllPlayersThroughToOtherSide))]
    private static bool On_RegionGate_AllPlayersThroughToOtherSide(On.RegionGate.orig_AllPlayersThroughToOtherSide orig, RegionGate self)
    {
        if (!orig(self))
        {
            return false;
        }

        foreach (var abstractCreature in FriendUtils.TrackedFriends)
        {
            if (abstractCreature.realizedCreature is not { dead: false } creature || creature.room != self.room)
            {
                continue;
            }

            foreach (var bodyChunk in creature.bodyChunks ?? [])
            {
                if (bodyChunk != null && self.GetBodyChunkLeftTile(bodyChunk) > -8f && self.GetBodyChunkRightTile(bodyChunk) < 4f)
                {
                    return false;
                }
            }
        }

        return true;
    }

    [HookPatch(typeof(IL.RegionGate), nameof(IL.RegionGate.Update))]
    [HookTest([44], ["brfalse; ldarg.0; callvirt RegionGate::get_EnergyEnoughToOpen"])]
    private static void IL_RegionGate_Update(ILContext il)
    {
        ILCursor c = new(il);
        ILLabel l = c.DefineLabel();

        if (c.TryGotoNext(
            i => i.MatchLdarg(0),
            i => i.MatchLdcI4(0),
            i => i.MatchStfld<RegionGate>("startCounter"),
            i => i.MatchBr(out l)
        ) && c.TryGotoPrev(
            MoveType.After,
            i => i.MatchLdarg(0),
            i => i.MatchCall<RegionGate>("PlayersStandingStill")
        ))
        {
            c.Next.Operand = l;
        }
    }
}

internal class WatcherGateHooks : WatcherHooks
{
    protected override Configurable<bool>[] Options => [Config.Gate];

    [HookPatch(typeof(IL.Watcher.WarpPoint), nameof(IL.Watcher.WarpPoint.Update))]
    [HookTest([976], ["ldloc.s; ldfld UpdatableAndDeletable::room"])]
    private static void IL_WarpPoint_Update(ILContext il)
    {
        ILCursor c = new(il);
        ILLabel l = c.DefineLabel();

        if (c.TryGotoNext(
            i => i.MatchLdarg(0),
            i => i.MatchLdfld<WarpPoint>("triggerTime"),
            i => i.MatchLdarg(0),
            i => i.MatchLdfld<WarpPoint>("triggerActivationTime"),
            i => i.MatchBltUn(out l)
        ) && c.TryGotoPrev(
            i => i.MatchLdloc(out _),
            i => i.MatchLdfld<UpdatableAndDeletable>("room"),
            i => i.MatchBrfalse(out _)
        ))
        {
            c.Emit(OpCodes.Ldarg_0);
            c.EmitGuarded((WarpPoint warpPoint) =>
            {
                if (warpPoint.guaranteeTrigger || (warpPoint.overrideData ?? warpPoint.Data).dynamicWarpPoint)
                {
                    if (MeadowOptions.IsInLobby && warpPoint.room.game.Players.Any(abstractPlayer => abstractPlayer.realizedCreature is Player { dead: false } player && warpPoint.IsInWarpPoint(player)))
                    {
                        warpPoint.triggerTime = warpPoint.triggerActivationTime;
                    }

                    return true;
                }

                if (MeadowOptions.IsInLobby)
                {
                    if (warpPoint.CanActivate((warpPoint.room?.game).AllPlayers, GateZoneUtils.IsInWarpPoint))
                    {
                        return true;
                    }

                    warpPoint.triggerTime = 0f;

                    return false;
                }

                warpPoint.triggerTime = 0f;

                if (warpPoint.visualOpenness <= 0.5f || (warpPoint.canPreCast && !Region.RegionReadyToWarp))
                {
                    return false;
                }

                if (warpPoint.CanActivate(warpPoint.room?.game?.AlivePlayers, GateZoneUtils.IsInWarpPoint))
                {
                    warpPoint.triggerTime = warpPoint.triggerActivationTime;

                    return true;
                }

                return false;
            }, _ => true);
            c.Emit(OpCodes.Brfalse, l);
        }
    }
}
