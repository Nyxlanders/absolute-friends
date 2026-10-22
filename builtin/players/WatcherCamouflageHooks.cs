using System.Runtime.CompilerServices;
using AbsoluteFriends.Core;
using AbsoluteFriends.Diagnostics;
using AbsoluteFriends.Hooks;
using AbsoluteFriends.Utils;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using UnityEngine;

namespace AbsoluteFriends.Players;

internal class WatcherCamouflageHooks : WatcherHooks
{
    private static readonly ConditionalWeakTable<Player, StrongBox<bool>> _camoStates = new();

    private static readonly ConditionalWeakTable<Player, StrongBox<(Player Source, bool InitialCamo)>> _sharedCamoActivations = new();

    private static readonly ConditionalWeakTable<Player, StrongBox<int>> _rippleFoodClocks = new();

    protected override Configurable<bool>[] Options => [Config.WatcherCamouflage];

    private static bool CanToggleCamo(Player player) => player.room != null && !player.inShortcut && player.Consious && player.warpExhausionTime <= 0 && player.timeInVoidSeaRoom < RainWorldUtils.Second;

    private static bool CanEnterCamo(Player player) => CanToggleCamo(player) && player.camoRechargePenalty <= 0;

    private static bool IsActivatingCamo(Player player) => player.activateCamoTimer > 0 || player.performingActivationTimer > 0;

    private static Player? SharedCamoSource(Player player)
    {
        if (!player.IsCamouflagePlayer || !player.IsTracked || (player.isCamo ? !CanToggleCamo(player) : !CanEnterCamo(player)))
        {
            return null;
        }

        foreach (var friendPlayer in FriendPlayers(player))
        {
            bool alreadyToggled = friendPlayer.startingCamoStateOnActivate >= 0
                && friendPlayer.isCamo != (friendPlayer.startingCamoStateOnActivate != 0);

            if (!alreadyToggled && friendPlayer.isCamo == player.isCamo && CanToggleCamo(friendPlayer) && friendPlayer.RippleAbilityActivationButtonCondition)
            {
                return friendPlayer;
            }
        }

        return null;
    }

    private static bool IsSynchronizedActivation(Player player) => !player.RippleAbilityActivationButtonCondition && _sharedCamoActivations.TryGetValue(player, out _);

    private static bool CanContinueSharedCamo(Player player, (Player Source, bool InitialCamo) activation)
    {
        return IsActivatingCamo(player)
            && player.isCamo == activation.InitialCamo
            && (activation.Source.RippleAbilityActivationButtonCondition || activation.Source.isCamo != activation.InitialCamo);
    }

    private static bool ShouldActivateCamo(bool ownInput, Player player)
    {
        if (ownInput)
        {
            _sharedCamoActivations.Remove(player);

            return true;
        }

        if (_sharedCamoActivations.TryGetValue(player, out StrongBox<(Player Source, bool InitialCamo)> shared)
            && CanContinueSharedCamo(player, shared.Value))
        {
            return true;
        }

        if (SharedCamoSource(player) is not { } source)
        {
            _sharedCamoActivations.Remove(player);

            return false;
        }

        _sharedCamoActivations.GetOrCreateValue(player).Value = (source, player.isCamo);

        return true;
    }

    private static int LevitationActivationTimer(int timer, Player player)
    {
        if (player.RippleAbilityActivationButtonCondition)
        {
            _sharedCamoActivations.Remove(player);

            return timer;
        }

        return _sharedCamoActivations.TryGetValue(player, out _) ? 0 : timer;
    }

    private static IEnumerable<Player> FriendPlayers(Player? player)
    {
        if (!player.IsCamouflagePlayer || !player.IsTracked)
        {
            yield break;
        }

        foreach (var friendPlayer in (player?.abstractCreature?.world?.game).RealizedPlayers)
        {
            if (friendPlayer != player && friendPlayer.IsTrackedWatcher && player.IsFriend(friendPlayer))
            {
                yield return friendPlayer;
            }
        }
    }

    private static void SynchronizeCamoCharge(RainWorldGame game)
    {
        List<Player> watchers = [];
        Dictionary<Player, (float Charge, int Penalty, bool AteRippleFood)> activeCharges = [];

        foreach (var player in game.RealizedPlayers)
        {
            if (player.IsTrackedWatcher)
            {
                watchers.Add(player);

                if (!player.inShortcut)
                {
                    bool ateRippleFood = _rippleFoodClocks.TryGetValue(player, out StrongBox<int> foodClock) && foodClock.Value == game.clock;

                    activeCharges[player] = (player.camoCharge, player.camoRechargePenalty, ateRippleFood);
                }
            }
        }

        if (activeCharges.Count == 0)
        {
            return;
        }

        foreach (var player in watchers)
        {
            float minCharge = 0f;
            float maxCharge = 0f;
            int penalty = 0;
            bool ateRippleFood = false;
            bool hasSource = false;

            foreach (var source in activeCharges)
            {
                if (source.Key != player && !player.IsFriend(source.Key))
                {
                    continue;
                }

                minCharge = hasSource ? Mathf.Min(minCharge, source.Value.Charge) : source.Value.Charge;
                maxCharge = hasSource ? Mathf.Max(maxCharge, source.Value.Charge) : source.Value.Charge;
                penalty = Math.Max(penalty, source.Value.Penalty);
                ateRippleFood |= source.Value.AteRippleFood;
                hasSource = true;
            }

            if (!hasSource)
            {
                continue;
            }

            player.camoCharge = ateRippleFood ? minCharge : maxCharge;
            player.camoRechargePenalty = penalty;

            if (player.isCamo && penalty > 0)
            {
                if (player.room != null && !player.inShortcut)
                {
                    player.ToggleCamo();
                }

                player.isCamo = false;
                _camoStates.GetOrCreateValue(player).Value = false;
            }
        }
    }

