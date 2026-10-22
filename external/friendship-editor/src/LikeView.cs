using AbsoluteFriends.Manager;
using AbsoluteFriends.Options;

namespace AbsoluteFriends.FriendshipEditor;

internal class LikeView(FriendTab tab, RainWorldGame game, float y) : CreatureView(tab, game, y, Stat.Like, Stat.TempLike, Stat.Know)
{
    internal override string Title => "Like";

    internal static bool IsAvailableFor(AbstractCreature friend) => Config.LikeEditor.IsActive && IsFriendTarget(friend);

    internal override bool IsVisibleFor(AbstractCreature friend) => IsAvailableFor(friend);

    protected override bool Knows(AbstractCreature creature, AbstractCreature target) => Memory(creature, target) != null;

    protected override void ShowRow(CreatureRow row, AbstractCreature creature, AbstractCreature target, float y) =>
        row.ShowLikes(creature, Memory(creature, target) is { } memory ? (memory.like, memory.tempLike, memory.know) : null, y);

    protected override void Apply() => RuleEffects.ApplyLikes(Game);

    private static SocialMemory.Relationship? Memory(AbstractCreature creature, AbstractCreature target) => creature.state?.socialMemory?.GetRelationship(target.ID);
}
