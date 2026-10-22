using Menu.Remix.MixedUI;
using UnityEngine;

namespace AbsoluteFriends.Options;

public class ScrollArea : OpScrollBox
{
    public ScrollArea(Vector2 position, Vector2 size) : base(position, size, size.y, false, false, true)
    {
        doesBackBump = false;
        ScrollLocked = true;
    }

    public float ContentHeight
    {
        get => contentSize;
        set
        {
            float fromTop = targetScrollOffset - MaxScroll;

            if (SetContentSize(value))
            {
                ScrollOffset = Mathf.Clamp(MaxScroll + fromTop, MaxScroll, 0f);
            }

            ScrollLocked = !Overflows;
        }
    }

    public bool Overflows => contentSize > size.y;

    public float Top => contentSize;

    public override void GrafUpdate(float timeStacker)
    {
        base.GrafUpdate(timeStacker);

        if (Overflows)
        {
            _rectSlidebar?.Show();
        }
        else
        {
            _rectSlidebar?.Hide();
        }
    }
}
