using Menu.Remix;
using Menu.Remix.MixedUI;
using UnityEngine;

namespace AbsoluteFriends.Manager;

internal partial class FriendManagerDialog
{
    private class RowButton(Vector2 pos, Vector2 size) : UIfocusable(pos, size)
    {
        internal event Action? Clicked;

        public override bool CurrentlyFocusableNonMouse => IsInView;

        internal bool IsInView { get; set; }

        public override void Update()
        {
            base.Update();

            if (!MenuMouseMode)
            {
                if (held && !CtlrInput.jmp)
                {
                    held = false;
                }

                return;
            }

            if (MouseOver && Input.GetMouseButton(0))
            {
                held = true;
            }
            else if (held)
            {
                held = false;

                if (MouseOver)
                {
                    Clicked?.Invoke();
                }
            }
        }
    }

    private class RowWrapper(MenuTabWrapper tabWrapper, RowButton button, OpScrollBox scrollBox) : UIelementWrapper(tabWrapper, button)
    {
        public override void Update()
        {
            Vector2 local = button.GetPos() + new Vector2(0f, scrollBox.scrollOffset);

            pos = scrollBox.GetPos() + local;
            button.IsInView = local.y >= 0f && local.y + button.size.y <= scrollBox.size.y;

            base.Update();
        }

        public override void GrafUpdate(float timeStacker)
        {
            base.GrafUpdate(timeStacker);

            glow.pos = pos;

            if (!button.IsInView)
            {
                glow.alpha = 0f;
            }
        }
    }
}
