using AbsoluteFriends.Manager;
using AbsoluteFriends.Options;
using AbsoluteFriends.Utils;
using Menu.Remix.MixedUI;
using UnityEngine;

namespace AbsoluteFriends.FriendshipEditor;

internal class ReputationView : EditorView
{
    private readonly CreatureCommunities _communities;

    private readonly OpComboBox _community;

    private readonly OpComboBox? _region;

    private readonly ScrollArea _list;

    private readonly List<ReputationRow> _rows = [];

    internal ReputationView(FriendTab tab, RainWorldGame game, CreatureCommunities communities, float y) : base(tab, game)
    {
        float width = tab.Size.x;

        _communities = communities;

        EditorUi.Label(Add, Translation.Of("Community"), new Vector2(0f, y), StatRow.LabelWidth);
        _community = EditorUi.Combo(Add, Communities().Prepend((Rules.All, Translation.Of("Every Community"))), Rules.All, new Vector2(StatRow.LabelWidth, y), width - StatRow.LabelWidth,
            Translation.Of("Choose the community to set rules for."));
        _community.OnValueUpdate += delegate { ChangeSubject(); };
        y -= StatRow.Height;

        if (game.IsStorySession)
        {
            EditorUi.Label(Add, Translation.Of("Region"), new Vector2(0f, y), StatRow.LabelWidth);
            _region = EditorUi.Combo(Add, Regions().Prepend((Rules.All, Translation.Of("Every Region"))), Rules.All, new Vector2(StatRow.LabelWidth, y), width - StatRow.LabelWidth,
                Translation.Of("Choose the region to set rules for."));
            _region.OnValueUpdate += delegate { ChangeSubject(); };
            y -= StatRow.Height;
        }

        y = AddStat(Stat.Reputation, y);
        y = AddRuleList(y);
        _list = EditorUi.List(Add, width, Translation.Of(game.IsStorySession ? "Regional Reputation" : "Community Reputation"), y);

        foreach (var (key, name) in game.IsStorySession ? Regions() : Communities())
        {
            _rows.Add(new ReputationRow(tab, _list, key, name));
        }

        _list.ContentHeight = _rows.Count * ReputationRow.Height;
    }

    internal override string Title => "Reputation";

    private int ReputationPlayer => Game.IsStorySession ? 0 : PlayerNumber(Target);

    private CreatureCommunities.CommunityID ShownCommunity => _community.value == Rules.All ? CreatureCommunities.CommunityID.All : new(_community.value);

    internal static bool IsAvailableFor(AbstractCreature friend) =>
        Config.ReputationEditor.IsActive
        && friend.IsPlayer
        && friend.state is PlayerState state
        && RainWorldUtils.CurrentGame is { session.creatureCommunities: { } communities } game
        && (game.IsStorySession ? state.playerNumber == 0 : state.playerNumber < communities.playerOpinions.GetLength(2));

    internal override bool IsVisibleFor(AbstractCreature friend) => IsAvailableFor(friend);

    protected override string TargetOf(AbstractCreature friend) => RuleEffects.ReputationKey(Game, Rules.All, Rules.All, PlayerNumber(friend)).Target;

    protected override RuleKey KeyOf(Stat stat) => RuleEffects.ReputationKey(Game, _community.value, _region?.value ?? Rules.All, ReputationPlayer);

    protected override string RuleName(RuleKey key) => Game.IsStorySession ? $"{CommunityName(key.Subject)} - {RegionName(key.Region)}" : CommunityName(key.Subject);

    protected override void ShowKey(RuleKey key)
    {
        EditorUi.ShowValue(_community, key.Subject);

        if (_region != null)
        {
            EditorUi.ShowValue(_region, key.Region);
        }
    }

    protected override void RefreshList(AbstractCreature target)
    {
        float y = _list.ContentHeight;

        foreach (var row in _rows)
        {
            y -= ReputationRow.Height;
            row.Show(y, Game.IsStorySession ? Saved(ShownCommunity, RegionIndex(row.Key)) : Saved(new(row.Key), -1));
        }

        _list.MarkDirty(0.5f);
    }

    protected override void Apply() => RuleEffects.ApplyReputation(Game);

    private static int PlayerNumber(AbstractCreature? friend) => friend?.state is PlayerState state ? state.playerNumber : 0;

    private static string CommunityName(string community) => community == Rules.All ? Translation.Of("Every Community") : community;

    private static string RegionName(string region) => region switch
    {
        Rules.All => Translation.Of("Every Region"),
        RuleEffects.GlobalRegion => "Global",
        _ => region,
    };

    private IEnumerable<(string Key, string Name)> Communities()
    {
        foreach (var name in CreatureCommunities.CommunityID.values.entries)
        {
            if (name != CreatureCommunities.CommunityID.None.value && new CreatureCommunities.CommunityID(name).Index - 1 < _communities.playerOpinions.GetLength(0))
            {
                yield return (name, name);
            }
        }
    }

    private IEnumerable<(string Key, string Name)> Regions()
    {
        string[] regions = Game.rainWorld.progression.regionNames;
        SlugcatStats.Name? slugcat = (Game.session as StoryGameSession)?.saveStateNumber;

        yield return (RuleEffects.GlobalRegion, "Global");

        for (int region = 0; region < regions.Length && region + 1 < _communities.playerOpinions.GetLength(1); region++)
        {
            yield return (regions[region], $"{regions[region]}  {Translation.Of(Region.GetRegionFullName(regions[region], slugcat))}");
        }
    }

    private int RegionIndex(string key) => Array.IndexOf(Game.rainWorld.progression.regionNames, key);

    private float Saved(CreatureCommunities.CommunityID community, int region) => _communities.playerOpinions[community.Index - 1, region + 1, ReputationPlayer];
}
