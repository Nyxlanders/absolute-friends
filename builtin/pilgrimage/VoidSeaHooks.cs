using System.Runtime.CompilerServices;
using AbsoluteFriends.Diagnostics;
using AbsoluteFriends.Hooks;
using AbsoluteFriends.Utils;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using UnityEngine;
using VoidSea;
using Watcher;

namespace AbsoluteFriends.Pilgrimage;

internal class VoidSeaHooks : BaseHooks
{
    private static readonly ConditionalWeakTable<Creature, RestrictionState> _restrictions = new();

    private static readonly ConditionalWeakTable<Player, object> _forcedVoidFlags = new();

    protected override Configurable<bool>[] Options => [Config.VoidSea];

    [HookPatch(typeof(On.Creature), nameof(On.Creature.Update))]
    private static void On_Creature_Update(On.Creature.orig_Update orig, Creature self, bool eu)
    {
        VoidSeaScene? scene = VoidSeaUtils.CompanionScene(self);

        if (scene != null || (self is Player rider && VoidSeaUtils.IsWormRider(rider)) || WeaverVoidUtils.IsInWeaverVoid(self))
        {
            RestrictionState restrictions = _restrictions.GetValue(self, creature => new RestrictionState(creature));

            foreach (var chunk in self.bodyChunks)
            {
                chunk.restrictInRoomRange = float.MaxValue;
            }

            if (scene != null && self.mainBodyChunk.pos.y < VoidSeaUtils.DepthHeight)
            {
                restrictions.DisableTerrain(self);
            }
            else
            {
                restrictions.RestoreTerrain(self);
            }
        }
        else if (_restrictions.TryGetValue(self, out RestrictionState restrictions))
        {
            restrictions.Restore(self);

            _restrictions.Remove(self);
        }

        orig(self, eu);
    }

    [HookPatch(typeof(On.AirBreatherCreature), nameof(On.AirBreatherCreature.Update))]
    private static void On_AirBreatherCreature_Update(On.AirBreatherCreature.orig_Update orig, AirBreatherCreature self, bool eu)
    {
        bool inVoidSea = VoidSeaUtils.CompanionScene(self) != null;

        if (inVoidSea)
        {
            self.lungs = 1f;
        }

        orig(self, eu);

        if (inVoidSea)
        {
            self.lungs = 1f;
        }
    }

    [HookPatch(typeof(On.Creature), nameof(On.Creature.Die))]
    private static void On_Creature_Die(On.Creature.orig_Die orig, Creature self)
    {
        if (self.Submersion <= 0f || VoidSeaUtils.CompanionScene(self) == null)
        {
            orig(self);
        }
    }

    [HookPatch(typeof(On.Player), nameof(On.Player.Update))]
    private static void On_Player_Update(On.Player.orig_Update orig, Player self, bool eu)
    {
        bool isProtected = (self.isNPC && VoidSeaUtils.CompanionScene(self) != null) || VoidSeaUtils.IsWormRider(self);

        if (isProtected)
        {
            if (!self.inVoidSea)
            {
                self.inVoidSea = true;

                _forcedVoidFlags.GetValue(self, _ => new());
            }

            self.airInLungs = 1f;
        }
        else if (_forcedVoidFlags.TryGetValue(self, out _) && VoidSeaUtils.ActiveScene(self.room) == null)
        {
            self.inVoidSea = false;

            _forcedVoidFlags.Remove(self);
        }

        orig(self, eu);

        if (isProtected)
        {
            self.airInLungs = 1f;
        }
    }

    [HookPatch(typeof(On.RainWorldGame), nameof(On.RainWorldGame.BeatGameMode))]
    private static void On_RainWorldGame_BeatGameMode(On.RainWorldGame.orig_BeatGameMode orig, RainWorldGame game, bool standardVoidSea)
    {
        bool isWatcherAscension = ModManager.Watcher
            && game.StoryCharacter == WatcherEnums.SlugcatStatsName.Watcher
            && game.rainWorld.progression.miscProgressionData.watcherEndingID == 4;

        if ((standardVoidSea || isWatcherAscension) && game.session is StoryGameSession { saveState: { } saveState })
        {
            saveState.KeepEndingCompanions(game);
        }

        orig(game, standardVoidSea);
    }

    [HookPatch(typeof(On.LizardGraphics), nameof(On.LizardGraphics.Update))]
    private static void On_LizardGraphics_Update(On.LizardGraphics.orig_Update orig, LizardGraphics self)
    {
        if (VoidSeaUtils.CompanionScene(self.lizard) == null)
        {
            orig(self);

            return;
        }

        float cullRange = self.cullRange;

        try
        {
            self.cullRange = 0f;

            orig(self);
        }
        finally
        {
            self.cullRange = cullRange;
        }
    }

    [HookPatch(typeof(On.VoidSea.VoidSeaScene), nameof(On.VoidSea.VoidSeaScene.Move))]
    private static void On_VoidSeaScene_Move(On.VoidSea.VoidSeaScene.orig_Move orig, VoidSeaScene self, Player player, Vector2 delta, bool moveCamera)
    {
        VoidSeaState state = self.State;
        int clock = self.room.game.clock;

        if (state.Clock != clock)
        {
            state.Clock = clock;
            state.MovedBy.Clear();
        }

        Creature[] companions = [.. self.GatherCompanions().Where(friend =>
            state.MovedBy.TryGetValue(friend.abstractCreature, out Player mover)
                ? mover == player
                : VoidSeaUtils.FollowedPlayer(friend, state) == player
        )];

        orig(self, player, delta, moveCamera);

        foreach (var companion in companions)
        {
            if (companion.room == self.room)
            {
                state.MovedBy[companion.abstractCreature] = player;

                VoidSeaUtils.Shift(companion, delta);
            }
        }
    }

