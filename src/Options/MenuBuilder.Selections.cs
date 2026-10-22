using Menu.Remix.MixedUI;
using UnityEngine;

namespace AbsoluteFriends.Options;

public abstract partial class MenuBuilder
{
    public OpComboBox? AddComboBox(Configurable<string> configurable, string[] choices, string? text = null, float span = 1f, bool enabled = true)
    {
        return AddComboBox(configurable, [.. choices.Select(choice => new ListItem(choice))], text, span, enabled);
    }

    public OpComboBox? AddComboBox(Configurable<string> configurable, ListItem[] choices, string? text = null, float span = 1f, bool enabled = true)
    {
        return AddComboBox(configurable, choices, static (option, position, width, items) => new OpComboBox(option, position, width, items), text, span, enabled);
    }

    public OpComboBox? AddComboBox(Configurable<string> configurable, ListItem[] choices, Action<ListItem, FLabel, bool> styleItem, string? text = null, float span = 1f, bool enabled = true)
    {
        if (styleItem == null)
        {
            throw new ArgumentNullException(nameof(styleItem));
        }

        return AddComboBox(configurable, choices, (option, position, width, items) => new StyledComboBox(option, position, width, items, styleItem), text, span, enabled);
    }

    public OpComboBox? AddComboBox(Configurable<string> configurable, ListItem[] choices, Func<Configurable<string>, Vector2, float, List<ListItem>, OpComboBox> create, string? text = null, float span = 1f, bool enabled = true)
    {
        if (choices.Length == 0 || !TryBeginControl(configurable, text, span, enabled, out ControlLayout layout))
        {
            return null;
        }

        OpComboBox comboBox = create(
            configurable,
            layout.Position,
            layout.Width,
            [.. choices]
        );

        comboBox.description = layout.Description;

        Watch(() =>
        {
            if (comboBox.held)
            {
                comboBox.MoveToFront();
            }
        });

        return CompleteControl(configurable, comboBox, layout.Label, span);
    }

    public OpResourceSelector? AddResourceSelector(ConfigurableBase configurable, string? text = null, float span = 1f, bool enabled = true)
    {
        return AddControl(configurable, text, span, enabled, layout => new OpResourceSelector(
            configurable,
            layout.Position,
            layout.Width
        )
        {
            description = layout.Description
        });
    }

    public OpResourceSelector? AddResourceSelector(Configurable<string> configurable, OpResourceSelector.SpecialEnum resource, string? text = null, float span = 1f, bool enabled = true)
    {
        return AddControl(configurable, text, span, enabled, layout => new OpResourceSelector(
            configurable,
            layout.Position,
            layout.Width,
            resource
        )
        {
            description = layout.Description
        });
    }

    public OpListBox? AddListBox(Configurable<string> configurable, string[] choices, string? text = null, ushort visibleItems = 5, float? span = null, bool enabled = true)
    {
        return AddListBox(configurable, [.. choices.Select(choice => new ListItem(choice))], text, visibleItems, span, enabled);
    }

    public OpListBox? AddListBox(Configurable<string> configurable, ListItem[] choices, string? text = null, ushort visibleItems = 5, float? span = null, bool enabled = true)
    {
        return AddListBox(configurable, choices, static (option, position, width, items, count) => new OpListBox(option, position, width, items, count, true), text, visibleItems, span, enabled);
    }

    public OpListBox? AddListBox(Configurable<string> configurable, ListItem[] choices, Action<ListItem, FLabel, bool> styleItem, string? text = null, ushort visibleItems = 5, float? span = null, bool enabled = true)
    {
        if (styleItem == null)
        {
            throw new ArgumentNullException(nameof(styleItem));
        }

        return AddListBox(configurable, choices, (option, position, width, items, count) => new StyledListBox(option, position, width, items, count, styleItem), text, visibleItems, span, enabled);
    }

    public OpListBox? AddListBox(Configurable<string> configurable, ListItem[] choices, Func<Configurable<string>, Vector2, float, List<ListItem>, ushort, OpListBox> create, string? text = null, ushort visibleItems = 5, float? span = null, bool enabled = true)
    {
        if (choices.Length == 0)
        {
            return null;
        }

        return AddListControl(
            configurable,
            text,
            visibleItems,
            span,
            enabled,
            (position, width) => create(configurable, position, width, [.. choices], visibleItems),
            listBox => listBox._listHeight
        );
    }

    public OpResourceList? AddResourceList(ConfigurableBase configurable, string? text = null, ushort visibleItems = 5, float? span = null, bool enabled = true)
    {
        return AddListControl(
            configurable,
            text,
            visibleItems,
            span,
            enabled,
            (position, width) => new OpResourceList(configurable, position, width, visibleItems, true),
            resourceList => resourceList._listHeight
        );
    }

    public OpResourceList? AddResourceList(Configurable<string> configurable, OpResourceSelector.SpecialEnum resource, string? text = null, ushort visibleItems = 5, float? span = null, bool enabled = true)
    {
        return AddListControl(
            configurable,
            text,
            visibleItems,
            span,
            enabled,
            (position, width) => new OpResourceList(configurable, position, width, resource, visibleItems, true),
            resourceList => resourceList._listHeight
        );
    }

    private static float ListBoxHeight(int visibleItems)
    {
        return visibleItems * 20f + 34f;
    }

    private T? AddListControl<T>(
        ConfigurableBase configurable,
        string? text,
        ushort visibleItems,
        float? span,
        bool enabled,
        Func<Vector2, float, T> create,
        Func<T, int> measure
    ) where T : UIfocusable
    {
        if (_currentTab == null || !enabled || visibleItems == 0)
        {
            return null;
        }

        BeginBlock(span);

        string description = DescriptionOf(configurable);
        float width = BlockWidth(span);

        if (text != null)
        {
            AddBlockLabel(text, description, width);
        }

        float top = _pos.y + Spacing * 0.5f;
        T list = create(new Vector2(_pos.x + Gap, _pos.y), width - Gap * 2f);

        list.description = description;

        float height = ListBoxHeight(measure(list));

        list.SetPos(new Vector2(list.GetPos().x, top - height));

        AddElements(list);
        Register(configurable, list);

        _pos.y = top - height;

        AddRow(0.5f);

        return list;
    }
}
