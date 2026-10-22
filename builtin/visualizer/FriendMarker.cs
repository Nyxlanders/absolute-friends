using AbsoluteFriends.Core;
using AbsoluteFriends.Utils;
using JollyCoop;
using RWCustom;
using UnityEngine;
using Random = UnityEngine.Random;

namespace AbsoluteFriends.Visualizer;

internal class FriendMarker
{
    private const string ArrowSymbol = "Multiplayer_Arrow";

    private const string FallbackSymbol = "FriendA";

    private const float TextScale = 0.75f;

    private const float IconSize = 24f;

    private const float ArrowHeight = 40f;

    private const float NameHeight = 20f;

    private const float PointingDistance = 20f;

    private const float RoomAlpha = 0.85f;

    private const float FadeStep = 1f / 8f;

    private const float IconGlowScale = 110f / 16f;

    private const float IconGlowAlpha = 0.15f;

    private const float ArrowGlowAlpha = 0.17f;

    private const int Pulse = 40;

    private readonly AbstractCreature _creature;

    private readonly FSprite _arrow = new(ArrowSymbol, true) { isVisible = false };

    private readonly FLabel _name = new(Custom.GetDisplayFont(), "") { anchorX = 0.5f, anchorY = 0.5f, scale = TextScale, isVisible = false };

    private readonly FSprite _icon = new(FallbackSymbol, true) { isVisible = false, IsHologram = true };

    private readonly FSprite _iconGlow = CreateGlow(IconGlowScale);

    private readonly FSprite _arrowGlow = CreateGlow(0f);

    private OverlayPosition _place;

    private Color _color = Color.white;

    private float _iconScale = 1f;

    private float _nearAlpha;

    private float _lastNearAlpha;

    private float _farAlpha;

    private float _lastFarAlpha;

    private int _frequency;

    private int _shortcutWait;

    public FriendMarker(AbstractCreature creature) => _creature = creature;

    public IEnumerable<FNode> Nodes => [_iconGlow, _arrowGlow, _icon, _arrow, _name];

    public bool IsVisible => _nearAlpha > 0f || _farAlpha > 0f || _lastNearAlpha > 0f || _lastFarAlpha > 0f;

    public void Refresh()
    {
        string symbol = _creature.Icon.Symbol;
        Color color = _creature.ShortcutColor;

        _color = _creature.IsDead ? color.Dimmed : color;

        string name = _creature.FriendName;

        if (_name.text != name)
        {
            _name.text = name;
        }

        _icon.FitSymbol(Futile.atlasManager.DoesContainElementWithName(symbol) ? symbol : FallbackSymbol, IconSize);
        _iconScale = _icon.scale;
        _iconGlow.color = _color;
        _arrowGlow.color = JollyCustom.GenerateClippedInverseColor(_color);
    }

    public void Update(OverlayPosition? place)
    {
        bool isKnown = place.HasValue;
        bool isInRoom = isKnown && !place!.Value.IsOtherRoom;

        if (place.HasValue)
        {
            _place = place.Value;
        }

        _lastNearAlpha = _nearAlpha;
        _lastFarAlpha = _farAlpha;
        _shortcutWait = _creature.realizedCreature?.ShortcutVessel(_creature.world?.game)?.wait ?? 0;
        _frequency = (_frequency + 1) % Pulse;
        _nearAlpha = Mathf.MoveTowards(_nearAlpha, isInRoom ? 1f : 0f, FadeStep);
        _farAlpha = Mathf.MoveTowards(_farAlpha, isKnown && !isInRoom ? RoomAlpha : 0f, FadeStep);
    }

