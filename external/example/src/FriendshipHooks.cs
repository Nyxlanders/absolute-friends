using AbsoluteFriends.Core;
using AbsoluteFriends.Diagnostics;
using AbsoluteFriends.Hooks;
using AbsoluteFriends.Options;
using AbsoluteFriends.Utils;
using Mono.Cecil.Cil;
using MonoMod.Cil;

namespace AbsoluteFriendsExample;

internal class FriendshipHooks : DownpourHooks
{
    // Hook groups use any active option by default. Override IsOptionEnabled when customized condition is needed.
    protected override Configurable<bool>[] Options => [AddonConfig.Friendship];

    // Override Subject to give failure notifications a clearer title than the option label or hook group name.
    // protected override string? Subject => "Hunter Daddy";

    // Override the default Any check when the hook group requires every listed option to be active.
    protected override bool IsOptionEnabled => Options.All(option => option.IsActive);

    [HookPatch(typeof(On.ArtificialIntelligence), nameof(On.ArtificialIntelligence.StaticRelationship))]
    // The On_Type_Method and IL_Type_Method names are a readability convention; HookPatch binds any valid method name.
    private static CreatureTemplate.Relationship On_ArtificialIntelligence_StaticRelationship(On.ArtificialIntelligence.orig_StaticRelationship orig, ArtificialIntelligence self, AbstractCreature otherCreature)
    {
        return self.creature.IsHunterDaddy && self.creature.IsFriend(otherCreature, direct: true)
            ? new(CreatureTemplate.Relationship.Type.Pack, 1f)
            : orig(self, otherCreature);
    }

    [HookPatch(typeof(IL.DaddyAI), nameof(IL.DaddyAI.Update))]
    // HookTest verifies where this IL hook changed the original method; anchors may be added for a stronger baseline.
    [HookTest([0], ["ldarg.0; call ArtificialIntelligence::get_noiseTracker"])]
    private static void IL_DaddyAI_Update(ILContext il)
    {
        ILCursor c = new(il);

        c.Emit(OpCodes.Ldarg_0);
        // EmitGuarded reports an exception from the delegate and disables its hook group instead of disrupting later updates.
        c.EmitGuarded((DaddyAI self) => UpdateTracking(self.creature));
    }

    // In Rain Meadow, a creature's AI runs only in the game of the player who controls it, so DaddyAI.Update never runs here for another player's Hunter Long Legs.
    // HasAuthority is false for those copies, so this update tracks them instead. HasAuthority is always true outside Rain Meadow.
    [HookPatch(typeof(On.DaddyLongLegs), nameof(On.DaddyLongLegs.Update))]
    private static void On_DaddyLongLegs_Update(On.DaddyLongLegs.orig_Update orig, DaddyLongLegs self, bool eu)
    {
        orig(self, eu);

        if (!self.HasAuthority)
        {
            UpdateTracking(self.abstractCreature);
        }
    }

    private static void UpdateTracking(AbstractCreature abstractCreature)
    {
        if (!abstractCreature.IsHunterDaddy)
        {
            return;
        }

        // Core registers only creatures with a vanilla FriendTracker (lizards, squidcadas, slugpups) on its own.
        // Hunter Long Legs has none, so this addon registers it while it is a player's friend and removes it otherwise.
        // Track() only registers: IsTracked also requires friendship eligibility and every tracking rule to agree.
        // Core clears registrations when the world changes and drops creatures deleted while alive, so call Track() repeatedly, as these updates do.
        // Registered creatures are TrackingCandidates, so the Manager lists them and can untrack or nickname them.
        // IsFriendOfPlayer checks every player in PlayerUtils.AllPlayers, which in Rain Meadow also holds other players' slugcats; use AllPlayers rather than game.Players for the same reason.
        // The Manager hides Forget for creatures without a vanilla FriendTracker, because these hooks would register them again at once.
        if (AddonConfig.Friendship.IsActive && abstractCreature.IsFriendOfPlayer)
        {
            abstractCreature.Track();
        }
        else
        {
            // Untrack() also removes a creature that Core or another addon registered.
            abstractCreature.Untrack();
        }
    }

    [HookPatch(typeof(On.ArtificialIntelligence), nameof(On.ArtificialIntelligence.CurrentPlayerAggression))]
    private static float On_ArtificialIntelligence_CurrentPlayerAggression(On.ArtificialIntelligence.orig_CurrentPlayerAggression orig, ArtificialIntelligence self, AbstractCreature player)
    {
        return self.creature.IsHunterDaddy && self.creature.IsFriend(player) ? 0f : orig(self, player);
    }
}
