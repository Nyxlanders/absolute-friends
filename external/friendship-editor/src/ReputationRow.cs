using AbsoluteFriends.Manager;
using AbsoluteFriends.Options;
using AbsoluteFriends.Utils;
using Menu;
using Menu.Remix.MixedUI;
using UnityEngine;

namespace AbsoluteFriends.FriendshipEditor;

internal class ReputationRow
{
    internal const float Height = EditorUi.ListRowHeight;

    private const float BarX = EditorUi.ListBarX;

    private const float BarWidth = EditorUi.ListBarWidth;

    private const float BarHeight = EditorUi.ListBarHeight;

    private const float PercentX = EditorUi.ListPercentX;

    private const float PercentWidth = EditorUi.ListPercentWidth;

    private readonly OpLabel _name;

    private readonly OpImage _barBack;

    private readonly OpImage _barFill;

    private readonly OpImage _center;

    private readonly OpLabel _percent;

    internal ReputationRow(FriendTab tab, ScrollArea list, string key, string name)
    {
        Key = key;
        _name = new(Vector2.zero, new Vector2(EditorUi.ListNameWidth, EditorUi.RowHeight), EditorUi.Trim(name, EditorUi.ListNameWidth), FLabelAlignment.Left);
        _barBack = EditorUi.Pixel(MenuColorEffect.rgbDarkGrey);
        _barBack.scale = new Vector2(BarWidth, BarHeight);
        _barFill = EditorUi.Pixel(Color.white);
        _center = EditorUi.Pixel(MenuColorEffect.rgbMediumGrey);
        _center.scale = new Vector2(1f, BarHeight + EditorUi.ListMarkOverhang * 2f);
        _percent = new(Vector2.zero, new Vector2(PercentWidth, EditorUi.RowHeight), string.Empty, FLabelAlignment.Right);

        foreach (var element in Elements)
        {
            tab.Add(list, element);
        }
    }

    internal string Key { get; }

    private UIelement[] Elements => [_name, _barBack, _barFill, _center, _percent];

    internal void Show(float y, float value)
    {
        float labelY = y + (Height - EditorUi.RowHeight) * 0.5f;
        float barY = y + (Height - BarHeight) * 0.5f;
        float center = BarX + BarWidth * 0.5f;
        float fill = BarWidth * 0.5f * Mathf.Clamp(value, -1f, 1f);

        EditorUi.Place(_name, new Vector2(0f, labelY));
        EditorUi.Place(_barBack, new Vector2(BarX, barY));
        EditorUi.Place(_barFill, new Vector2(fill < 0f ? center + fill : center, barY));
        EditorUi.Place(_center, new Vector2(center, barY - EditorUi.ListMarkOverhang));
        EditorUi.Place(_percent, new Vector2(PercentX, labelY));

        _barFill.scale = new Vector2(Mathf.Abs(fill), BarHeight);
        _barFill.color = ColorOf(value);
        _percent.text = EditorUi.PercentText(value);
        _percent.color = _barFill.color;
    }

    private static Color ColorOf(float value) => value switch
    {
        > 0.005f => Palette.Friendly,
        < -0.005f => Palette.Unfriendly,
        _ => Palette.Neutral,
    };
}