    public void Draw(RoomCamera camera, OverlayPosition? place, float timeStacker)
    {
        if (place.HasValue)
        {
            _place = place.Value;
        }

        float nearAlpha = Mathf.Pow(Mathf.Max(0f, Mathf.Lerp(_lastNearAlpha, _nearAlpha, timeStacker)), 0.7f);
        float farAlpha = Mathf.SmoothStep(_lastFarAlpha, _farAlpha, timeStacker);
        Color color = _creature.PulseColor(_color, timeStacker);

        if (_shortcutWait > 0 && nearAlpha > 0f)
        {
            float wait = (float)_shortcutWait / Player.InitialShortcutWaitTime;
            float speed = Mathf.Pow(Mathf.Lerp(17f, 4f, wait), 1.2f);
            float pulse = 0.5f * (0.8f + Mathf.Sin(speed * _frequency / Pulse));

            nearAlpha = 1f;
            color = Color.Lerp(_color, JollyCustom.GenerateClippedInverseColor(_color), Mathf.Lerp(0.01f, 0.6f, pulse));
        }

        DrawArrow(camera, nearAlpha, color);
        DrawIcon(camera, farAlpha, color);
    }

    public void Remove()
    {
        foreach (var node in Nodes)
        {
            node.RemoveFromContainer();
        }
    }

    private void DrawArrow(RoomCamera camera, float alpha, Color color)
    {
        Vector2 arrow = (_place.Actual + new Vector2(0f, ArrowHeight)).ClampToScreen(camera);
        float rotation = 0f;

        if (Vector2.Distance(_place.Actual, arrow) > PointingDistance)
        {
            rotation = Mathf.Round(Custom.AimFromOneVectorToAnother(_place.Actual, arrow) / 90f) * 90f;
        }

        _arrow.SetPosition(arrow);
        _arrow.rotation = rotation;
        _arrow.color = color;
        _name.SetPosition((arrow + new Vector2(0f, NameHeight)).ClampToScreen(camera));
        _name.color = color;

        SetAlpha(_arrow, alpha);
        SetAlpha(_name, alpha);
        _arrowGlow.SetPosition(arrow);
        _arrowGlow.scale = Mathf.Lerp(80f, 110f, alpha) / 16f;

        SetAlpha(_arrowGlow, ArrowGlowAlpha * alpha * alpha);
    }

    private void DrawIcon(RoomCamera camera, float alpha, Color color)
    {
        if (alpha <= 0f || Random.value > Mathf.InverseLerp(0.5f, 0.75f, alpha))
        {
            SetAlpha(_icon, 0f);
            SetAlpha(_iconGlow, 0f);

            return;
        }

        Vector2 middle = camera.sSize / 2f;
        Vector2 position = ToEdge(camera, _place.Actual);
        float distance = Mathf.Max(1f, Vector2.Distance(_place.Actual, middle));
        float scale = Mathf.Lerp(0.65f, 1.65f, Mathf.Pow(middle.magnitude / distance, 1.2f)) / 1.25f;

        _icon.SetPosition(position);
        _icon.scale = _iconScale * scale;
        _icon.color = color;
        _iconGlow.SetPosition(position);

        SetAlpha(_icon, alpha);
        SetAlpha(_iconGlow, IconGlowAlpha * alpha * alpha);
    }

    private static Vector2 ToEdge(RoomCamera camera, Vector2 position)
    {
        Vector2 min = new(OverlayUtils.Margin, OverlayUtils.Margin);
        Vector2 max = camera.sSize - min;

        if (position.x > min.x && position.x < max.x && position.y > min.y && position.y < max.y)
        {
            float left = position.x - min.x;
            float right = max.x - position.x;
            float bottom = position.y - min.y;
            float top = max.y - position.y;
            float nearest = Mathf.Min(Mathf.Min(left, right), Mathf.Min(bottom, top));

            if (nearest == left)
            {
                position.x = min.x;
            }
            else if (nearest == right)
            {
                position.x = max.x;
            }
            else if (nearest == bottom)
            {
                position.y = min.y;
            }
            else
            {
                position.y = max.y;
            }
        }

        return position.ClampToScreen(camera);
    }

    private static void SetAlpha(FNode node, float alpha)
    {
        node.alpha = alpha;
        node.isVisible = alpha > 0f;
    }

    private static FSprite CreateGlow(float scale) => new("Futile_White", true)
    {
        shader = RainWorldUtils.Shader("FlatLight") ?? FShader.defaultShader,
        scale = scale,
        alpha = 0f,
        isVisible = false,
    };
}
