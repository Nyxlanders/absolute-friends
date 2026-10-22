using Menu.Remix.MixedUI;
using UnityEngine;

namespace AbsoluteFriends.Options;

public abstract partial class MenuBuilder
{
    public OpCheckBox? AddCheckBox(Configurable<bool> configurable, string? text = null, float span = 1f, bool enabled = true)
    {
        if (!enabled)
        {
            return null;
        }

        return AddBox(configurable, () => new OpCheckBox(
            configurable,
            new Vector2(_pos.x, _pos.y - Spacing * 0.5f)
        ), text, span);
    }

    public OpRadioButtonGroup? AddRadioButtonGroup(Configurable<int> configurable, bool enabled = true)
    {
        EndRadioButtonGroup();

        if (_currentTab == null || !enabled)
        {
            return null;
        }

        OpRadioButtonGroup radioButtonGroup = new(configurable)
        {
            description = DescriptionOf(configurable)
        };

        Register(configurable, radioButtonGroup);

        return _radioButtonGroup = radioButtonGroup;
    }

    public OpRadioButton? AddRadioButton(string text, string? description = null, float span = 1f, bool enabled = true)
    {
        return AddRadioButton(text, out _, description, span, enabled);
    }

    public OpSlider? AddIntSlider(Configurable<int> configurable, string? text = null, float span = 1f, int? min = null, int? max = null, bool enabled = true)
    {
        return AddControl(configurable, text, span, enabled, layout => new OpSlider(
            configurable,
            layout.Position - new Vector2(0f, 3f),
            (int)layout.Width
        )
        {
            description = layout.Description,
            min = MinOf(configurable, min, 0),
            max = MaxOf(configurable, max, 10),
        });
    }

    public OpSliderTick? AddIntSliderTick(Configurable<int> configurable, string? text = null, float span = 1f, int? min = null, int? max = null, bool enabled = true)
    {
        return AddControl(configurable, text, span, enabled, layout => new OpSliderTick(
            configurable,
            layout.Position - new Vector2(0f, 3f),
            (int)layout.Width,
            false
        )
        {
            description = layout.Description,
            min = MinOf(configurable, min, 0),
            max = MaxOf(configurable, max, 10),
        });
    }

    public OpLabel[] AddTickLabels(OpSliderTick? slider, params string[] labels)
    {
        if (_currentTab == null || slider == null || labels.Length == 0)
        {
            return [];
        }

        FinishRow();

        float step = labels.Length > 1 ? slider._mul * (slider.max - slider.min) / (labels.Length - 1) : slider.size.x;
        OpLabel[] tickLabels = new OpLabel[labels.Length];

        for (int index = 0; index < labels.Length; ++index)
        {
            float left = Mathf.Clamp(slider.pos.x + step * index - step * 0.5f, _marginX.x, _marginX.y - step);
            FLabelAlignment alignment = index == 0 && labels.Length > 1 ? FLabelAlignment.Left : index == labels.Length - 1 && labels.Length > 1 ? FLabelAlignment.Right : FLabelAlignment.Center;

            if (alignment == FLabelAlignment.Left)
            {
                left = Mathf.Max(_marginX.x, slider.pos.x - Gap);
            }
            else if (alignment == FLabelAlignment.Right)
            {
                left = Mathf.Min(_marginX.y, slider.pos.x + slider.size.x + Gap) - step;
            }

            tickLabels[index] = new(
                new Vector2(left, slider.pos.y - Spacing * 0.75f),
                new Vector2(step, Spacing),
                Translate(labels[index]),
                alignment
            )
            {
                description = slider.description
            };
        }

        AddElements(tickLabels);
        AddRow(0.75f);

        return tickLabels;
    }

    public OpFloatSlider? AddFloatSlider(Configurable<float> configurable, string? text = null, float span = 1f, float? min = null, float? max = null, byte decimals = 2, int increment = 1, bool enabled = true)
    {
        return AddControl(configurable, text, span, enabled, layout => new OpFloatSlider(
            configurable,
            layout.Position - new Vector2(0f, 3f),
            (int)layout.Width,
            decimals
        )
        {
            description = layout.Description,
            min = MinOf(configurable, min, 0f),
            max = MaxOf(configurable, max, 1f),
            Increment = increment,
        });
    }

