using AbsoluteFriends.Hooks;
using AbsoluteFriends.Utils;
using Menu.Remix;
using Menu.Remix.MixedUI;
using RWCustom;
using UnityEngine;

namespace AbsoluteFriends.Options;

public abstract partial class MenuBuilder : OptionInterface
{
    public const float Padding = 30f;

    private const float CanvasSize = 600f;

    private const float Spacing = 24f;

    private const float Gap = 6f;

    private static readonly Vector2 _defaultMarginX = new(Padding, CanvasSize - Padding);

    private static readonly Vector2[] _focusDirections = [Vector2.left, Vector2.up, Vector2.right, Vector2.down];

    private readonly List<UIelement> _elements = [];

    private readonly List<ConfigurableBase> _options = [];

    private readonly Dictionary<ConfigurableBase, OptionEntry> _entries = [];

    private readonly Dictionary<ConfigurableBase, Action> _propagates = [];

    private readonly List<Action> _externals = [];

    private readonly List<OpRadioButton> _radioButtons = [];

    private readonly List<Section> _sections = [];

    private OpTab? _currentTab;

    private OpRadioButtonGroup? _radioButtonGroup;

    private Section? _section;

    private SectionFilter? _filter;

    private Configurable<bool>? _buttonRequirement;

    private OpRect? _box;

    private int _boxIndex;

    private int _headerIndex = -1;

    private float _headerBottom;

    private Vector2 _marginX = _defaultMarginX;

    private Vector2 _pos = new();

    private float _edge = CanvasSize;

    private int _columns = 4;

    private float _currentColumn = 0f;

    private float Width => _marginX.y - _marginX.x;

    private float ElementWidth => Width / _columns;

    public override void Initialize()
    {
        base.Initialize();

        _currentTab = null;
        _radioButtonGroup = null;
        _section = null;
        _filter = null;
        _buttonRequirement = null;
        _box = null;
        _marginX = _defaultMarginX;
        _headerIndex = -1;

        _elements.Clear();
        _options.Clear();
        _entries.Clear();
        _propagates.Clear();
        _externals.Clear();
        _radioButtons.Clear();
        _sections.Clear();
    }

    public override void Update()
    {
        base.Update();

        foreach (var external in _externals)
        {
            external();
        }

        foreach (var option in _options)
        {
            if (option is not Configurable<bool> boolOption || !_entries.TryGetValue(option, out OptionEntry entry) || entry.Focusable is not OpCheckBox checkBox)
            {
                continue;
            }

            if (HookManager.IsFailed(boolOption))
            {
                checkBox.greyedOut = true;
                checkBox.symbolSprite.isVisible = false;

                if (checkBox.colorEdge != Palette.Error)
                {
                    Mark(entry, Palette.Error, Translate("This feature ran into an error and disabled for this session. Restart the game to try it again."), failed: true);
                    Propagate(option);
                }
            }
            else if (HookManager.IsWarned(boolOption) && checkBox.colorEdge != Palette.Error && checkBox.colorEdge != Palette.Warning)
            {
                Mark(entry, Palette.Warning, Translate("This feature was applied, but not the way it expected, so it may not be working."));
            }
        }
    }

    internal Configurable<T> Bind<T>(string key, T defaultValue, ConfigurableInfo? info = null)
    {
        return config.configurables.TryGetValue(key, out ConfigurableBase configurable) && configurable is Configurable<T> bound
            ? bound
            : config.Bind(key, defaultValue, info);
    }

    internal void WithButtonRequirement(Configurable<bool>? requirement, Action build)
    {
        Configurable<bool>? previous = _buttonRequirement;

        _buttonRequirement = requirement;

        try
        {
            build();
        }
        finally
        {
            _buttonRequirement = previous;
        }
    }

    protected void BeginTab(OpTab? tab)
    {
        EndTab();

        _currentTab = tab;
        _pos = new(_marginX.x, CanvasSize);
        _edge = CanvasSize;
        _currentColumn = 0;

        AddRow();
    }

    protected void EndTab()
    {
        if (_currentTab == null)
        {
            return;
        }

        EndRadioButtonGroup();
        EndSection();
        FinishRow();

        UIelement[] elements = [.. _elements];
        UIelement[] header = _headerIndex < 0 ? [] : [.. elements.Take(_headerIndex)];
        UIelement[] body = _headerIndex < 0 ? elements : [.. elements.Skip(_headerIndex)];
        float view = _headerIndex < 0 ? CanvasSize : _headerBottom;
        float height = view - _pos.y + Spacing * 0.5f;
        OpScrollBox? scrollBox = null;

        _elements.Clear();

        if (height > view)
        {
            Vector2 offset = new(0f, height - view);

            foreach (var element in body)
            {
                element?.SetPos(element.GetPos() + offset);
            }

            scrollBox = new(new Vector2(0f, 0f), new Vector2(CanvasSize, view), height, false, false, true);
            _currentTab.AddItems(scrollBox);
            scrollBox.AddItems(body);
            Dictionary<UIfocusable, UIfocusable?[]> links = [];

            scrollBox.OnUpdate += () =>
            {
                FocusScrollChild(scrollBox, body);
                LinkScrollChild(scrollBox, body, header, links);
            };
        }
        else
        {
            _currentTab.AddItems(body);
        }

        if (header.Length > 0)
        {
            _currentTab.AddItems(header);
        }

        foreach (var element in elements)
        {
            Snap(element);
        }

        if (_sections.Count > 0)
        {
            if (_filter is { } filter)
            {
                foreach (var section in _sections)
                {
                    section.Anchor();
                }

                filter.Bind(this, scrollBox, height, view);
                _externals.Add(filter.UpdateLayout);
            }

            _sections.Clear();
        }

        _filter = null;
        _headerIndex = -1;
        _currentTab = null;
    }