    [HookPatch(typeof(On.VoidSea.VoidSeaScene), nameof(On.VoidSea.VoidSeaScene.Update))]
    private static void On_VoidSeaScene_Update(On.VoidSea.VoidSeaScene.orig_Update orig, VoidSeaScene self, bool eu)
    {
        orig(self, eu);

        if (self.Inverted)
        {
            return;
        }

        float velocityFactor = Mathf.Lerp(0.95f, 1f, RainWorldUtils.MainCamera(self.room.game)?.voidSeaGoldFilter ?? 0f);
        VoidSeaState state = self.State;
        Creature[] companions = self.GatherCompanions();
        HashSet<Creature> riders = [];
        bool wormRiding = self.IsWormRiding;

        VoidSeaUtils.FollowWormRide(self, state, companions, riders);

        foreach (var friend in companions)
        {
            if (friend is not Player)
            {
                foreach (var chunk in friend.bodyChunks)
                {
                    chunk.vel *= velocityFactor;
                    chunk.vel.y += friend.gravity - friend.buoyancy;
                }
            }

            bool isCarried = friend.IsCarried;

            if (wormRiding && !isCarried && friend.mainBodyChunk.pos.y < VoidSeaUtils.DepthHeight)
            {
                VoidSeaUtils.StopRising(friend);
            }

            if (!isCarried && !riders.Contains(friend) && VoidSeaUtils.NearestDiver(friend, state) is { } diver)
            {
                VoidSeaUtils.FollowDiver(friend, diver);
            }
        }

        VoidSeaUtils.AttachThreads(self, state, companions);
    }

    private class RestrictionState(Creature creature)
    {
        private readonly float[] _ranges = [.. creature.bodyChunks.Select(chunk => chunk.restrictInRoomRange)];

        private readonly bool[] _terrainCollisions = [.. creature.bodyChunks.Select(chunk => chunk.collideWithTerrain)];

        private bool _terrainDisabled;

        public void DisableTerrain(Creature self)
        {
            foreach (var chunk in self.bodyChunks)
            {
                chunk.collideWithTerrain = false;
            }

            _terrainDisabled = true;
        }

        public void RestoreTerrain(Creature self)
        {
            if (!_terrainDisabled)
            {
                return;
            }

            for (int i = 0; i < self.bodyChunks.Length; ++i)
            {
                self.bodyChunks[i].collideWithTerrain = i >= _terrainCollisions.Length || _terrainCollisions[i];
            }

            _terrainDisabled = false;
        }

        public void Restore(Creature self)
        {
            for (int i = 0; i < self.bodyChunks.Length; ++i)
            {
                self.bodyChunks[i].restrictInRoomRange = i < _ranges.Length ? _ranges[i] : self.bodyChunks[i].defaultRestrictInRoomRange;
            }

            RestoreTerrain(self);
        }
    }
}

internal class WatcherVoidSeaHooks : WatcherHooks
{
    protected override Configurable<bool>[] Options => [Config.VoidSea];

    [HookPatch(typeof(On.Player), nameof(On.Player.Update))]
    private static void On_Player_Update(On.Player.orig_Update orig, Player self, bool eu)
    {
        if (self is { isNPC: true, inWeaverVoid: true } && !WeaverVoidUtils.IsWeaverRoom(self.room))
        {
            self.inWeaverVoid = false;
        }

        orig(self, eu);
    }

    [HookPatch(typeof(On.Watcher.WarpPoint), nameof(On.Watcher.WarpPoint.ChangeState))]
    private static void On_WarpPoint_ChangeState(On.Watcher.WarpPoint.orig_ChangeState orig, WarpPoint self, WarpPoint.State nextState)
    {
        orig(self, nextState);

        if (nextState == WarpPoint.State.EnterWarp && self.currentState == WarpPoint.State.EnterWarp && self.IsVoidWarp)
        {
            self.BringCompanions();
        }
    }

    [HookPatch(typeof(IL.Watcher.WarpPoint), nameof(IL.Watcher.WarpPoint.SuckInCreatures))]
    [HookTest([84], ["brtrue; ldloc.s; ldfld PhysicalObject::abstractPhysicalObject"])]
    private static void IL_WarpPoint_SuckInCreatures(ILContext il)
    {
        ILCursor c = new(il);
        int physicalObject = -1;

        if (c.TryGotoNext(
            MoveType.After,
            i => i.MatchLdloc(out physicalObject),
            i => i.MatchIsinst<Player>(),
            i => i.MatchBrtrue(out _),
            i => i.MatchLdarg(0),
            i => i.MatchLdfld<WarpPoint>("canWarpToVoidWeaverEnding")
        ))
        {
            c.Emit(OpCodes.Ldloc, physicalObject);
            c.EmitGuarded((bool canWarpToVoidWeaverEnding, PhysicalObject physicalObject) => canWarpToVoidWeaverEnding && physicalObject is not Creature { abstractCreature.IsCompanion: true }, (canWarpToVoidWeaverEnding, _) => canWarpToVoidWeaverEnding);
        }
    }
}