    private static void SynchronizeCamo(Player player)
    {
        if (!player.IsCamouflagePlayer || !CanToggleCamo(player) || IsActivatingCamo(player))
        {
            return;
        }

        StrongBox<bool> camoState = _camoStates.GetOrCreateValue(player);

        if (player.isCamo != camoState.Value && (!camoState.Value || CanEnterCamo(player)))
        {
            foreach (var friendPlayer in FriendPlayers(player))
            {
                if (friendPlayer.isCamo == camoState.Value)
                {
                    player.ToggleCamo();

                    break;
                }
            }
        }

        camoState.Value = player.isCamo;
    }

    [HookPatch(typeof(IL.Player), nameof(IL.Player.WatcherUpdate))]
    [HookTest([651], ["brfalse; ldarg.0; ldfld Player::rippleRingDelay"])]
    private static void IL_Player_WatcherUpdate(ILContext il)
    {
        ILCursor c = new(il);

        if (c.TryGotoNext(MoveType.After, i => i.MatchCall<Player>("get_RippleAbilityActivationButtonCondition")))
        {
            c.Emit(OpCodes.Ldarg_0);
            c.EmitGuarded(ShouldActivateCamo, (condition, _) => condition);
        }
    }

    [HookPatch(typeof(IL.Player), nameof(IL.Player.TickLevitation_bool_int_float))]
    [HookTest([2], ["ldc.i4.0; bgt.s; ldarg.0"])]
    private static void IL_Player_TickLevitation(ILContext il)
    {
        ILCursor c = new(il);

        if (c.TryGotoNext(MoveType.After, i => i.MatchLdfld<Player>("performingActivationTimer")))
        {
            c.Emit(OpCodes.Ldarg_0);
            c.EmitGuarded(LevitationActivationTimer, (timer, _) => timer);
        }
    }

    [HookPatch(typeof(On.RainWorldGame), nameof(On.RainWorldGame.JollyGameUpdate))]
    private static void On_RainWorldGame_JollyGameUpdate(On.RainWorldGame.orig_JollyGameUpdate orig, RainWorldGame self)
    {
        if (!self.RealizedPlayers.Any(player => player.IsTrackedWatcher))
        {
            orig(self);
        }
    }

    [HookPatch(typeof(On.RainWorldGame), nameof(On.RainWorldGame.Update))]
    private static void On_RainWorldGame_Update(On.RainWorldGame.orig_Update orig, RainWorldGame self)
    {
        orig(self);

        SynchronizeCamoCharge(self);
    }

    [HookPatch(typeof(On.Player), nameof(On.Player.SpawnRippleRing))]
    private static void On_Player_SpawnRippleRing(On.Player.orig_SpawnRippleRing orig, Player self)
    {
        if (!IsSynchronizedActivation(self))
        {
            orig(self);
        }
    }

    [HookPatch(typeof(On.Player), nameof(On.Player.SpawnPersistentRipple))]
    private static void On_Player_SpawnPersistentRipple(On.Player.orig_SpawnPersistentRipple orig, Player self, float minRadius, float maxRadius, int cycleExpiry)
    {
        if (!IsSynchronizedActivation(self))
        {
            orig(self, minRadius, maxRadius, cycleExpiry);
        }
    }

    [HookPatch(typeof(On.Player), nameof(On.Player.ToggleCamo))]
    private static void On_Player_ToggleCamo(On.Player.orig_ToggleCamo orig, Player self)
    {
        bool wasCamo = self.isCamo;

        orig(self);

        if (self.activateCamoTimer <= 0 || self.reachedCamoToggle || !CanToggleCamo(self) || self.isCamo == wasCamo)
        {
            return;
        }

        _camoStates.GetOrCreateValue(self).Value = self.isCamo;

        foreach (var player in FriendPlayers(self))
        {
            _camoStates.GetOrCreateValue(player).Value = self.isCamo;

            if (player.isCamo != self.isCamo && !IsActivatingCamo(player) && (self.isCamo ? CanEnterCamo(player) : CanToggleCamo(player)))
            {
                player.ToggleCamo();
            }
        }
    }

    [HookPatch(typeof(On.Player), nameof(On.Player.CamoUpdate))]
    private static void On_Player_CamoUpdate(On.Player.orig_CamoUpdate orig, Player self)
    {
        RainWorldGame? game = self.abstractCreature?.world?.game;

        if (game != null && self.consumedRippleFood > 0)
        {
            _rippleFoodClocks.GetOrCreateValue(self).Value = game.clock;
        }

        orig(self);
    }

    [HookPatch(typeof(On.Creature), nameof(On.Creature.Update))]
    private static void On_Creature_Update(On.Creature.orig_Update orig, Creature self, bool eu)
    {
        orig(self, eu);

        if (self is not Player player || player.room == null || player.abstractCreature == null)
        {
            return;
        }

        if (!IsActivatingCamo(player))
        {
            _sharedCamoActivations.Remove(player);
        }

        SynchronizeCamo(player);
    }
}
