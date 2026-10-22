using AbsoluteFriends.Manager;
using AbsoluteFriends.Utils;
using Menu.Remix.MixedUI;
using UnityEngine;

namespace AbsoluteFriends.FriendshipEditor;

internal class EditorTab
{
    private const string TabId = Plugin.GUID + ".editor";

    private readonly FriendTab _tab;

    private readonly List<EditorView> _views = [];

    private readonly OpComboBox _view;

    private EditorView? _shown;

    private bool _choosing;

    private EditorTab(FriendTab tab, RainWorldGame game)
    {
        _tab = tab;

        float width = tab.Size.x;
        float y = tab.Size.y - EditorUi.RowHeight;
        float viewY = y - StatRow.Height;

        if (game.session.creatureCommunities is { } communities)
        {
            _views.Add(new ReputationView(tab, game, communities, viewY));
        }

        _views.Add(new LikeView(tab, game, viewY));
        _views.Add(new RelationshipView(tab, game, viewY));

        EditorUi.Label(tab.Add, Translation.Of("View"), new Vector2(0f, y), StatRow.LabelWidth);
        _view = EditorUi.Combo(tab.Add, Items(_views), _views[0].Title, new Vector2(StatRow.LabelWidth, y), width - StatRow.LabelWidth,
            Translation.Of("Choose what to edit."));
        _view.OnValueUpdate += delegate
        {
            if (!_choosing)
            {
                ChooseView();
            }
        };

        tab.FriendChanged += _ => ChooseView();
        tab.Shown += ChooseView;
        tab.Refresh += Refresh;
        tab.Update += Update;
    }

    internal static void Register() => FriendManager.RegisterTab(TabId, "Friendship", Build, IsVisibleFor);

    internal static void Unregister() => FriendManager.UnregisterTab(TabId);

    private static bool IsVisibleFor(AbstractCreature friend) =>
        ReputationView.IsAvailableFor(friend) || LikeView.IsAvailableFor(friend) || RelationshipView.IsAvailableFor(friend);

    private static void Build(FriendTab tab)
    {
        if (RainWorldUtils.CurrentGame is { } game)
        {
            _ = new EditorTab(tab, game);
        }
    }

    private static IEnumerable<(string Key, string Name)> Items(IEnumerable<EditorView> views) => views.Select(view => (view.Title, Translation.Of(view.Title)));

    private void ChooseView()
    {
        if (_tab.Friend is not { } friend || !_tab.IsActive)
        {
            return;
        }

        List<EditorView> views = [.. _views.Where(view => view.IsVisibleFor(friend))];

        if (views.Count == 0)
        {
            Show(null);

            return;
        }

        _choosing = true;
        EditorUi.SetItems(_view, Items(views), _view.value);
        _choosing = false;
        Show(views.Find(view => view.Title == _view.value) ?? views[0]);
    }

    private void Show(EditorView? view)
    {
        foreach (var other in _views.Where(other => other != view))
        {
            other.SetShown(false);
        }

        view?.SetShown(true);
        _shown = view;
        view?.Reload();
    }

    private void Refresh() => _shown?.Refresh();

    private void Update() => EditorUi.KeepOnTop([_view, .. _shown?.Elements ?? []]);
}
