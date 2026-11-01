using AbsoluteFriends.Core;
using RWCustom;
using UnityEngine;

namespace AbsoluteFriends.ChainedTogether;

internal partial class Chain
{
    private const float SegmentLength = 10f;

    private const int MinSegments = 32;

    private const float FadeStart = 0.4f;

    private const float EndFade = 5f;

    private const float SnapDistance = 200f;

    private const float SlackFollow = 0.35f;

    private const float TautFollowStart = 0.9f;

    private readonly Vector2[] _positions;

    private readonly Vector2[] _lastPositions;

    private Color _firstColor = Color.white;

    private Color _secondColor = Color.white;

    private bool _isDrawn;

    private float _visibility;

    private float _strain;

    private static int SegmentCount => Mathf.Max(Mathf.CeilToInt(Length * (1f + Elasticity) / SegmentLength) + 1, MinSegments);

    internal int LastIndex => _positions.Length - 1;

    internal float Visibility => _visibility;

    internal float Strain => _strain;

    internal static float EndFadeOf(int index, int last) => Mathf.InverseLerp(0f, EndFade, Mathf.Min(index, last - index));

    internal Color ColorAt(float t) => Color.Lerp(_firstColor, _secondColor, t);

    internal Vector2 PositionAt(int index, float timeStacker)
    {
        if (index == 0)
        {
            return Vector2.Lerp(_first.mainBodyChunk.lastPos, _first.mainBodyChunk.pos, timeStacker);
        }

        if (index == _positions.Length - 1)
        {
            return Vector2.Lerp(_second.mainBodyChunk.lastPos, _second.mainBodyChunk.pos, timeStacker);
        }

        return Vector2.Lerp(_lastPositions[index], _positions[index], timeStacker);
    }

    private static float SagOf(float chord, float ratio)
    {
        float arc = chord * ratio;
        float shallow = Mathf.Sqrt(3f / 8f * chord * (arc - chord));
        float folded = Mathf.Sqrt(arc * arc - chord * chord) * 0.5f;

        return Mathf.Lerp(shallow, folded, Mathf.InverseLerp(1f, 2f, ratio));
    }

    private static Vector2 SagDirection(Vector2 from, Vector2 to)
    {
        Vector2 along = (to - from).normalized;

        return Vector2.down - along * Vector2.Dot(Vector2.down, along);
    }

    private void UpdateSegments(bool wasActive)
    {
        int last = _positions.Length - 1;
        int pointCount = _rope!.TotalPositions;
        float pathLength = _rope.totalLength;

        Array.Copy(_positions, _lastPositions, _positions.Length);

        _isDrawn = pointCount - 1 <= last && pathLength > 1f;

        if (!_isDrawn)
        {
            return;
        }

        float ratio = Mathf.Max(_length, pathLength) / pathLength;
        float gravity = Mathf.Clamp01(room.gravity);
        float follow = Mathf.Lerp(SlackFollow, 1f, Mathf.InverseLerp(TautFollowStart * _length, _length, pathLength));
        float travelled = 0f;
        int start = 0;

        for (int point = 0; point < pointCount - 1; ++point)
        {
            Vector2 from = _rope.GetPosition(point);
            Vector2 to = _rope.GetPosition(point + 1);
            float chord = Vector2.Distance(from, to);

            travelled += chord;

            int end = point == pointCount - 2
                ? last
                : Mathf.Clamp(Mathf.RoundToInt(travelled / pathLength * last), start + 1, last - (pointCount - 2 - point));
            Vector2 sag = SagDirection(from, to) * (SagOf(chord, ratio) * gravity);

            for (int i = start; i <= end; ++i)
            {
                float t = Mathf.InverseLerp(start, end, i);
                Vector2 straight = Vector2.Lerp(from, to, t);
                Vector2 sagged = straight + sag * (4f * t * (1f - t));
                Vector2 target = SharedPhysics.ExactTerrainRayTracePos(room, straight, sagged) ?? sagged;

                Move(i, target, i == start || i == end ? 1f : follow, wasActive);
            }

            start = end;
        }
    }

    private void UpdateAppearance()
    {
        bool isShown = _isActive && _isDrawn;

        _firstColor = _first.abstractCreature.ShortcutColor;
        _secondColor = _second.abstractCreature.ShortcutColor;
        _visibility = isShown ? Mathf.InverseLerp(FadeStart * Length, Length, _rope!.totalLength) : 0f;
        _strain = isShown ? Mathf.InverseLerp(_length, Limit, _rope!.totalLength) : 0f;
    }

    private void Move(int index, Vector2 target, float follow, bool wasActive)
    {
        if (!wasActive || !Custom.DistLess(_positions[index], target, SnapDistance))
        {
            _positions[index] = target;
            _lastPositions[index] = target;

            return;
        }

        _positions[index] = Vector2.Lerp(_positions[index], target, follow);
    }
}
