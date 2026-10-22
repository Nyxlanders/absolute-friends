using AbsoluteFriends.Core;
using AbsoluteFriends.Utils;
using JollyCoop;
using RWCustom;
using UnityEngine;

namespace AbsoluteFriends.Visualizer;

internal partial class FriendMeter
{
    private class MeterIcon
    {
        private const string FallbackSymbol = "Kill_Slugcat";

        private const float IconSize = 20f;

        private const float GlowScale = 3.75f;

        private const float GlowAlpha = 0.45f;

        private static readonly Color _deathGlow = Color.Lerp(Color.red, Color.black, 0.5f);

        private readonly AbstractCreature _creature;

        private readonly FSprite _glow = new("Futile_White", true)
        {
            shader = RainWorldUtils.Shader("FlatLight") ?? FShader.defaultShader,
            alpha = 0f,
        };

        private readonly FSprite _icon = new(FallbackSymbol, true) { alpha = 0f };

        private Vector2 _position;

        private Vector2 _lastPosition;

        private Color _color;

        private float _baseScale = 1f;

        private float _rad;

        private float _blink;

        private float _lastBlink;

        private bool _isDead;

        private bool _isPlaced;

        public MeterIcon(AbstractCreature creature, FContainer container)
        {
            _creature = creature;
            _isDead = creature.IsDead;

            container.AddChild(_glow);
            container.AddChild(_icon);

            Restyle();
        }

        public bool Update(float fade)
        {
            bool changed = _isDead != _creature.IsDead;

            _lastBlink = _blink;
            _blink = Mathf.Max(0f, _blink - 0.05f);
            _rad = Custom.LerpAndTick(_rad, Custom.LerpMap(fade, 0f, 0.79f, 0.79f, 1f, 1.3f), 0.12f, 0.1f);

            if (changed)
            {
                _isDead = !_isDead;
                _blink = 3f;

                Restyle();
            }

            return changed;
        }

        public void MoveTo(Vector2 position)
        {
            _lastPosition = _isPlaced ? _position : position;
            _position = position;
            _isPlaced = true;
        }

        public void Draw(float fade, int counter, float timeStacker)
        {
            Vector2 position = Vector2.Lerp(_lastPosition, _position, timeStacker);
            float scale = _rad * _creature.PulseScale(timeStacker);
            Color color = _creature.PulseColor(_color, timeStacker);

            if (counter % 6 < 2 && _lastBlink > 0f)
            {
                Vector3 hsl = Custom.RGB2HSL(color);

                color = Color.Lerp(color, Custom.HSL2RGB(hsl.x, hsl.y, hsl.z + 0.2f), Mathf.InverseLerp(0f, 0.5f, Mathf.Lerp(_lastBlink, _blink, timeStacker)));
            }

            _icon.SetPosition(position);
            _icon.scale = _baseScale * scale;
            _icon.color = color;
            _icon.alpha = fade;
            _glow.SetPosition(position);
            _glow.scale = GlowScale * scale;
            _glow.alpha = Mathf.SmoothStep(0f, 1f, fade) * GlowAlpha;
        }

        public void Remove()
        {
            _glow.RemoveFromContainer();
            _icon.RemoveFromContainer();
        }

        private void Restyle()
        {
            string symbol = _creature.Icon.Symbol;
            Color color = _creature.ShortcutColor;

            _glow.color = _isDead ? _deathGlow : JollyCustom.ColorClamp(color, -1f, 360f, 60f, 360f, -1f, 360f);

            _color = _isDead ? color.Dimmed : color;
            _icon.FitSymbol(Futile.atlasManager.DoesContainElementWithName(symbol) ? symbol : FallbackSymbol, IconSize);
            _baseScale = _icon.scale;
        }
    }
}
