using AbsoluteFriends.Options;
using Menu;
using Menu.Remix;
using Menu.Remix.MixedUI;
using RWCustom;
using UnityEngine;

namespace AbsoluteFriends.Manager;

internal partial class FriendManagerDialog
{
    private void UpdateCursor()
    {
        Rect? target = selectedObject is UIelementWrapper { thisElement: UIfocusable focusable } wrapper && wrapper.CurrentlySelectableNonMouse
            ? FocusRectOf(focusable)
            : null;

        _cursor.Update(target, manager.menuesMouseMode, selectedObject is UIelementWrapper { thisElement: UIfocusable { held: true } });
    }

    private static Rect FocusRectOf(UIfocusable focusable)
    {
        Rect rect = focusable.FocusRect;

        if (!focusable.InScrollBox)
        {
            return rect;
        }

        OpScrollBox scrollBox = focusable.scrollBox;
        Vector2 camera = scrollBox._camPos - (scrollBox.horizontal ? Vector2.right : Vector2.up) * scrollBox.scrollOffset - scrollBox.pos;
        Rect view = scrollBox.FocusRect;

        rect.x -= camera.x;
        rect.y -= camera.y;
        rect.width = Mathf.Max(Mathf.Min(rect.width, view.width, view.x + view.width - rect.x, rect.x + rect.width - view.x), 0f);
        rect.height = Mathf.Max(Mathf.Min(rect.height, view.height, view.y + view.height - rect.y, rect.y + rect.height - view.y), 0f);
        rect.x = Mathf.Clamp(rect.x, view.x, view.x + view.width);
        rect.y = Mathf.Clamp(rect.y, view.y, view.y + view.height);

        return rect;
    }

    private void KeepHeldSelected()
    {
        if (manager.menuesMouseMode && HeldElement != null && selectedObject is UIelementWrapper held && held.page.selectables is { } selectables && selectables.IndexOf(held) > 0)
        {
            selectables.Remove(held);
            selectables.Insert(0, held);
        }
    }

    private void LinkFocus()
    {
        List<MenuObject> content = [.. ContentElements()
            .Select(WrapperOf)
            .OfType<UIelementWrapper>()
            .Where(wrapper => wrapper.CurrentlySelectableNonMouse)];
        MenuObject? row = SelectedRow()?.Focus ?? _rows.FirstOrDefault()?.Focus;
        MenuObject? activeButton = _tabButtons
            .Where(button => !button.Hidden && button.Represented == _activeTab)
            .Select(WrapperOf)
            .FirstOrDefault();
        MenuObject? entry = content
            .OrderByDescending(wrapper => CenterOf(wrapper).y)
            .ThenBy(wrapper => CenterOf(wrapper).x)
            .FirstOrDefault();

        foreach (var wrapper in content)
        {
            wrapper.nextSelectable[0] = Nearest(wrapper, content, Vector2.left) ?? activeButton ?? row ?? wrapper;
            wrapper.nextSelectable[1] = Nearest(wrapper, content, Vector2.up) ?? wrapper;
            wrapper.nextSelectable[2] = Nearest(wrapper, content, Vector2.right) ?? wrapper;
            wrapper.nextSelectable[3] = Nearest(wrapper, content, Vector2.down) ?? wrapper;
        }

        foreach (var friendRow in _rows)
        {
            friendRow.Focus.nextSelectable[2] = activeButton ?? entry ?? friendRow.Focus;
        }

        List<UIelementWrapper> buttons = [.. _tabButtons
            .Where(button => !button.Hidden)
            .Select(WrapperOf)
            .OfType<UIelementWrapper>()];

        for (int index = 0; index < buttons.Count; index++)
        {
            UIelementWrapper button = buttons[index];

            button.nextSelectable[0] = row ?? button;
            button.nextSelectable[1] = index > 0 ? buttons[index - 1] : button;
            button.nextSelectable[2] = Nearest(button, content, Vector2.right) ?? entry ?? button;
            button.nextSelectable[3] = index < buttons.Count - 1 ? buttons[index + 1] : button;
        }
    }

    private IEnumerable<UIelement> ContentElements() => _activeTab?.Elements ?? [];

    private MenuObject? Nearest(MenuObject from, List<MenuObject> candidates, Vector2 direction) => FocusNavigation.Nearest(from, CenterOf(from), candidates, CenterOf, direction);

    private Vector2 CenterOf(MenuObject menuObject) => menuObject is UIelementWrapper { thisElement: { } element } and not RowWrapper
        ? element.ScreenPos + element.size * 0.5f
        : CenterPositionOfObject((PositionedMenuObject)menuObject);

    private class FocusCursor
    {
        private readonly FSprite[] _sprites = new FSprite[4];

        private Vector2 _pos;

        private Vector2 _lastPos;

        private Vector2 _size;

        private Vector2 _lastSize;

        private float _fade = 1f;

        private float _lastFade = 1f;

        private bool _held;

        private bool _placed;

        internal FocusCursor(FContainer container)
        {
            for (int index = 0; index < _sprites.Length; index++)
            {
                _sprites[index] = new FSprite("modMenuCursor0")
                {
                    rotation = 90f * index,
                    anchorX = 1f,
                    anchorY = 1f,
                    color = MenuColorEffect.rgbMediumGrey
                };
                container.AddChild(_sprites[index]);
            }
        }

        internal void Update(Rect? target, bool mouseMode, bool held)
        {
            _lastPos = _pos;
            _lastSize = _size;
            _lastFade = _fade;
            _held = held;
            _fade = mouseMode || target == null
                ? Custom.LerpAndTick(_fade, 1f, 0.2f, 0.1f / UIelement.frameMulti)
                : Custom.LerpAndTick(_fade, 0f, 0.6f, 0.1f / UIelement.frameMulti);

            if (target is not { } rect)
            {
                return;
            }

            Vector2 pos = new(rect.x, rect.y);
            Vector2 size = new(rect.width, rect.height);

            if (!_placed)
            {
                _placed = true;
                _pos = _lastPos = pos;
                _size = _lastSize = size;

                return;
            }

            _pos = Vector2.Lerp(_pos, pos, 0.6f / UIelement.frameMulti);
            _size = Vector2.Lerp(_size, size, 0.6f / UIelement.frameMulti);
        }

        internal void GrafUpdate(float timeStacker)
        {
            Vector2 pos = Vector2.Lerp(_lastPos, _pos, timeStacker);
            Vector2 size = Vector2.Lerp(_lastSize, _size, timeStacker);
            Vector2[] corners = [pos, pos + new Vector2(0f, size.y), pos + size, pos + new Vector2(size.x, 0f)];
            FAtlasElement element = Futile.atlasManager.GetElementWithName(_held ? "modMenuCursor1" : "modMenuCursor0");

            for (int index = 0; index < _sprites.Length; index++)
            {
                _sprites[index].element = element;
                _sprites[index].alpha = 1f - Mathf.Lerp(_lastFade, _fade, timeStacker);
                _sprites[index].x = corners[index].x + 0.01f;
                _sprites[index].y = corners[index].y + 0.01f;
            }
        }

        internal void Remove()
        {
            foreach (var sprite in _sprites)
            {
                sprite.RemoveFromContainer();
            }
        }
    }
}
