using AbsoluteFriends.Diagnostics;
using AbsoluteFriends.Utils;
using Menu;
using Menu.Remix;
using Menu.Remix.MixedUI;
using UnityEngine;

namespace AbsoluteFriends.Manager;

internal partial class FriendManagerDialog : Dialog
{
    private const float RowSpacing = 30f;

    private const float ListWidth = 250f;

    private const float ListHeight = 650f;

    private const float RowInset = 2f;

    private const float RowWidth = ListWidth - RowInset * 2f;

    private const float CanvasSize = 630f;

    private const float TabAreaHeight = 600f;

    private const float CanvasInset = 15f;

    private const float TopPadding = 12f;

    private const float VerticalOffset = 35f;

    private const float TransitionOffset = CanvasSize + 180f;

    private const float TransitionDuration = 0.3f;

    private const float ShadeAlpha = 0.75f;

    private const float IconSize = 24f;

    private const float HighlightAlpha = 0.15f;

    private const float SelectionEase = 0.35f;

    private const int MaxVisibleTabs = 8;

    private const float TabWidth = 30f;

    private static readonly AddonLogger _logger = Reporter.GetLogger(Plugin.Name);

    private readonly RainWorldGame _game;

    private readonly MenuTabWrapper _wrapper;

    private readonly FContainer _pageContainer;

    private readonly List<FriendRow> _rows = [];

    private readonly List<FriendTab> _tabs = [];

    private readonly List<FriendTab> _shownTabs = [];

    private readonly List<TabButton> _tabButtons = [];

    private readonly MenuLabel _emptyLabel;

    private readonly FocusCursor _cursor;

    private readonly Vector2 _listPos;

    private OpScrollBox? _scrollBox;

    private float _pendingScroll;

    private int _tickFrame = -1;

    private UIelementWrapper? _scrollWrapper;

    private OpRect? _selectionBox;

    private FriendTab? _activeTab;

    private AbstractCreature? _selectedFriend;

    private int _showDelay = 2;

    private bool _pausedGame;

    private bool _previousPaused;

    private bool _closing;

    private float _nextDetailsRefresh;

    private float _nextMembershipRefresh;

    private float _lastOpenProgress;

    private float _openProgress;

    private float _contentHeight;

    private float _selectionY;

    private float _lastSelectionY;

    private float _selectionTarget;

    private int _lastFocusedRow = -1;

    private TabScrollButton? _tabScrollUp;

    private TabScrollButton? _tabScrollDown;

    private int _tabTop;

    private float _tabColumnX;

    private float _tabColumnTop;

    internal FriendManagerDialog(ProcessManager manager, RainWorldGame game) : base(manager)
    {
        _game = game;
        container.isVisible = false;

        Vector2 screen = manager.rainWorld.options.ScreenSize;
        float center = screen.x * 0.5f;
        Vector2 remix = new(center - 683f, screen.y - 768f - VerticalOffset);
        Vector2 canvasPos = remix + new Vector2(543f, 105f);
        Page page = dialogPage;

        _listPos = remix + new Vector2(208f, 60f);

        _pageContainer = new()
        {
            y = TransitionOffset
        };
        container.AddChild(_pageContainer);
        page.myContainer = _pageContainer;

        darkSprite.alpha = 0f;
        darkSprite.scaleX = Futile.screen.pixelWidth + 2f;
        darkSprite.scaleY = Futile.screen.pixelHeight + 2f;

        string titleText = Translation.Of("Friend Manager");
        bool bigTitle = titleText == "Friend Manager";
        float titleY = canvasPos.y + CanvasSize + 28f;
        MenuLabel title = new(this, page, titleText, new Vector2(center - 420f, titleY - 20f), new Vector2(840f, 40f), bigTitle, default);

        title.label.scale = bigTitle ? 1.4f : 1.8f;
        page.subObjects.Add(title);

        _wrapper = new(this, page);
        page.subObjects.Add(_wrapper);

        OpRect canvas = new(canvasPos, new Vector2(CanvasSize, CanvasSize));

        _ = new UIelementWrapper(_wrapper, canvas);

        _emptyLabel = AddLabel(page, string.Empty, _listPos.x, _listPos.y + ListHeight * 0.5f, ListWidth);
        _ = new UIelementWrapper(_wrapper, new ListScrollBar(this, _listPos - new Vector2(ListScrollBar.Width, 0f), ListHeight));
        _ = new UIelementWrapper(_wrapper, new OpImage(_listPos + new Vector2(265f, -ListScrollBar.LineOverhang), "pixel")
        {
            scale = new(2f, ListHeight + ListScrollBar.LineOverhang * 2f),
            color = MenuColorEffect.rgbMediumGrey
        });

        BuildTabs(canvasPos, remix + new Vector2(520f, 120f));
        BuildList(CollectCreatures(), 0f);
        _scrollBox?.ScrollToTop(true);
        SelectFriend(_rows.FirstOrDefault()?.Creature);

        _cursor = new(_pageContainer);

        _nextMembershipRefresh = Time.unscaledTime + 1f;
    }

    internal UIfocusable? HeldElement => _wrapper is { holdElement: true } && selectedObject is UIelementWrapper { tabWrapper: var tabWrapper, thisElement: UIfocusable { held: true } held } && tabWrapper == _wrapper
        ? held
        : null;

