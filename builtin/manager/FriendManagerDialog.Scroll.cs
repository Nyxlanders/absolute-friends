using Menu;
using Menu.Remix.MixedUI;
using UnityEngine;

namespace AbsoluteFriends.Manager;

internal partial class FriendManagerDialog
{
    private class ListScrollBar : UIelement
    {
        internal const float Width = 30f;

        internal const float LineOverhang = 25f;

        private const float KnobRadius = 5f;

        private readonly FriendManagerDialog _dialog;

        private readonly FSprite _knob;

        private readonly FSprite _lower;

        private readonly FSprite _upper;

        private bool _dragging;

        private bool _wasMouseDown;

        internal ListScrollBar(FriendManagerDialog dialog, Vector2 position, float height) : base(position, new Vector2(Width, height))
        {
            _dialog = dialog;
            _knob = new FSprite("Menu_Subtle_Slider_Nob")
            {
                x = Width * 0.5f
            };
            _lower = Line();
            _upper = Line();

            myContainer.AddChild(_lower);
            myContainer.AddChild(_upper);
            myContainer.AddChild(_knob);
        }

        private float? Fraction => _dialog._scrollBox is { MaxScroll: < 0f } scrollBox
            ? Mathf.Clamp01(scrollBox.scrollOffset / scrollBox.MaxScroll)
            : null;

        private bool KnobHovered => Fraction is { } fraction && (MousePos - new Vector2(Width * 0.5f, fraction * size.y)).magnitude <= KnobRadius + 3f;

        public override void Update()
        {
            base.Update();

            bool mouseDown = Input.GetMouseButton(0);

            if (!MenuMouseMode || !mouseDown || _dialog._scrollBox is not { MaxScroll: < 0f } scrollBox)
            {
                _dragging = false;
            }
            else
            {
                if (!_wasMouseDown && KnobHovered)
                {
                    _dragging = true;
                }

                if (_dragging)
                {
                    scrollBox.targetScrollOffset = Mathf.Clamp01(MousePos.y / size.y) * scrollBox.MaxScroll;
                    scrollBox.scrollOffset = scrollBox.targetScrollOffset;
                }
            }

            _wasMouseDown = mouseDown;
        }

        public override void GrafUpdate(float timeStacker)
        {
            base.GrafUpdate(timeStacker);

            _lower.y = -LineOverhang;

            if (Fraction is not { } fraction)
            {
                _knob.isVisible = false;
                _upper.isVisible = false;
                _lower.scaleY = size.y + LineOverhang * 2f;

                return;
            }

            float knobY = fraction * size.y;

            _knob.isVisible = true;
            _knob.y = knobY;
            _knob.color = _dragging || (MenuMouseMode && KnobHovered) ? MenuColorEffect.rgbWhite : MenuColorEffect.rgbMediumGrey;
            _lower.scaleY = LineOverhang + knobY - KnobRadius;
            _upper.isVisible = true;
            _upper.y = knobY + KnobRadius;
            _upper.scaleY = size.y + LineOverhang - _upper.y;
        }

        private static FSprite Line() => new("pixel")
        {
            anchorY = 0f,
            scaleX = 2f,
            x = Width * 0.5f,
            color = MenuColorEffect.rgbMediumGrey
        };
    }
}
