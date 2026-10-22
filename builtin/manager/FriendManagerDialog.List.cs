using AbsoluteFriends.Core;
using AbsoluteFriends.Utils;
using Menu.Remix;
using Menu.Remix.MixedUI;
using UnityEngine;

namespace AbsoluteFriends.Manager;

internal partial class FriendManagerDialog
{
    internal void RemoveElement(UIelementWrapper wrapper)
    {
        UIelement element = wrapper.thisElement;

        if (element.InScrollBox)
        {
            OpScrollBox.RemoveItemsFromScrollBox([element]);
        }

        _wrapper._tab.RemoveItems([element]);
        _wrapper.wrappers.Remove(element);
        _wrapper.RemoveSubObject(wrapper);
        wrapper.page.selectables.Remove(wrapper);
        wrapper.glow?.sprite.RemoveFromContainer();

        if (selectedObject == wrapper)
        {
            selectedObject = null;
        }

        element.Unload();
    }

    private static List<AbstractCreature> CollectCreatures() => [.. FriendUtils.TrackingCandidates
        .OrderBy(creature => creature.IsPlayer ? 0 : creature.IsSlugpup ? 1 : 2)
        .ThenBy(creature => creature.creatureTemplate?.name, StringComparer.Ordinal)
        .ThenBy(creature => creature.ID.number)];

    private static float ContentHeight(int count) => Mathf.Max(ListHeight, count * RowSpacing + 20f);

    private void RefreshMembership()
    {
        List<AbstractCreature> creatures = CollectCreatures();

        if (_rows.Select(row => row.Creature).SequenceEqual(creatures))
        {
            return;
        }

        AbstractCreature? selected = _selectedFriend;
        float scroll = _scrollBox?.targetScrollOffset ?? 0f;

        BuildList(creatures, scroll);

        if (selected != null && creatures.Contains(selected))
        {
            RefreshTab();
        }
        else
        {
            SelectFriend(creatures.FirstOrDefault());
        }
    }

    private void BuildList(List<AbstractCreature> creatures, float scroll)
    {
        bool focusedInList = selectedObject is RowWrapper;

        ClearList();

        _contentHeight = ContentHeight(creatures.Count);

        _scrollBox = new(_listPos, new Vector2(ListWidth, ListHeight), _contentHeight, false, false, false)
        {
            doesBackBump = false
        };
        _scrollWrapper = new(_wrapper, _scrollBox);
        _scrollWrapper.glow?.Hide();
        _scrollWrapper.page.selectables.Remove(_scrollWrapper);
        _selectionBox = AddSelectionBox();

        for (int index = 0; index < creatures.Count; index++)
        {
            AbstractCreature creature = creatures[index];
            float y = RowY(index);
            (string symbol, Color color) = creature.Icon;
            OpLabel name = new(new Vector2(RowInset + 36f, y + RowSpacing * 0.5f - 15f), new Vector2(RowWidth - 36f, 30f), creature.FriendName, FLabelAlignment.Left)
            {
                color = creature.ShortcutColor
            };
            RowButton button = new(new Vector2(RowInset, y + 2f), new Vector2(RowWidth, RowSpacing - 4f))
            {
                description = Translation.Of("Show this friend's details.")
            };
            List<UIelement> elements = [name];

            button.Clicked += () => ClickRow(creature);
            OpImage? icon = null;

            if (Futile.atlasManager.DoesContainElementWithName(symbol))
            {
                icon = new(new Vector2(RowInset + 16f, y + RowSpacing * 0.5f), symbol)
                {
                    anchor = new(0.5f, 0.5f),
                    color = color
                };

                elements.Add(icon);
            }

            _scrollBox.AddItems([button, .. elements]);

            RowWrapper focus = new(_wrapper, button, _scrollBox);
            UIelementWrapper[] wrappers = [focus, .. elements.Select(element => new UIelementWrapper(_wrapper, element))];

            if (icon != null)
            {
                FitIcon(icon);
            }

            _rows.Add(new(creature, button, focus, name, icon, wrappers));
        }

        LinkRows();

        HighlightSelection(true);

        if (focusedInList)
        {
            selectedObject = SelectedRow()?.Focus;
        }

        _scrollBox.targetScrollOffset = Mathf.Clamp(scroll, _scrollBox.MaxScroll, 0f);
        _emptyLabel.label.text = creatures.Count == 0 ? Translation.Of("No trackable friends") : string.Empty;
    }

