using AbsoluteFriends.Core;
using AbsoluteFriends.Manager;
using AbsoluteFriends.Options;
using AbsoluteFriends.Utils;
using Menu;
using Menu.Remix.MixedUI;
using UnityEngine;

namespace AbsoluteFriends.FriendshipEditor;

internal class CreatureRow
{
    internal const float Height = EditorUi.ListRowHeight;

    private const float NameX = 28f;

    private const float NameWidth = EditorUi.ListNameWidth - NameX;

    private const float BarX = EditorUi.ListBarX;

    private const float BarWidth = EditorUi.ListBarWidth;

    private const float BarCenter = BarX + BarWidth * 0.5f;

    private const float BarHeight = EditorUi.ListBarHeight;

    private const float KnowHeight = 2f;

    private const float KnowGap = 4f;

    private const float TickWidth = 2f;

    private const float TickHeight = 12f;

    private const float PercentX = EditorUi.ListPercentX;

    private const float PercentWidth = EditorUi.ListPercentWidth;

    private readonly OpImage _icon;

    private readonly OpLabel _name;

    private readonly OpImage _barBack;

    private readonly OpImage _barFill;

    private readonly OpImage _knowFill;

    private readonly OpImage _center;

    private readonly OpImage _threshold;

    private readonly OpImage _tickBack;

    private readonly OpImage _tick;

    private readonly OpLabel _relation;

    private readonly OpLabel _percent;

    internal CreatureRow(FriendTab tab, ScrollArea list)
    {
        _icon = new(Vector2.zero, "Futile_White")
        {
            anchor = new(0.5f, 0.5f)
        };
        _name = new(Vector2.zero, new Vector2(NameWidth, EditorUi.RowHeight), string.Empty, FLabelAlignment.Left);
        _barBack = EditorUi.Pixel(MenuColorEffect.rgbDarkGrey);
        _barBack.scale = new Vector2(BarWidth, BarHeight);
        _barFill = EditorUi.Pixel(Color.white);
        _knowFill = EditorUi.Pixel(MenuColorEffect.rgbMediumGrey);
        _center = EditorUi.Pixel(MenuColorEffect.rgbMediumGrey);
        _center.scale = new Vector2(1f, BarHeight + EditorUi.ListMarkOverhang * 2f);
        _threshold = EditorUi.Pixel(MenuColorEffect.rgbMediumGrey);
        _threshold.scale = new Vector2(1f, BarHeight + EditorUi.ListMarkOverhang * 2f);
        _tickBack = EditorUi.Pixel(Color.black);
        _tickBack.scale = new Vector2(TickWidth + 2f, TickHeight + 2f);
        _tick = EditorUi.Pixel(Color.white);
        _tick.scale = new Vector2(TickWidth, TickHeight);
        _relation = new(Vector2.zero, new Vector2(BarWidth, EditorUi.RowHeight), string.Empty, FLabelAlignment.Left);
        _percent = new(Vector2.zero, new Vector2(PercentWidth, EditorUi.RowHeight), string.Empty, FLabelAlignment.Right);

        foreach (var element in Elements)
        {
            tab.Add(list, element);
        }
    }

    private UIelement[] Elements => [_icon, _name, _barBack, _barFill, _knowFill, _center, _threshold, _tickBack, _tick, _relation, _percent];

    private UIelement[] Bars => [_barBack, _barFill, _knowFill, _center, _threshold, _tickBack, _tick];

    internal void ShowLikes(AbstractCreature creature, (float Like, float TempLike, float Know)? likes, float y)
    {
        float barY = y + (Height - BarHeight) * 0.5f;

        ShowCreature(creature, y);
        EditorUi.Place(_barBack, new Vector2(BarX, barY));
        EditorUi.Place(_knowFill, new Vector2(BarX, barY - KnowGap - KnowHeight));
        EditorUi.Place(_center, new Vector2(BarCenter, barY - EditorUi.ListMarkOverhang));
        EditorUi.Place(_threshold, new Vector2(BarCenter + BarWidth * 0.5f * FriendUtils.FriendLikeThreshold, barY - EditorUi.ListMarkOverhang));
        EditorUi.SetShown(_relation, false);
        EditorUi.SetShown(_percent, likes != null);

        foreach (var element in Bars)
        {
            EditorUi.SetShown(element, likes != null);
        }

        if (likes is not { } values)
        {
            return;
        }

        float like = Mathf.Clamp(values.Like, -1f, 1f);
        float tempLike = Mathf.Clamp(values.TempLike, -1f, 1f);
        Color likeColor = like > FriendUtils.FriendLikeThreshold && tempLike > FriendUtils.FriendLikeThreshold ? Palette.Friendly
            : like < 0f ? Palette.Unfriendly : Palette.Neutral;
        float fill = BarWidth * 0.5f * like;
        Vector2 tick = new(Mathf.Clamp(BarCenter + BarWidth * 0.5f * tempLike - TickWidth * 0.5f, BarX, BarX + BarWidth - TickWidth), y + (Height - TickHeight) * 0.5f);

        EditorUi.Place(_tickBack, tick - Vector2.one);
        EditorUi.Place(_tick, tick);
        EditorUi.Place(_barFill, new Vector2(fill < 0f ? BarCenter + fill : BarCenter, barY));
        _barFill.scale = new Vector2(Mathf.Abs(fill), BarHeight);
        _knowFill.scale = new Vector2(BarWidth * Mathf.Clamp01(values.Know), KnowHeight);
        _barFill.color = likeColor;
        _percent.text = EditorUi.PercentText(like);
        _percent.color = likeColor;
    }

    internal void ShowRelationship(AbstractCreature creature, CreatureTemplate.Relationship relationship, bool live, float y)
    {
        Color color = live ? RelationshipTypes.ColorOf(relationship.type) : MenuColorEffect.rgbMediumGrey;

        ShowCreature(creature, y);
        EditorUi.Place(_relation, new Vector2(BarX, LabelY(y)));
        EditorUi.SetShown(_relation, true);
        EditorUi.SetShown(_percent, true);

        foreach (var element in Bars)
        {
            EditorUi.SetShown(element, false);
        }

        _relation.text = EditorUi.Trim(relationship.type.value, BarWidth);
        _relation.color = color;
        _percent.text = EditorUi.PercentText(relationship.intensity);
        _percent.color = color;
    }

    internal void Hide()
    {
        foreach (var element in Elements)
        {
            EditorUi.SetShown(element, false);
        }
    }

    private static float LabelY(float y) => y + (Height - EditorUi.RowHeight) * 0.5f;

    private void ShowCreature(AbstractCreature creature, float y)
    {
        (string symbol, Color color) = creature.Icon;
        bool hasIcon = Futile.atlasManager.DoesContainElementWithName(symbol);

        EditorUi.Place(_icon, new Vector2(12f, y + Height * 0.5f));
        EditorUi.Place(_name, new Vector2(NameX, LabelY(y)));
        EditorUi.Place(_percent, new Vector2(PercentX, LabelY(y)));
        EditorUi.SetShown(_icon, hasIcon);
        EditorUi.SetShown(_name, true);

        if (hasIcon)
        {
            _icon.sprite.SetElementByName(symbol);
            _icon.color = color;
            EditorUi.FitIcon(_icon);
        }

        _name.text = EditorUi.Trim(creature.FriendName, NameWidth);
        _name.color = creature.ShortcutColor;
    }
}
