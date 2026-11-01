using AbsoluteFriends.Core;
using AbsoluteFriends.Diagnostics;
using AbsoluteFriends.Options;
using AbsoluteFriends.Utils;
using UnityEngine;

namespace AbsoluteFriends.ChainedTogether;

internal partial class Chain : CosmeticSprite
{
    private const int ScanInterval = RainWorldUtils.Second / 4;

    private const float RampStep = 2f;

    private const float RigidPull = 0.85f;

    private const float ElasticPull = 0.1f;

    private const float ElasticShift = 0.3f;

    private static readonly AddonLogger _logger = Reporter.GetLogger(Plugin.Name);

    private static readonly List<Chain> _chains = [];

    private readonly Creature _first;

    private readonly Creature _second;

    private readonly ChainStyle _style;

    private readonly ChainGraphic _graphic;

    private Rope? _rope;

    private float _length;

    private bool _isActive;

    public Chain(Creature first, Creature second)
    {
        _first = first;
        _second = second;
        _positions = new Vector2[SegmentCount];
        _lastPositions = new Vector2[_positions.Length];
        _style = ChainStyle.Current;
        _graphic = _style.Create(this);
    }

    private static float Length => Config.ChainLength.Value;

    private static float Elasticity => Config.ChainElasticity.Value;

    private float Limit => _length * (1f + Elasticity);

    private bool IsReplaced => !Config.Enabled.IsActive || _positions.Length != SegmentCount || _style != ChainStyle.Current;

    public static void Scan(Room room)
    {
        if (room.game == null || room.game.clock % ScanInterval != 0)
        {
            return;
        }

        _chains.RemoveAll(chain => chain.slatedForDeletetion);

        List<Creature> ends = [.. room.game.RealizedPlayers.Where(player => IsInRoom(player, room)), .. FriendUtils.TrackedFriends
            .Select(abstractCreature => abstractCreature.realizedCreature)
            .Where(creature => creature != null && IsInRoom(creature, room))];

        for (int i = 0; i < ends.Count; ++i)
        {
            for (int j = i + 1; j < ends.Count; ++j)
            {
                Tie(room, ends[i], ends[j]);
            }
        }
    }

    public static void Clear() => _chains.Clear();

    public override void Update(bool eu)
    {
        base.Update(eu);

        try
        {
            if (!IsValid())
            {
                Destroy();

                return;
            }

            bool wasActive = _isActive;

            _isActive = !_first.IsScripted && !_second.IsScripted && !Holds(_first, _second) && !Holds(_second, _first) && TryUpdateRope();

            if (_isActive)
            {
                float pathLength = _rope!.totalLength;

                Pull(_first, _rope.AConnect, _second, pathLength);
                Pull(_second, _rope.BConnect, _first, pathLength);
                UpdateSegments(wasActive);
            }
            else
            {
                _rope = null;
            }

            UpdateAppearance();
            UpdateSounds(wasActive);
        }
        catch (Exception exception)
        {
            _logger.LogError("A chain failed and was removed", exception);

            Destroy();
        }
    }

    public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
    {
        sLeaser.sprites = new FSprite[1];

        _graphic.InitiateSprites(sLeaser.sprites, rCam);

        AddToContainer(sLeaser, rCam, null);
    }

    public override void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer? newContainer)
    {
        base.AddToContainer(sLeaser, rCam, newContainer ?? rCam.ReturnFContainer(_graphic.Container));
    }

    public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
    {
        base.DrawSprites(sLeaser, rCam, timeStacker, camPos);

        if (slatedForDeletetion)
        {
            return;
        }

        try
        {
            bool isVisible = _visibility > 0f;

            foreach (var sprite in sLeaser.sprites)
            {
                sprite.isVisible = isVisible;
            }

            if (!isVisible)
            {
                return;
            }

            _graphic.DrawSprites(sLeaser.sprites, timeStacker, camPos);
        }
        catch (Exception exception)
        {
            _logger.LogError("A chain could not be drawn and was removed", exception);

            Destroy();
        }
    }

    private static void Tie(Room room, Creature first, Creature second)
    {
        if (_chains.Any(chain => chain.Ties(first, second)) || !IsOnSameLayer(first, second) || !ShouldTie(first, second))
        {
            return;
        }

        Chain chain = new(first, second);

        _chains.Add(chain);
        room.AddObject(chain);
    }

    private static bool ShouldTie(Creature first, Creature second) => (first.IsPlayer, second.IsPlayer) switch
    {
        (true, true) => first.IsFriend(second),
        (true, false) => second.abstractCreature.IsTrackedFor(first.abstractCreature),
        (false, true) => first.abstractCreature.IsTrackedFor(second.abstractCreature),
        _ => first.IsTracked && second.IsTracked && first.IsFriend(second)
    };

    private static bool IsInRoom(Creature creature, Room room) => creature.room == room && !creature.inShortcut && !creature.slatedForDeletetion;

    private static bool IsOnSameLayer(Creature first, Creature second)
    {
        AbstractCreature firstCreature = first.abstractCreature;
        AbstractCreature secondCreature = second.abstractCreature;

        return firstCreature.rippleBothSides || secondCreature.rippleBothSides || firstCreature.rippleLayer == secondCreature.rippleLayer;
    }

    private static bool Holds(Creature holder, Creature held) => held.grabbedBy.Any(grasp => grasp?.grabber == holder);

    private bool Ties(Creature first, Creature second) => (_first == first && _second == second) || (_first == second && _second == first);

    private bool IsValid() => !IsReplaced && IsInRoom(_first, room) && IsInRoom(_second, room) && IsOnSameLayer(_first, _second) && ShouldTie(_first, _second);

    private bool TryUpdateRope()
    {
        Vector2 firstPosition = _first.mainBodyChunk.pos;
        Vector2 secondPosition = _second.mainBodyChunk.pos;

        if (_rope == null)
        {
            if (!room.VisualContact(firstPosition, secondPosition))
            {
                return false;
            }

            _rope = new Rope(room, firstPosition, secondPosition, 1f);
            _length = Mathf.Max(Length, _rope.totalLength);
        }

        _rope.Update(firstPosition, secondPosition);
        _length = Mathf.Max(Length, Mathf.Min(_length, _rope.totalLength) - RampStep);

        return true;
    }

    private void Pull(Creature creature, Vector2 anchor, Creature other, float pathLength)
    {
        float stretch = pathLength - _length;

        if (!creature.HasAuthority || stretch <= 0f)
        {
            return;
        }

        BodyChunk chunk = creature.mainBodyChunk;
        Vector2 direction = (anchor - chunk.pos).normalized;
        float share = other.TotalMass / (creature.TotalMass + other.TotalMass);
        float pull = stretch * Mathf.Lerp(RigidPull, ElasticPull, Elasticity);
        float shift = Mathf.Lerp(1f, ElasticShift, Elasticity);

        chunk.vel += direction * (pull * share);
        chunk.pos += direction * (pull * shift * share);

        float overflow = pathLength - Limit - pull * shift;

        if (overflow <= 0f)
        {
            return;
        }

        chunk.pos += direction * (overflow * share);

        float outward = -Vector2.Dot(chunk.vel, direction);

        if (outward > 0f)
        {
            chunk.vel += direction * (outward * share);
        }
    }
}
