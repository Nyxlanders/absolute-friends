using UnityEngine;

namespace AbsoluteFriends.Meadow;

internal class StateCache(Func<string> build)
{
    private const float Interval = 0.5f;

    private string? _value;

    private float _time;

    internal string Value
    {
        get
        {
            float now = Time.realtimeSinceStartup;

            if (_value == null || now - _time >= Interval)
            {
                _value = build();
                _time = now;
            }

            return _value;
        }
    }

    internal void Clear() => _value = null;
}
