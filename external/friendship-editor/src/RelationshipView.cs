using AbsoluteFriends.Manager;
using AbsoluteFriends.Options;

namespace AbsoluteFriends.FriendshipEditor;

internal class RelationshipView(FriendTab tab, RainWorldGame game, float y) : CreatureView(tab, game, y, Stat.Relationship)
{
    internal override string Title => "Relationship";

    internal static bool IsAvailableFor(AbstractCreature friend) => Config.RelationshipEditor.IsActive && IsFriendTarget(friend);

    internal override bool IsVisibleFor(AbstractCreature friend) => IsAvailableFor(friend);

    protected override bool Knows(AbstractCreature creature, AbstractCreature target) => Representation(creature, target) != null;

    protected override void ShowRow(CreatureRow row, AbstractCreature creature, AbstractCreature target, float y)
    {
        CreatureTemplate.Relationship? tracked = Representation(creature, target)?.dynamicRelationship?.currentRelationship;

        row.ShowRelationship(creature, tracked ?? RuleEffects.RelationshipFor(creature, target) ?? creature.creatureTemplate.CreatureRelationship(target.creatureTemplate), tracked != null, y);
    }

    private static Tracker.CreatureRepresentation? Representation(AbstractCreature creature, AbstractCreature target) =>
        creature.abstractAI?.RealAI?.tracker?.RepresentationForCreature(target, false);
}