    public override void RawUpdate(float dt)
    {
        using (InputBlocker.Allow())
        {
            base.RawUpdate(dt);
        }
    }

    public override void Update()
    {
        floatScrollWheel += _pendingScroll;
        _pendingScroll = 0f;
        _tickFrame = Time.frameCount;

        OpScrollBox? wheelTarget = WheelTarget();
        float previousScroll = wheelTarget?.targetScrollOffset ?? 0f;

        LinkFocus();
        KeepHeldSelected();
        base.Update();
        UpdateCursor();

        UpdateList();
        UpdateTab();

        bool togglePressed = FriendManagerHooks.ConsumeShortcut() || RWInput.CheckPauseButton(0);

        if (togglePressed)
        {
            _closing = true;
        }

        _lastOpenProgress = _openProgress;
        _openProgress = Mathf.Clamp01(_openProgress + (_closing ? -1f : 1f) * Time.unscaledDeltaTime / TransitionDuration);

        if (!_closing && _lastOpenProgress < 1f && _openProgress >= 1f)
        {
            _scrollBox?.ScrollToTop(true);
            _scrollBox?.MarkDirty();
        }

        if (Time.unscaledTime >= _nextMembershipRefresh)
        {
            _nextMembershipRefresh = Time.unscaledTime + 1f;

            RefreshMembership();
        }

        if (Time.unscaledTime >= _nextDetailsRefresh)
        {
            _nextDetailsRefresh = Time.unscaledTime + 0.25f;

            RefreshAll();
        }

        float wheel = mouseScrollWheelMovement * 40f;

        if (wheelTarget is { ScrollLocked: false } && wheel != 0f && !Input.GetMouseButton(0) && HeldElement == null)
        {
            wheelTarget.targetScrollOffset = Mathf.Clamp(previousScroll + wheel, wheelTarget.MaxScroll, 0f);
        }

        if (_showDelay > 0 && --_showDelay == 0)
        {
            container.isVisible = true;
        }

        if (_closing && _openProgress <= 0f)
        {
            manager.StopSideProcess(this);
        }
    }

    public override void GrafUpdate(float timeStacker)
    {
        PlaceSelection(timeStacker);
        GrafUpdateTab(timeStacker);
        _cursor.GrafUpdate(timeStacker);

        base.GrafUpdate(timeStacker);

        float progress = Mathf.Lerp(_lastOpenProgress, _openProgress, timeStacker);
        float eased = Mathf.SmoothStep(0f, 1f, progress);

        _pageContainer.y = TransitionOffset * (1f - eased);
        darkSprite.alpha = ShadeAlpha * eased;
    }

    public override void ShutDownProcess()
    {
        if (_pausedGame)
        {
            _game.paused = _previousPaused;
        }

        CloseTabs();
        _cursor.Remove();

        base.ShutDownProcess();
    }

    internal void CollectScroll()
    {
        if (_tickFrame != Time.frameCount && manager.menuesMouseMode)
        {
            _pendingScroll += Input.GetAxis("Mouse ScrollWheel");
        }
    }

    internal void PauseGame()
    {
        if (_pausedGame)
        {
            return;
        }

        _previousPaused = _game.paused;
        _game.paused = true;
        _pausedGame = true;
    }

    internal static void Snap(UIelement element)
    {
        element.lastScreenPos = element.ScreenPos;
        element.GrafUpdate(1f);
    }

    internal UIelementWrapper? WrapperOf(UIelement element) => _wrapper.wrappers.TryGetValue(element, out UIelementWrapper wrapper) ? wrapper : null;

    private OpScrollBox? WheelTarget() => ContentElements()
        .OfType<OpScrollBox>()
        .FirstOrDefault(scrollBox => !scrollBox.IsInactive && scrollBox.MouseOver) ?? _scrollBox;

    private static void SetShown(UIelement element, bool shown)
    {
        if (shown && element.Hidden)
        {
            element.Show();
            Snap(element);
        }
        else if (!shown && !element.Hidden)
        {
            element.Hide();
        }
    }

    private static void SetShown(bool shown, params UIelement[] elements)
    {
        foreach (var element in elements)
        {
            SetShown(element, shown);
        }
    }

    private static void FitIcon(OpImage icon)
    {
        Vector2 size = icon.sprite.element.sourcePixelSize;
        float scale = Mathf.Min(1f, IconSize / Mathf.Max(size.x, size.y));

        icon.scale = new(scale, scale);
    }

    private static string Trim(string text, float width) => LabelTest.GetWidth(text) > width ? LabelTest.TrimText(text, width, true) : text;

    private MenuLabel AddLabel(Page page, string text, float x, float y, float width, FLabelAlignment alignment = default)
    {
        float offset = alignment switch
        {
            FLabelAlignment.Left => -width * 0.5f,
            FLabelAlignment.Right => width * 0.5f,
            _ => 0f,
        };

        MenuLabel label = new(this, page, text, new Vector2(x + offset, y), new Vector2(width, 30f), false, default);

        label.label.alignment = alignment;

        page.subObjects.Add(label);

        return label;
    }
}
