using AbsoluteFriends.Core;
using AbsoluteFriends.Options;
using AbsoluteFriends.Utils;
using Menu;
using Menu.Remix.MixedUI;
using UnityEngine;

namespace AbsoluteFriends.Manager;

internal partial class FriendManagerDialog
{
    private partial class DetailsTab
    {
        private static string RelationshipLabel(FriendshipType type) => type switch
        {
            FriendshipType.Tracked => "Tracking",
            FriendshipType.Friendly => "Friendly",
            FriendshipType.Neutral => "Neutral",
            FriendshipType.Indirect => "Indirect",
            _ => "Not friends",
        };

        private static Color RelationshipColor(FriendshipType type) => type switch
        {
            FriendshipType.Tracked => Palette.Tracked,
            FriendshipType.Friendly => Palette.Friendly,
            FriendshipType.Neutral => Palette.Neutral,
            FriendshipType.Indirect => Palette.Indirect,
            _ => MenuColorEffect.rgbMediumGrey,
        };

        private class RelationshipRow
        {
            internal const float Height = 30f;

            private const float LabelHeight = 24f;

            private const float NameX = 28f;

            private const float NameWidth = 192f;

            private const float RelationX = 226f;

            private const float RelationWidth = 70f;

            private const float BarX = 302f;

            private const float BarWidth = 167f;

            private const float BarCenter = BarX + BarWidth * 0.5f;

            private const float BarHeight = 6f;

            private const float KnowHeight = 2f;

            private const float KnowGap = 4f;

            private const float ThresholdOverhang = 2f;

            private const float TickWidth = 2f;

            private const float TickHeight = 12f;

            private const float TickOutline = 1f;

            private const float PercentWidth = 40f;

            private readonly OpImage _icon;

            private readonly OpLabel _name;

            private readonly OpLabel _relation;

            private readonly OpImage _barBack;

            private readonly OpImage _barFill;

            private readonly OpImage _knowFill;

            private readonly OpImage _center;

            private readonly OpImage _threshold;

            private readonly OpImage _tempTickBack;

            private readonly OpImage _tempTick;

            private readonly OpLabel _percent;

            private readonly float _percentX;

            internal RelationshipRow(FriendTab tab, ScrollArea scrollBox)
            {
                _percentX = tab.Size.x - 25f - PercentWidth;
                _icon = new(Vector2.zero, "Futile_White")
                {
                    anchor = new(0.5f, 0.5f)
                };
                _name = new(Vector2.zero, new Vector2(NameWidth, LabelHeight), string.Empty, FLabelAlignment.Left);
                _relation = new(Vector2.zero, new Vector2(RelationWidth, LabelHeight), string.Empty, FLabelAlignment.Left);
                _barBack = new(Vector2.zero, "pixel")
                {
                    scale = new(BarWidth, BarHeight),
                    color = MenuColorEffect.rgbDarkGrey
                };
                _barFill = new(Vector2.zero, "pixel")
                {
                    scale = new(0f, BarHeight)
                };
                _knowFill = new(Vector2.zero, "pixel")
                {
                    scale = new(0f, KnowHeight),
                    color = MenuColorEffect.rgbMediumGrey
                };
                _center = new(Vector2.zero, "pixel")
                {
                    scale = new(1f, BarHeight + ThresholdOverhang * 2f),
                    color = MenuColorEffect.rgbMediumGrey
                };
                _threshold = new(Vector2.zero, "pixel")
                {
                    scale = new(1f, BarHeight + ThresholdOverhang * 2f),
                    color = MenuColorEffect.rgbMediumGrey
                };
                _tempTickBack = new(Vector2.zero, "pixel")
                {
                    scale = new(TickWidth + TickOutline * 2f, TickHeight + TickOutline * 2f),
                    color = Color.black
                };
                _tempTick = new(Vector2.zero, "pixel")
                {
                    scale = new(TickWidth, TickHeight),
                    color = Color.white
                };
                _percent = new(Vector2.zero, new Vector2(PercentWidth, LabelHeight), string.Empty, FLabelAlignment.Right);

                foreach (var element in Elements)
                {
                    tab.Add(scrollBox, element);
                }
            }

