using AbsoluteFriends.Manager;
using Menu.Remix.MixedUI;

namespace AbsoluteFriends.FriendshipEditor;

internal abstract class EditorView(FriendTab tab, RainWorldGame game)
{
    private readonly List<UIelement> _elements = [];

    private readonly List<StatRow> _stats = [];

    private RuleList? _rules;

    private Rules? _loaded;

    internal FriendTab Tab { get; } = tab;

    internal abstract string Title { get; }

    internal IEnumerable<UIelement> Elements => _elements;

    protected RainWorldGame Game { get; } = game;

    protected AbstractCreature? Target => Tab.Friend;

    protected int StatCount => _stats.Count;

    internal void Add(UIelement element)
    {
        Tab.Add(element);
        _elements.Add(element);
    }

    internal void SetShown(bool shown)
    {
        foreach (var element in _elements)
        {
            EditorUi.SetShown(element, shown);
        }
    }

    internal abstract bool IsVisibleFor(AbstractCreature friend);

    internal void Reload() => Load(null, true);

    internal void Refresh()
    {
        if (Target is not { } target)
        {
            return;
        }

        Rules rules = RuleStore.For(Game);

        if (FriendManager.IsReadOnly && _rules != null && rules != _loaded)
        {
            Load(null, false);

            return;
        }

        foreach (var row in _stats)
        {
            row.Refresh(rules.Resolve(KeyOf(row.Stat)));
        }

        RefreshList(target);
        _rules?.Show(ConfiguredRules(target));
    }

    protected float AddStat(Stat stat, float y)
    {
        _stats.Add(new StatRow(this, stat, y));

        return y - StatRow.Height;
    }

    protected float AddRuleList(float y)
    {
        _rules = new RuleList(this, y, ApplyRules, RemoveRule, key => Load(key, false));

        return y - RuleList.Height;
    }

    protected abstract string TargetOf(AbstractCreature friend);

    protected abstract RuleKey KeyOf(Stat stat);

    protected abstract string RuleName(RuleKey key);

    protected abstract void ShowKey(RuleKey key);

    protected abstract void RefreshList(AbstractCreature target);

    protected void ChangeSubject() => Load(null, false);

    protected virtual void Apply()
    {
    }

    protected virtual void Loaded()
    {
    }

    private bool Owns(RuleKey key, AbstractCreature friend) => _stats.Any(row => row.Stat == key.Stat) && key.Target == TargetOf(friend);

    private void Load(RuleKey? selection, bool fallback)
    {
        if (Target is not { } target || _rules == null)
        {
            return;
        }

        Rules rules = RuleStore.For(Game);

        _loaded = rules;

        List<(RuleKey Key, string Text)> configured = [.. ConfiguredRules(target)];
        RuleKey? shown = selection ?? _rules.Choose([.. configured.Select(rule => rule.Key)], _stats.Select(row => KeyOf(row.Stat)), fallback);

        if (shown is { } chosen)
        {
            ShowKey(chosen);
        }

        foreach (var row in _stats)
        {
            RuleKey key = KeyOf(row.Stat);

            row.SetModes(key.Subject == Rules.All && key.Region == Rules.All);
            row.Load(rules.Exact(key));
        }

        Refresh();
        _rules.Select(configured, shown);
        Loaded();
    }

    private IEnumerable<(RuleKey Key, string Text)> ConfiguredRules(AbstractCreature target) => RuleStore.For(Game)
        .Matching(key => Owns(key, target))
        .OrderBy(rule => rule.Key.Subject != Rules.All)
        .ThenBy(rule => rule.Key.Subject, StringComparer.Ordinal)
        .ThenBy(rule => rule.Key.Region != Rules.All)
        .ThenBy(rule => rule.Key.Region, StringComparer.Ordinal)
        .ThenBy(rule => rule.Key.Stat)
        .Select(rule => (rule.Key, $"{RuleName(rule.Key)}: {EditorUi.RuleText(rule.Rule)}"));

    private void ApplyRules()
    {
        if (Target == null || FriendManager.IsReadOnly)
        {
            return;
        }

        Rules rules = RuleStore.For(Game);

        foreach (var row in _stats)
        {
            if (row.EditedRule is not { } rule || rule.IsValid(row.Stat))
            {
                rules.Set(KeyOf(row.Stat), row.EditedRule);
            }
        }

        Save();
    }

    private void RemoveRule(RuleKey key)
    {
        if (FriendManager.IsReadOnly)
        {
            return;
        }

        RuleStore.For(Game).Set(key, null);
        Save();
    }

    private void Save()
    {
        RuleStore.Save(Game);
        Apply();
        Load(null, false);
    }
}