    private void ClearList()
    {
        foreach (var row in _rows)
        {
            foreach (var wrapper in row.Wrappers)
            {
                RemoveElement(wrapper);
            }
        }

        _rows.Clear();

        if (_selectionBox != null && WrapperOf(_selectionBox) is { } selectionWrapper)
        {
            RemoveElement(selectionWrapper);
        }

        _selectionBox = null;

        if (_scrollWrapper != null)
        {
            RemoveElement(_scrollWrapper);
            _scrollWrapper = null;
            _scrollBox = null;
        }
    }

    private float RowY(int index) => _contentHeight - 10f - (index + 1) * RowSpacing;

    private OpRect AddSelectionBox()
    {
        OpRect box = new(new Vector2(RowInset, 0f), new Vector2(RowWidth, RowSpacing - 4f), HighlightAlpha);

        _scrollBox!.AddItems(box);
        _ = new UIelementWrapper(_wrapper, box);
        box.Hide();

        return box;
    }

    private void LinkRows()
    {
        for (int index = 0; index < _rows.Count; index++)
        {
            RowWrapper focus = _rows[index].Focus;

            focus.nextSelectable[0] = focus;
            focus.nextSelectable[1] = _rows[(index + _rows.Count - 1) % _rows.Count].Focus;
            focus.nextSelectable[3] = _rows[(index + 1) % _rows.Count].Focus;
        }
    }

    private FriendRow? SelectedRow() => _rows.Find(row => row.Creature == _selectedFriend);

    private void SelectFriend(AbstractCreature? creature)
    {
        _selectedFriend = creature;

        HighlightSelection(false);
        UpdateTabs();
        RefreshTab();
    }

    private void HighlightSelection(bool snap)
    {
        if (_selectionBox == null)
        {
            return;
        }

        int index = _rows.FindIndex(row => row.Creature == _selectedFriend);

        if (index < 0)
        {
            _selectionBox.Hide();

            return;
        }

        _selectionTarget = RowY(index) + 2f;

        if (snap || _selectionBox.Hidden)
        {
            _selectionY = _lastSelectionY = _selectionTarget;
        }

        _selectionBox.Show();
    }

    private void UpdateList()
    {
        _lastSelectionY = _selectionY;
        _selectionY = Mathf.Lerp(_selectionY, _selectionTarget, SelectionEase);

        _lastFocusedRow = manager.menuesMouseMode ? -1 : FollowFocus(_rows.FindIndex(row => row.Button.Focused));
    }

    private int FollowFocus(int focused)
    {
        if (focused < 0)
        {
            return -1;
        }

        FriendRow row = _rows[focused];

        if (_lastFocusedRow < 0 && SelectedRow() is { } current && current != row)
        {
            selectedObject = current.Focus;
            row = current;
        }
        else if (row.Creature != _selectedFriend)
        {
            SelectFriend(row.Creature);
        }

        OpScrollBox.ScrollToChild(row.Button, true);

        return _rows.IndexOf(row);
    }

    private void ClickRow(AbstractCreature creature)
    {
        if (creature != _selectedFriend)
        {
            SelectFriend(creature);
        }
    }

    private void PlaceSelection(float timeStacker)
    {
        _selectionBox?.SetPos(new Vector2(RowInset, Mathf.Lerp(_lastSelectionY, _selectionY, timeStacker)));
    }

    private void RefreshRows()
    {
        foreach (var row in _rows)
        {
            row.Name.text = row.Creature.FriendName;
            row.Name.color = row.Creature.ShortcutColor;

            row.Icon?.color = row.Creature.Icon.Color;
        }
    }

    private class FriendRow(AbstractCreature creature, RowButton button, RowWrapper focus, OpLabel name, OpImage? icon, UIelementWrapper[] wrappers)
    {
        internal AbstractCreature Creature { get; } = creature;

        internal RowButton Button { get; } = button;

        internal RowWrapper Focus { get; } = focus;

        internal OpLabel Name { get; } = name;

        internal OpImage? Icon { get; } = icon;

        internal UIelementWrapper[] Wrappers { get; } = wrappers;
    }
}
