using UnityEngine;

namespace AbsoluteFriends.Visualizer;

internal partial class FriendMeter(FContainer container)
{
    private const float IconDistance = 30f;

    private const float RowGap = 35f;

    private const float RightInset = 15f;

    private const float CustomFadeStep = 0.05f;

    private const float OutsideFade = 1f;

    private readonly Dictionary<AbstractCreature, MeterIcon> _icons = [];

    private readonly List<AbstractCreature> _order = [];

    private float _fade;

    private float _lastFade;

    private float _customFade;

    private int _counter;

    public void Update(HUD.HUD? hud, Vector2 screenSize, IReadOnlyCollection<AbstractCreature> friends)
    {
        for (int index = _order.Count - 1; index >= 0; --index)
        {
            AbstractCreature creature = _order[index];

            if (!friends.Contains(creature))
            {
                _icons[creature].Remove();
                _icons.Remove(creature);
                _order.RemoveAt(index);
            }
        }

        foreach (var creature in friends)
        {
            if (!_icons.ContainsKey(creature))
            {
                _icons.Add(creature, new(creature, container));
                _order.Add(creature);
            }
        }

        HUD.FoodMeter? foodMeter = hud?.foodMeter;
        Vector2 anchor = foodMeter == null
            ? new(screenSize.x - RightInset - 60f, 30f)
            : new(screenSize.x - foodMeter.pos.x - RightInset, foodMeter.pos.y + (hud!.jollyMeter != null ? RowGap : 0f));

        _lastFade = _fade;
        _fade = Mathf.Max(foodMeter?.fade ?? 0f, _customFade);
        _customFade = Mathf.Max(0f, _customFade - CustomFadeStep);
        _counter = _fade > 0f ? _counter + 1 : 0;

        for (int index = 0; index < _order.Count; ++index)
        {
            MeterIcon icon = _icons[_order[index]];

            icon.MoveTo(anchor - new Vector2((_order.Count - 1 - index) * IconDistance, 0f));

            if (icon.Update(_fade))
            {
                _customFade = 5f;
            }

            if (_order[index].IsOutsideGate)
            {
                _customFade = Mathf.Max(_customFade, OutsideFade);
            }
        }
    }

    public void Draw(float timeStacker)
    {
        float fade = Mathf.Lerp(_lastFade, _fade, timeStacker);

        foreach (var icon in _icons.Values)
        {
            icon.Draw(fade, _counter, timeStacker);
        }
    }

    public void Clear()
    {
        foreach (var icon in _icons.Values)
        {
            icon.Remove();
        }

        _icons.Clear();
        _order.Clear();
    }
}
