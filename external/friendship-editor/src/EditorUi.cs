using AbsoluteFriends.Options;
using AbsoluteFriends.Utils;
using Menu;
using Menu.Remix.MixedUI;
using UnityEngine;

namespace AbsoluteFriends.FriendshipEditor;

internal static class EditorUi
{
    internal const float RowHeight = 24f;

    internal const string Inherit = "Inherit";

    internal const float ListRowHeight = 30f;

    internal const float ListNameWidth = 220f;

    internal const float ListBarX = 228f;

    internal const float ListBarWidth = 241f;

    internal const float ListBarHeight = 6f;

    internal const float ListMarkOverhang = 2f;

    internal const float ListPercentX = 475f;

    internal const float ListPercentWidth = 40f;

    private const float SliderOffset = 3f;

    internal static OpLabel Label(Action<UIelement> add, string text, Vector2 position, float width, FLabelAlignment alignment = FLabelAlignment.Left, bool grey = false)
    {
        OpLabel label = new(position, new Vector2(width, RowHeight), text, alignment);

        if (grey)
        {
            label.color = MenuColorEffect.rgbMediumGrey;
        }

        add(label);

        return label;
    }

    internal static ScrollArea List(Action<UIelement> add, float width, string title, float y)
    {
        float height = y - 6f;
        ScrollArea list = new(Vector2.zero, new Vector2(width + MenuBuilder.Padding, height));

        add(new OpLabel(new Vector2(0f, height + 4f), new Vector2(width, 30f), title, FLabelAlignment.Center, true));
        add(list);

        return list;
    }

    internal static OpComboBox Combo(Action<UIelement> add, IEnumerable<(string Key, string Name)> items, string selected, Vector2 position, float width, string description)
    {
        List<ListItem> list = [.. items.Select((item, index) => new ListItem(item.Key, item.Name, index))];
        OpComboBox combo = new(new Configurable<string>(list.Any(item => item.name == selected) ? selected : list[0].name), position, width, list)
        {
            description = description
        };

        add(combo);

        return combo;
    }

    internal static OpSlider Slider(Action<UIelement> add, Vector2 position, float width, int minimum, int initial = 0)
    {
        OpSlider slider = new(new Configurable<int>(initial, new ConfigurableInfo(string.Empty, new ConfigAcceptableRange<int>(minimum, 100))), position - new Vector2(0f, SliderOffset), (int)width)
        {
            min = minimum,
            max = 100,
        };

        add(slider);

        return slider;
    }

    internal static OpImage Pixel(Color color)
    {
        return new OpImage(Vector2.zero, "pixel")
        {
            color = color
        };
    }

    internal static IEnumerable<(string Key, string Name)> ModeItems(bool topLevel) =>
    [
        (Inherit, Translation.Of(topLevel ? "No Rule" : "Inherit")),
        .. ((RuleMode[])[RuleMode.Vanilla, RuleMode.Minimum, RuleMode.Maximum, RuleMode.Fixed])
            .Where(mode => !topLevel || mode != RuleMode.Vanilla)
            .Select(mode => (mode.ToString(), Translation.Of(mode == RuleMode.Vanilla ? "No Rule" : mode.ToString()))),
    ];

    internal static IEnumerable<(string Key, string Name)> RelationshipItems(bool topLevel) =>
    [
        .. ModeItems(topLevel).Where(item => item.Key is Inherit or nameof(RuleMode.Vanilla)),
        .. RelationshipTypes.Editable.Select(type => (type.value, type.value)),
    ];

    internal static void KeepOnTop(IEnumerable<UIelement> elements)
    {
        List<UIelement> list = [.. elements];

        foreach (var element in list.Where(element => element is OpSlider))
        {
            element.MoveToFront();
        }

        foreach (var element in list.Where(element => element is UIfocusable { held: true }))
        {
            element.MoveToFront();
        }
    }

    internal static string StatName(Stat stat) => Translation.Of(stat switch
    {
        Stat.Reputation => "Reputation",
        Stat.Like => "Like",
        Stat.TempLike => "Temp Like",
        Stat.Relationship => "Relationship",
        _ => "Know",
    });

    internal static string RuleText(Rule rule) => rule.Enforces
        ? $"{rule.Relation ?? Translation.Of(rule.Mode.ToString())} {PercentText(rule.Value)}"
        : Translation.Of("No Rule");

    internal static string PercentText(float value) => $"{Mathf.RoundToInt(value * 100f)}%";

    internal static int PercentOf(UIconfig config) => int.TryParse(config.value, out int value) ? value : 0;

    internal static void ShowValue(UIconfig config, string value)
    {
        if (config.value != value)
        {
            config.ForceValue(value);
            config.Change();
        }
    }

    internal static void SetItems(OpComboBox combo, IEnumerable<(string Key, string Name)> items, string selected)
    {
        ListItem[] list = [.. items.Select((item, index) => new ListItem(item.Key, item.Name, index))];

        combo._itemList = list;

        if (list.All(item => item.name != selected))
        {
            selected = list[0].name;
        }

        combo.ForceValue(selected);
        combo._ResetIndex();
        combo.Change();
        combo._lblText.text = LabelTest.TrimText(combo._GetDisplayValue(), combo.size.x - 32f, true);
    }

    internal static void SetShown(UIelement element, bool shown)
    {
        if (shown && element.Hidden)
        {
            element.Show();
            Snap(element);
        }
        else if (!shown && !element.Hidden)
        {
            element.Hide();
        }
    }

    internal static void Place(UIelement element, Vector2 position)
    {
        if (element.GetPos() != position)
        {
            element.SetPos(position);
            Snap(element);
        }
    }

    internal static void FitIcon(OpImage icon)
    {
        Vector2 size = icon.sprite.element.sourcePixelSize;
        float scale = Mathf.Min(1f, 24f / Mathf.Max(size.x, size.y));

        icon.scale = new Vector2(scale, scale);
    }

    internal static string Trim(string text, float width) => LabelTest.GetWidth(text) > width ? LabelTest.TrimText(text, width, true) : text;

    private static void Snap(UIelement element)
    {
        element.lastScreenPos = element.ScreenPos;
        element.GrafUpdate(1f);
    }
}
