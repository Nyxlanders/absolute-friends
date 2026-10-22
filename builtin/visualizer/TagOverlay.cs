using AbsoluteFriends.Core;
using AbsoluteFriends.Options;
using AbsoluteFriends.Utils;
using UnityEngine;

namespace AbsoluteFriends.Visualizer;

internal class TagOverlay : HudOverlay
{
    private readonly List<SubjectTag> _tags = [];

    private readonly OverlayPositions _positions = new();

    private readonly Dictionary<AbstractPhysicalObject, Subject> _subjects = [];

    private readonly UpdateTimer _timer = new();

    private bool _isViewed;

    private static bool IsEnabled => Config.Icon.IsActive || Config.Name.IsActive;

    public override void Update(bool eu)
    {
        base.Update(eu);

        try
        {
            if (RainWorldUtils.MainCamera(room?.game)?.room != room)
            {
                _isViewed = false;
                _subjects.Clear();

                return;
            }

            if (!_isViewed)
            {
                _isViewed = true;

                _timer.Reset();
            }

            if (_timer.Elapse())
            {
                Refresh();
            }
        }
        catch (Exception exception)
        {
            this.Destroy(exception);
        }
    }

    public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
    {
        base.DrawSprites(sLeaser, rCam, timeStacker, camPos);

        if (sLeaser.deleteMeNextFrame || !OverlayUtils.ShouldDraw(rCam, room))
        {
            return;
        }

        try
        {
            int index = 0;

            if (IsEnabled)
            {
                foreach (var entry in _subjects)
                {
                    if (!_positions.TryGet(entry.Key, rCam, camPos, timeStacker, out OverlayPosition place) || (place.IsDistant && entry.Value.Type != FriendshipType.Tracked))
                    {
                        continue;
                    }

                    Vector2 position = place.Screen;
                    bool isDistant = place.IsDistant;

                    SubjectTag tag = TagAt(index++);

                    Draw(tag, entry.Key, entry.Value, position, isDistant, timeStacker);

                    tag.IsDistant = isDistant;
                    tag.IsVisible = true;
                }
            }

            for (int i = index; i < _tags.Count; ++i)
            {
                _tags[i].IsVisible = false;
            }
        }
        catch (Exception exception)
        {
            this.Destroy(exception);
        }
    }

    private static void Draw(SubjectTag tag, AbstractPhysicalObject source, Subject subject, Vector2 position, bool isDistant, float timeStacker)
    {
        bool isMarked = FriendHudOverlay.IsShowing(source);
        int row = !isDistant && isMarked ? 3 : 0;

        tag.Clear();

        if (Relationships.IsShown(subject.Type) && !isMarked)
        {
            DrawSide(tag.Friend, subject.Friend, source.PulseColor(subject.Friend.Color, timeStacker), position, isDistant, ref row);
        }

        if (!isDistant && Config.Owner.IsActive && subject.Owner is { } owner)
        {
            DrawSide(tag.Owner, owner, owner.Color, position, false, ref row);
        }
    }

    private static void DrawSide(SideTag tag, RelationshipAppearance appearance, Color color, Vector2 position, bool isDistant, ref int row)
    {
        if (Config.Name.IsActive)
        {
            tag.SetName(appearance.Name, color, position, Offset(row++, isDistant));
        }

        if (Config.Icon.IsActive)
        {
            tag.SetIcon(appearance.Symbol, color, position, Offset(row++, isDistant));
        }
    }

    private static float Offset(int row, bool isDistant) => OverlayUtils.Gap * (isDistant ? row : row + 1);

    private void Refresh()
    {
        _subjects.Clear();

        if (!IsEnabled)
        {
            return;
        }

        foreach (var source in room.TaggedObjects)
        {
            FriendshipType type = source.FriendshipOfLocalPlayer;
            AbstractCreature? owner = source.ExternalOwner;

            if (type == FriendshipType.None && owner == null)
            {
                continue;
            }

            RelationshipAppearance friend = type == FriendshipType.None ? default : Relationships.AppearanceOf(source, type, false);
            RelationshipAppearance? ownerAppearance = owner == null ? null : Relationships.AppearanceOf(owner, owner.FriendshipOfLocalPlayer, true);

            _subjects[source] = new(type, friend, ownerAppearance);
        }
    }

    private SubjectTag TagAt(int index)
    {
        while (_tags.Count <= index)
        {
            SubjectTag tag = new();

            AddNodes(tag.Nodes);

            tag.IsVisible = false;

            _tags.Add(tag);
        }

        return _tags[index];
    }

    private readonly struct Subject(FriendshipType type, RelationshipAppearance friend, RelationshipAppearance? owner)
    {
        public FriendshipType Type { get; } = type;

        public RelationshipAppearance Friend { get; } = friend;

        public RelationshipAppearance? Owner { get; } = owner;
    }
}
