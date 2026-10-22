using Menu.Remix.MixedUI;
using UnityEngine;

namespace AbsoluteFriends.Manager;

internal class HoldButton : OpHoldButton
{
    private const float FillTime = 80f;

    private float _hold;

    private bool _done;

    internal HoldButton(Vector2 pos, Vector2 size, string displayText) : base(pos, size, displayText, FillTime)
    {
        _isProgress = true;
    }

    internal event Action? Done;

    public override void Update()
    {
        base.Update();

        if (greyedOut || !held)
        {
            _done = false;
            _hold = Mathf.Max(0f, _hold - 4f * frameMulti / FillTime);
        }
        else if (!_done)
        {
            _hold = Mathf.Min(1f, _hold + frameMulti / FillTime);

            if (_hold >= 1f)
            {
                _done = true;
                Done?.Invoke();
            }
        }

        progress = _hold * 100f;
    }
}
