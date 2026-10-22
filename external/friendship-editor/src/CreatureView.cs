using AbsoluteFriends.Core;
using AbsoluteFriends.Manager;
using AbsoluteFriends.Options;
using AbsoluteFriends.Utils;
using Menu.Remix.MixedUI;
using UnityEngine;

namespace AbsoluteFriends.FriendshipEditor;

internal abstract class CreatureView : EditorView
{
    private const int MaxCreatures = 100;

    private readonly OpComboBox _type;

    private readonly ScrollArea _list;

    private readonly List<CreatureRow> _creatureRows = [];

    protected CreatureView(FriendTab tab, RainWorldGame game, float y, params Stat[] stats) : base(tab, game)
    {
        float width = tab.Size.x;

        EditorUi.Label(Add, Translation.Of("Creature Type"), new Vector2(0f, y), StatRow.LabelWidth);
        _type = EditorUi.Combo(Add, Types(), Rules.All, new Vector2(StatRow.LabelWidth, y), width - StatRow.LabelWidth,
            Translation.Of("Choose the creature type to set rules for."));
        _type.OnValueUpdate += delegate { ChangeSubject(); };
        y -= StatRow.Height;

        foreach (var stat in stats)
        {
            y = AddStat(stat, y);
        }

        y = AddRuleList(y);
        _list = EditorUi.List(Add, width, Translation.Of("Loaded Creatures"), y);
    }

    private string TypeSubject => _type.value;

    protected static bool IsFriendTarget(AbstractCreature friend) => friend.IsPlayer || friend.IsTracked;

    protected abstract bool Knows(AbstractCreature creature, AbstractCreature target);

    protected abstract void ShowRow(CreatureRow row, AbstractCreature creature, AbstractCreature target, float y);

    protected override string TargetOf(AbstractCreature friend) => RuleEffects.TargetKey(friend);

    protected override RuleKey KeyOf(Stat stat) => RuleEffects.TypeKey(TypeSubject, Target!, stat);

    protected override string RuleName(RuleKey key) => StatCount > 1 ? $"{TypeName(key.Subject)} - {EditorUi.StatName(key.Stat)}" : TypeName(key.Subject);

    protected override void ShowKey(RuleKey key) => EditorUi.ShowValue(_type, key.Subject);

    protected override void RefreshList(AbstractCreature target)
    {
        ShowCreatures([.. TypeCreatures(target)
            .OrderByDescending(creature => Knows(creature, target))
            .ThenByDescending(creature => creature.realizedCreature != null)
            .ThenBy(creature => creature.creatureTemplate.type.value, StringComparer.Ordinal)
            .ThenBy(creature => creature.ID.number)], target);
        _list.MarkDirty(0.5f);
    }

    protected override void Loaded() => _list.ScrollToTop();

    private static IEnumerable<(string Key, string Name)> Types() => StaticWorld.creatureTemplates
        .Where(template => template is { virtualCreature: false } && template.type != CreatureTemplate.Type.Slugcat)
        .Select(template => template.type.value)
        .Distinct()
        .OrderBy(type => type, StringComparer.Ordinal)
        .Select(type => (type, type))
        .Prepend((Rules.All, Translation.Of("Every Creature Type")));

    private static string TypeName(string type) => type == Rules.All ? Translation.Of("Every Creature Type") : type;

    private void ShowCreatures(List<AbstractCreature> creatures, AbstractCreature target)
    {
        int shown = Mathf.Min(creatures.Count, MaxCreatures);

        while (_creatureRows.Count < shown)
        {
            _creatureRows.Add(new CreatureRow(Tab, _list));
        }

        _list.ContentHeight = shown * CreatureRow.Height;

        float y = _list.ContentHeight;

        for (int index = 0; index < _creatureRows.Count; index++)
        {
            if (index < shown)
            {
                y -= CreatureRow.Height;
                ShowRow(_creatureRows[index], creatures[index], target, y);
            }
            else
            {
                _creatureRows[index].Hide();
            }
        }
    }

    private IEnumerable<AbstractCreature> TypeCreatures(AbstractCreature target) => RuleEffects.LoadedCreatures(Game).Values
        .Where(creature => !creature.IsPlayer && creature != target && (TypeSubject == Rules.All || creature.creatureTemplate.type.value == TypeSubject));
}