            private UIelement[] Elements => [_icon, _name, _relation, _barBack, _barFill, _knowFill, _center, _threshold, _tempTickBack, _tempTick, _percent];

            internal void Show(AbstractCreature friend, AbstractCreature other, float y)
            {
                (string symbol, Color color) = other.Icon;
                bool hasIcon = Futile.atlasManager.DoesContainElementWithName(symbol);
                FriendshipType friendship = friend.FriendshipWith(other);
                (float Like, float TempLike, float Know)? memory = friend.IsPlayer
                    ? MemoryOf(other, friend)
                    : MemoryOf(friend, other) ?? MemoryOf(other, friend);
                float labelY = y + (Height - LabelHeight) * 0.5f;
                float barY = y + (Height - BarHeight) * 0.5f;

                Place(_icon, new Vector2(12f, y + Height * 0.5f));
                Place(_name, new Vector2(NameX, labelY));
                Place(_relation, new Vector2(RelationX, labelY));
                Place(_barBack, new Vector2(BarX, barY));
                Place(_knowFill, new Vector2(BarX, barY - KnowGap - KnowHeight));
                Place(_center, new Vector2(BarCenter, barY - ThresholdOverhang));
                Place(_threshold, new Vector2(BarCenter + BarWidth * 0.5f * FriendUtils.FriendLikeThreshold, barY - ThresholdOverhang));
                Place(_percent, new Vector2(_percentX, labelY));

                SetShown(_icon, hasIcon);
                SetShown(true, _name, _relation);
                SetShown(memory != null, _barBack, _barFill, _knowFill, _center, _threshold, _tempTickBack, _tempTick, _percent);

                if (hasIcon)
                {
                    _icon.sprite.SetElementByName(symbol);
                    _icon.color = color;
                    FitIcon(_icon);
                }

                string name = other.FriendName;
                string relation = Translation.Of(RelationshipLabel(friendship));

                _name.text = Trim(name, NameWidth);
                _name.color = other.ShortcutColor;
                _relation.text = Trim(relation, RelationWidth);
                _relation.color = RelationshipColor(friendship);

                if (memory is not { } values)
                {
                    return;
                }

                float like = Mathf.Clamp(values.Like, -1f, 1f);
                float tempLike = Mathf.Clamp(values.TempLike, -1f, 1f);

                float fill = BarWidth * 0.5f * like;
                Vector2 tick = new(Mathf.Clamp(BarCenter + BarWidth * 0.5f * tempLike - TickWidth * 0.5f, BarX, BarX + BarWidth - TickWidth), y + (Height - TickHeight) * 0.5f);

                Place(_tempTickBack, tick - new Vector2(TickOutline, TickOutline));
                Place(_tempTick, tick);
                Place(_barFill, new Vector2(fill < 0f ? BarCenter + fill : BarCenter, barY));
                _barFill.scale = new(Mathf.Abs(fill), BarHeight);
                _barFill.color = _relation.color;
                _knowFill.scale = new(BarWidth * Mathf.Clamp01(values.Know), KnowHeight);
                _percent.text = $"{Mathf.RoundToInt(like * 100f)}%";
                _percent.color = _relation.color;
            }

            internal void Hide() => SetShown(false, Elements);

            private static (float Like, float TempLike, float Know)? MemoryOf(AbstractCreature creature, AbstractCreature subject) =>
                creature.state?.socialMemory is { } socialMemory && socialMemory.GetRelationship(subject.ID) != null
                    ? (socialMemory.GetLike(subject.ID), socialMemory.GetTempLike(subject.ID), socialMemory.GetKnow(subject.ID))
                    : null;

            private static void Place(UIelement element, Vector2 position)
            {
                if (element.GetPos() == position)
                {
                    return;
                }

                element.SetPos(position);
                Snap(element);
            }
        }
    }
}