    public OpDragger? AddDragger(Configurable<int> configurable, string? text = null, float span = 1f, int? min = null, int? max = null, bool enabled = true)
    {
        if (!enabled)
        {
            return null;
        }

        return AddBox(configurable, () => new OpDragger(
            configurable,
            new Vector2(_pos.x, _pos.y - Spacing * 0.5f)
        )
        {
            min = MinOf(configurable, min, 0),
            max = MaxOf(configurable, max, 10),
        }, text, span);
    }

    public OpUpdown? AddIntUpdown(Configurable<int> configurable, string? text = null, float span = 1f, int increment = 1, bool enabled = true)
    {
        return AddControl(configurable, text, span, enabled, layout => new OpUpdown(
            configurable,
            layout.Position,
            layout.Width
        )
        {
            description = layout.Description,
            Increment = increment,
        });
    }

    public OpUpdown? AddFloatUpdown(Configurable<float> configurable, string? text = null, float span = 1f, byte decimals = 2, int increment = 1, bool enabled = true)
    {
        return AddControl(configurable, text, span, enabled, layout => new OpUpdown(
            configurable,
            layout.Position,
            layout.Width,
            decimals
        )
        {
            description = layout.Description,
            Increment = increment,
        });
    }

    public OpTextBox? AddTextBox(ConfigurableBase configurable, string? text = null, float span = 1f, bool enabled = true)
    {
        return AddControl(configurable, text, span, enabled, layout => new OpTextBox(
            configurable,
            layout.Position,
            layout.Width
        )
        {
            description = layout.Description
        });
    }

    public OpKeyBinder? AddKeyBinder(Configurable<KeyCode> configurable, string? text = null, float span = 1f, bool enabled = true)
    {
        return AddControl(configurable, text, span, enabled, layout => new OpKeyBinder(
            configurable,
            layout.Position,
            new(layout.Width, Spacing)
        )
        {
            description = layout.Description
        });
    }

    public OpColorPicker? AddColorPicker(Configurable<Color> configurable, string? text = null, float? span = null, bool enabled = true)
    {
        if (_currentTab == null || !enabled)
        {
            return null;
        }

        BeginBlock(span);

        string description = DescriptionOf(configurable);
        float width = BlockWidth(span);
        OpLabel? label = text == null ? null : AddBlockLabel(text, description, width);
        float top = _pos.y + (label == null ? 0f : Spacing * 0.5f);
        OpColorPicker colorPicker = new(configurable, new Vector2(_pos.x + Gap, top - 150f))
        {
            description = description
        };

        AddElements(colorPicker);
        Register(configurable, colorPicker, label);
        _pos.y = top - 150f;
        AddRow(0.5f);

        return colorPicker;
    }

    private protected OpRadioButtonGroup? AddRadioButtonRow(Configurable<int> configurable, string text, params (string Text, string Description)[] choices)
    {
        OpRadioButtonGroup? radioButtonGroup = AddRadioButtonGroup(configurable);

        if (radioButtonGroup == null || !BeginElement(1f))
        {
            return radioButtonGroup;
        }

        AddControlLabel(radioButtonGroup.description, text, 1f, out _);
        AddColumn(1f);

        foreach (var (choice, description) in choices)
        {
            AddRadioButton(choice, out _, description, span: 2f, indent: Gap);
        }

        return radioButtonGroup;
    }

    private protected OpRadioButton? AddRadioButton(string text, out OpLabel? label, string? description = null, float span = 1f, bool enabled = true, float indent = 0f)
    {
        label = null;

        if (_radioButtonGroup == null || !enabled || !BeginElement(span))
        {
            return null;
        }

        string note = Translate(description ?? "");
        OpRadioButton radioButton = new(_pos.x + indent, _pos.y - Spacing * 0.5f)
        {
            description = note
        };

        label = new(
            new Vector2(_pos.x + indent + Spacing + Gap, _pos.y - Spacing * 0.5f),
            new(ElementWidth * span - indent - Spacing - Gap, Spacing),
            Translate(text),
            FLabelAlignment.Left
        )
        {
            description = note
        };

        AddColumn(span);
        AddElements(radioButton, label);
        _radioButtons.Add(radioButton);

        return radioButton;
    }

