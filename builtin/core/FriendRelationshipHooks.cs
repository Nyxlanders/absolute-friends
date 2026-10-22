using AbsoluteFriends.Diagnostics;
using AbsoluteFriends.Hooks;
using MonoMod.Cil;

namespace AbsoluteFriends.Core;

internal class FriendRelationshipHooks : BaseHooks
{
    protected override Configurable<bool>[] Options => [Config.FriendSlugcat, Config.FriendCreature, Config.FriendNeutralCreature, Config.FriendChaining];

    protected override bool IsOptionEnabled => base.IsOptionEnabled || CreatureRules.HasConfiguredRules;

    protected override string? Subject => "Relationship";

    private static bool ShouldNeutralize(CreatureTemplate.Relationship.Type type)
    {
        return type != CreatureTemplate.Relationship.Type.DoesntTrack
            && type != CreatureTemplate.Relationship.Type.Ignores
            && type != CreatureTemplate.Relationship.Type.PlaysWith
            && type != CreatureTemplate.Relationship.Type.SocialDependent
            && type != CreatureTemplate.Relationship.Type.Pack;
    }

    [HookPatch(typeof(IL.AbstractCreatureAI), nameof(IL.AbstractCreatureAI.MigrationBehavior))]
    [HookTest([20], ["ldarg.0; ldnull; stfld AbstractCreatureAI::followCreature"])]
    private static void IL_AbstractCreatureAI_MigrationBehavior(ILContext il)
    {
        ILCursor c = new(il);

        if (c.TryGotoNext(
            i => i.MatchCallOrCallvirt<SocialMemory>(nameof(SocialMemory.DiscardRelationship))
        ))
        {
            c.Remove();
            c.EmitGuarded((SocialMemory socialMemory, EntityID subjectID) =>
            {
                if (!subjectID.IsKnownSlugcat)
                {
                    socialMemory.DiscardRelationship(subjectID);
                }
            }, (socialMemory, subjectID) => socialMemory.DiscardRelationship(subjectID));
        }
    }

    [HookPatch(typeof(On.RelationshipTracker.DynamicRelationship), nameof(On.RelationshipTracker.DynamicRelationship.Update))]
    private static void On_DynamicRelationship_Update(On.RelationshipTracker.DynamicRelationship.orig_Update orig, RelationshipTracker.DynamicRelationship self)
    {
        if (self is not { rt: { AI: { creature: { } source } ai } relationshipTracker, trackerRep: { representedCreature: { } target } trackerRep }
            || !source.IsFriend(target, direct: true)
            || ai is not IUseARelationshipTracker relationshipUser)
        {
            orig(self);
            self.RecordVanillaRelationship(self.currentRelationship);

            return;
        }

        CreatureTemplate.Relationship relationship = relationshipUser.UpdateDynamicRelationship(self);

        self.RecordVanillaRelationship(relationship);

        if (ShouldNeutralize(relationship.type))
        {
            relationship = new(CreatureTemplate.Relationship.Type.Ignores, FriendUtils.FriendLikeThreshold);
        }

        if (relationship.type != self.currentRelationship.type)
        {
            relationshipTracker.SortCreatureIntoModule(self, relationship);
        }

        trackerRep.priority = relationship.intensity * self.trackedByModuleWeigth;
        self.currentRelationship = relationship;
    }
}
