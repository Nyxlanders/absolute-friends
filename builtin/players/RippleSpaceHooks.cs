using System.Runtime.CompilerServices;
using AbsoluteFriends.Core;
using AbsoluteFriends.Hooks;
using AbsoluteFriends.Utils;

namespace AbsoluteFriends.Players;

internal class RippleSpaceHooks : WatcherHooks
{
    private static readonly ConditionalWeakTable<Creature, StrongBox<int>> _shownRippleLayers = new();

    protected override Configurable<bool>[] Options => [Config.RippleSpace];

    private static void SynchronizeRippleLayer(Creature creature)
    {
        int rippleLayer = creature.abstractCreature.rippleLayer;
        List<AbstractCreature> abstractPlayers = [.. creature.room.game.AllPlayers];

        if (abstractPlayers.All(abstractPlayer => abstractPlayer?.rippleLayer == rippleLayer) || !creature.IsTracked)
        {
            return;
        }

        if (LeadingPlayer(creature, abstractPlayers) is { } leader && leader.abstractCreature.rippleLayer != rippleLayer)
        {
            creature.ChangeRippleLayer(leader.abstractCreature.rippleLayer);

            RevealInRippleSpace(creature.room.game, creature);
        }
    }

    private static void RevealRemoteRippleLayer(Creature creature)
    {
        int rippleLayer = creature.abstractCreature.rippleLayer;
        StrongBox<int> shown = _shownRippleLayers.GetValue(creature, _ => new(rippleLayer));

        if (shown.Value == rippleLayer)
        {
            return;
        }

        shown.Value = rippleLayer;

        if (creature.IsTracked)
        {
            RevealInRippleSpace(creature.room.game, creature);
        }
    }

    private static Player? LeadingPlayer(Creature creature, List<AbstractCreature> abstractPlayers)
    {
        Player? nearest = null;
        Player? stayed = null;
        Player? other = null;
        float nearestDistance = float.MaxValue;

        foreach (var abstractPlayer in abstractPlayers)
        {
            if (abstractPlayer?.realizedCreature is not Player friendPlayer || !creature.IsFriend(friendPlayer))
            {
                continue;
            }

            if (friendPlayer.room == creature.room && friendPlayer.mainBodyChunk != null && creature.mainBodyChunk != null)
            {
                float distance = (friendPlayer.mainBodyChunk.pos - creature.mainBodyChunk.pos).sqrMagnitude;

                if (distance < nearestDistance)
                {
                    nearest = friendPlayer;
                    nearestDistance = distance;
                }
            }
            else if (abstractPlayer.rippleLayer == creature.abstractCreature.rippleLayer)
            {
                stayed ??= friendPlayer;
            }
            else
            {
                other ??= friendPlayer;
            }
        }

        return nearest ?? stayed ?? other;
    }

    private static void RevealInRippleSpace(RainWorldGame? game, PhysicalObject target)
    {
        foreach (var camera in game?.cameras ?? [])
        {
            foreach (var sLeaser in camera?.spriteLeasers ?? [])
            {
                RevealInRippleSpace(sLeaser, target);
            }
        }
    }

    private static void RevealInRippleSpace(RoomCamera.SpriteLeaser sLeaser, PhysicalObject? target = null)
    {
        PhysicalObject? owner = sLeaser.DrawnObject;

        if (target == null ? !owner.IsSlugcat && owner.IsFriendOfPlayer && owner.IsTracked : owner == target)
        {
            foreach (var sprite in sLeaser?.sprites ?? [])
            {
                if (sprite?.shader is { } shader)
                {
                    if (RainWorldUtils.Shader(shader.name + "BothSides") is { } bothSides)
                    {
                        sprite.shader = bothSides;
                    }
                    else if (shader == FShader.defaultShader && RainWorldUtils.Shader("RippleBasicBothSides") is { } rippleBasicBothSides)
                    {
                        sprite.shader = rippleBasicBothSides;
                    }
                }
            }
        }
    }

    [HookPatch(typeof(On.Creature), nameof(On.Creature.Update))]
    private static void On_Creature_Update(On.Creature.orig_Update orig, Creature self, bool eu)
    {
        orig(self, eu);

        if (self is Player || self.room == null || self.abstractCreature == null)
        {
            return;
        }

        if (self.HasAuthority)
        {
            SynchronizeRippleLayer(self);
        }
        else
        {
            RevealRemoteRippleLayer(self);
        }
    }

    [HookPatch(typeof(On.RoomCamera.SpriteLeaser), nameof(On.RoomCamera.SpriteLeaser.ctor))]
    private static void On_SpriteLeaser_ctor(On.RoomCamera.SpriteLeaser.orig_ctor orig, RoomCamera.SpriteLeaser self, IDrawable obj, RoomCamera rCam)
    {
        orig(self, obj, rCam);

        RevealInRippleSpace(self);
    }

    [HookPatch(typeof(On.RoomCamera.SpriteLeaser), nameof(On.RoomCamera.SpriteLeaser.UpdatePalette))]
    private static void On_SpriteLeaser_UpdatePalette(On.RoomCamera.SpriteLeaser.orig_UpdatePalette orig, RoomCamera.SpriteLeaser self, RoomCamera rCam, RoomPalette palette)
    {
        orig(self, rCam, palette);

        RevealInRippleSpace(self);
    }
}