    private protected void PinHeader()
    {
        if (_currentTab == null)
        {
            return;
        }

        if (_currentColumn > 0)
        {
            AddRow(0.75f);
        }

        _headerIndex = _elements.Count;
        _headerBottom = _pos.y;
        _edge = _pos.y + Spacing * 0.5f;
        _pos.y -= Spacing * 0.5f;
    }

    private protected void BeginFold(Func<bool> unfolded)
    {
        if (_section is not { } section || _box is not { } box)
        {
            return;
        }

        FinishRow();
        _pos.y = _edge - Spacing;
        section.Fold(box, unfolded, _elements.Count - section.Index, _edge - Gap);
    }

    private protected void BeginSection(string? name = null, string? alias = null, Func<bool>? gate = null)
    {
        BeginSection([name, alias], gate);
    }

    private protected void BeginSection(string?[] names, Func<bool>? gate = null)
    {
        EndSection();

        if (_currentTab == null)
        {
            return;
        }

        _section = new(names, _elements.Count, _pos.y, gate);
    }

    private protected void Watch(Action update)
    {
        _externals.Add(update);
    }

    private static void Snap(UIelement element)
    {
        element.lastScreenPos = element.ScreenPos;
    }

    private static void FocusScrollChild(OpScrollBox scrollBox, UIelement[] body)
    {
        if (!ReferenceEquals(ConfigContainer.FocusedElement, scrollBox)
            || scrollBox.held
            || ReferenceEquals(ConfigContainer.lastFocusedElement?.scrollBox, scrollBox)
            || Custom.rainWorld?.processManager?.menuesMouseMode == true)
        {
            return;
        }

        UIfocusable? firstVisible = body.OfType<UIfocusable>()
            .FirstOrDefault(item => IsNavigable(item) && OpScrollBox.IsChildVisible(item))
            ?? body.OfType<UIfocusable>().FirstOrDefault(IsNavigable);

        if (firstVisible != null)
        {
            ConfigConnector.FocusNewElement(firstVisible);
        }
    }

    private static void LinkScrollChild(OpScrollBox scrollBox, UIelement[] body, UIelement[] header, Dictionary<UIfocusable, UIfocusable?[]> links)
    {
        if (ConfigContainer.FocusedElement is not { InScrollBox: true } focused
            || !ReferenceEquals(focused.scrollBox, scrollBox)
            || Custom.rainWorld?.processManager?.menuesMouseMode == true)
        {
            return;
        }

        if (!links.TryGetValue(focused, out UIfocusable?[] written))
        {
            written = links[focused] = new UIfocusable?[_focusDirections.Length];
        }

        UIfocusable[] children = [.. body.OfType<UIfocusable>().Where(IsNavigable)];
        Vector2 center = CenterOf(focused);
        Vector2 top = scrollBox.GetPos() + new Vector2(center.x, scrollBox.size.y);

        for (int index = 0; index < _focusDirections.Length; index++)
        {
            if (focused.NextFocusable[index] is { } link && link != written[index])
            {
                continue;
            }

            Vector2 direction = _focusDirections[index];
            UIfocusable next = FocusNavigation.Nearest(focused, center, children, CenterOf, direction)
                ?? (direction == Vector2.up ? FocusNavigation.Nearest(focused, top, header.OfType<UIfocusable>().Where(IsNavigable), CenterOf, direction) : null)
                ?? (direction == Vector2.left ? TabButton() : null)
                ?? focused;

            focused.NextFocusable[index] = written[index] = next;
        }
    }

    private static UIfocusable? TabButton()
    {
        return ConfigContainer.menuTab?.tabCtrler?.GetCurrentTabButton() is { } button && IsNavigable(button) ? button : null;
    }

    private static bool IsNavigable(UIfocusable element) => !element.IsInactive && element.CurrentlyFocusableNonMouse;

    private static Vector2 CenterOf(UIelement element) => element.GetPos() + (element.isRectangular ? element.size * 0.5f : Vector2.one * element.rad);

    private void EndSection()
    {
        if (_section is not { } section)
        {
            return;
        }

        EndBox();
        section.Close([.. _elements.Skip(section.Index)], _pos.y);
        _sections.Add(section);
        _section = null;
    }

    private void EndRadioButtonGroup()
    {
        if (_radioButtonGroup == null)
        {
            return;
        }

        _radioButtonGroup.SetButtons([.. _radioButtons]);
        _currentTab?.AddItems(_radioButtonGroup);
        _radioButtonGroup = null;
        _radioButtons.Clear();
    }

}