    private protected SectionFilter? AddSearchBox(string key, string text, string description, float span = 1f)
    {
        if (!BeginElement(span))
        {
            return null;
        }

        string note = Translate(description);

        AddControlLabel(note, text, span, out float labelWidth);

        SectionFilter filter = _filter ??= new();
        OpTextBox searchBox = new(
            Bind(key, "", new ConfigurableInfo(note)),
            new Vector2(_pos.x + Gap + labelWidth, _pos.y - Spacing * 0.5f),
            ElementWidth * span - Gap * 2f - labelWidth
        )
        {
            description = note,
            allowSpace = true,
        };
        string? pending = null;

        searchBox.OnChange += delegate
        {
            pending = searchBox.value;
        };

        _externals.Add(delegate
        {
            if (pending is { } query)
            {
                pending = null;
                filter.Apply(query);
            }
        });

        filter.Restore(searchBox.value);
        AddColumn(span);
        AddElements(searchBox);

        return filter;
    }

    private static T MinOf<T>(Configurable<T> configurable, T? min, T fallback) where T : struct, IComparable
    {
        return min ?? (configurable.info?.acceptable is ConfigAcceptableRange<T> range ? range.MinValue : fallback);
    }

    private static T MaxOf<T>(Configurable<T> configurable, T? max, T fallback) where T : struct, IComparable
    {
        return max ?? (configurable.info?.acceptable is ConfigAcceptableRange<T> range ? range.MaxValue : fallback);
    }

    private T? AddBox<T>(ConfigurableBase configurableBase, Func<T> create, string? text, float span) where T : UIfocusable
    {
        if (!BeginElement(span))
        {
            return null;
        }

        string description = DescriptionOf(configurableBase);
        T box = create();

        box.description = description;

        OpLabel? label = null;

        if ((text ?? configurableBase.Label) is { } labelText)
        {
            label = new(
                new Vector2(_pos.x + Spacing + Gap, _pos.y - Spacing * 0.5f),
                new(ElementWidth * span - Spacing - Gap, Spacing),
                LabelOf(configurableBase, labelText),
                FLabelAlignment.Left
            )
            {
                description = description
            };

            AddElements(box, label);
        }
        else
        {
            AddElements(box);
        }

        AddColumn(span);
        Register(configurableBase, box, label);

        return box;
    }

    private T? AddControl<T>(ConfigurableBase configurable, string? text, float span, bool enabled, Func<ControlLayout, T> create) where T : UIfocusable
    {
        if (!TryBeginControl(configurable, text, span, enabled, out ControlLayout layout))
        {
            return null;
        }

        return CompleteControl(configurable, create(layout), layout.Label, span);
    }

    private bool TryBeginControl(ConfigurableBase configurable, string? text, float span, bool enabled, out ControlLayout layout)
    {
        layout = default;

        if (!enabled || !BeginElement(span))
        {
            return false;
        }

        string description = DescriptionOf(configurable);
        OpLabel? label = AddControlLabel(description, text, span, out float labelWidth, IsMarkedDisabledInMeadow(configurable));

        layout = new(
            description,
            label,
            new Vector2(_pos.x + Gap + labelWidth, _pos.y - Spacing * 0.5f),
            ElementWidth * span - Gap * 2f - labelWidth
        );

        return true;
    }

    private T CompleteControl<T>(ConfigurableBase configurable, T control, OpLabel? label, float span) where T : UIfocusable
    {
        AddColumn(span);
        AddElements(control);
        Register(configurable, control, label);

        return control;
    }

    private OpLabel? AddControlLabel(string description, string? text, float span, out float labelWidth, bool meadowMarked = false)
    {
        labelWidth = 0f;

        if (text == null)
        {
            return null;
        }

        labelWidth = Mathf.Min(ElementWidth, ElementWidth * span * 0.5f);

        OpLabel label = new(
            new Vector2(_pos.x + Gap, _pos.y - Spacing * 0.5f),
            new(labelWidth - Gap * 2f, Spacing),
            WithMeadowMark(Translate(text), meadowMarked),
            FLabelAlignment.Left
        )
        {
            description = description
        };

        AddElements(label);

        return label;
    }

    private readonly struct ControlLayout(string description, OpLabel? label, Vector2 position, float width)
    {
        internal string Description { get; } = description;

        internal OpLabel? Label { get; } = label;

        internal Vector2 Position { get; } = position;

        internal float Width { get; } = width;
    }
}
