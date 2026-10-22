using AbsoluteFriends.Utils;
using Menu;
using Menu.Remix.MixedUI;
using UnityEngine;

namespace AbsoluteFriends.Manager;

internal partial class FriendManagerDialog
{
    private class TabButton : OpSimpleButton
    {
        private readonly FriendManagerDialog _dialog;

        private string _title = string.Empty;

        private bool _active;

        private bool _isTop;

        private bool _isBottom;

        internal TabButton(FriendManagerDialog dialog) : base(Vector2.zero, new Vector2(TabWidth, 60f), string.Empty)
        {
            _dialog = dialog;
            _rect.hiddenSide = DyeableRect.HiddenSide.Right;
            _rectH.hiddenSide = DyeableRect.HiddenSide.Right;
            _label.rotation = -90f;
            OnClick += delegate { _dialog.ShowTab(Represented); };
        }

        internal FriendTab? Represented { get; private set; }

        public override string DisplayDescription() => Translation.Of("Show <PLACEHOLDER>.").FillPlaceholders(_title);

        public override void Update()
        {
            base.Update();

            if (MenuMouseMode || !Focused || CtlrInput.y == 0 || LastCtlrInput.y == CtlrInput.y)
            {
                return;
            }

            if (_isTop && CtlrInput.y > 0)
            {
                _dialog.ScrollTabs(-1);
            }
            else if (_isBottom && CtlrInput.y < 0)
            {
                _dialog.ScrollTabs(1);
            }
        }

        public override void GrafUpdate(float timeStacker)
        {
            base.GrafUpdate(timeStacker);

            float grow = _active ? 1f : bumpBehav.AddSize;

            _label.x = -grow * 4f + 15f;
            _label.y = 6f;
            _rect.addSize = new Vector2(8f, 4f) * grow;
            _rect.pos.x = -_rect.addSize.x * 0.5f;
            _rectH.addSize = new Vector2(4f, -4f) * grow;
            _rectH.pos.x = -_rectH.addSize.x * 0.5f;
            _rect.GrafUpdate(timeStacker);
            _rectH.GrafUpdate(timeStacker);

            float glow = MouseOver || Focused ? (0.5f + 0.5f * bumpBehav.Sin(10f)) * grow : 0f;

            for (int index = 0; index < 8; index++)
            {
                _rectH.sprites[index].alpha = _active ? 1f : glow;
            }
        }

        internal void Represent(FriendTab tab, string title, bool active)
        {
            Represented = tab;
            _title = title;
            _active = active;
        }

        internal void Place(Vector2 position, float height, bool isTop, bool isBottom)
        {
            _isTop = isTop;
            _isBottom = isBottom;
            _pos = position;
            _size = new Vector2(TabWidth, height - 6f);
            Change();

            _label.text = Trim(_title, height - 16f);
            _label.alignment = FLabelAlignment.Left;
            Snap(this);
        }
    }

    private class TabScrollButton : OpSimpleImageButton
    {
        internal TabScrollButton(Vector2 position, bool up) : base(position, new Vector2(TabWidth, 20f), "Big_Menu_Arrow")
        {
            _size = new Vector2(TabWidth, 20f);
            sprite.rotation = up ? 0f : 180f;
            sprite.scale = 0.5f;
            sprite.x = TabWidth * 0.5f;
            sprite.y = 10f;
        }

        public override bool CurrentlyFocusableNonMouse => false;

        public override void Change()
        {
            base.Change();

            _size = new Vector2(TabWidth, 20f);
        }

        public override void GrafUpdate(float timeStacker)
        {
            base.GrafUpdate(timeStacker);

            _rect.Hide();
            _rectH.Hide();
            sprite.color = bumpBehav.GetColor(MenuColorEffect.rgbMediumGrey);
        }
    }
}
