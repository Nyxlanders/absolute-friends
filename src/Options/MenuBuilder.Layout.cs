using AbsoluteFriends.Utils;
using Menu.Remix.MixedUI;
using UnityEngine;

namespace AbsoluteFriends.Options;

public abstract partial class MenuBuilder
{
    public void SetColumns(int columns)
    {
        if (columns <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(columns), "Column count must be greater than zero.");
        }

        FinishRow();
        _columns = columns;
    }

    public void AddParallelColumns(params Action[] columns)
    {
        if (columns == null)
        {
            throw new ArgumentNullException(nameof(columns));
        }

        if (columns.Length < 2)
        {
            throw new ArgumentException("At least two columns are required.", nameof(columns));
        }

        if (columns.Any(column => column == null))
        {
            throw new ArgumentException("Column callbacks cannot be null.", nameof(columns));
        }

        if (_currentTab == null)
        {
            return;
        }

        FinishRow();

        Vector2 margins = _marginX;
        int previousColumns = _columns;
        float top = _pos.y;
        float edge = _edge;
        float columnWidth = (margins.y - margins.x) / columns.Length;
        float bottom = top;

        void BuildColumn(Action content, Vector2 columnMargins)
        {
            _marginX = columnMargins;
            _columns = 1;
            _pos = new(columnMargins.x, top);
            _currentColumn = 0f;
            _edge = top;

            content();
            FinishRow();

            bottom = Mathf.Min(bottom, Mathf.Min(_pos.y, _edge));
        }

        try
        {
            for (int index = 0; index < columns.Length; index++)
            {
                float left = margins.x + columnWidth * index + (index == 0 ? 0f : Gap);
                float right = margins.x + columnWidth * (index + 1) - (index == columns.Length - 1 ? 0f : Gap);

                BuildColumn(columns[index], new(left, right));
            }
        }
        finally
        {
            _marginX = margins;
            _columns = previousColumns;
            _pos = new(margins.x, bottom);
            _edge = Mathf.Min(edge, bottom);
            _currentColumn = 0f;
        }
    }

    public void AddColumn(float span = 1f)
    {
        ValidateSpan(span);
        _pos.x += ElementWidth * span;

        if ((_currentColumn += span) > _columns - 0.5f)
        {
            FinishRow();
        }
    }

    public void AddRow(float modifier = 1f)
    {
        _pos.x = _marginX.x;
        _pos.y -= modifier * Spacing;
        _currentColumn = 0;
    }

    public OpRect? AddContainer(Action content)
    {
        if (_currentTab == null || _box != null)
        {
            return null;
        }

        BeginBox();

        OpRect? container = null;

        try
        {
            content();
        }
        finally
        {
            container = EndBox();
        }

        return container;
    }

    public OpLabel? AddLabel(string text, bool bigText = false, FLabelAlignment alignment = FLabelAlignment.Center, bool enabled = true)
    {
        return AddLabel(text, null, bigText, alignment, enabled);
    }

    public OpLabel? AddLabel(string text, string value, FLabelAlignment alignment = FLabelAlignment.Center)
    {
        return AddLabel(text, value, false, alignment, true);
    }

    public OpLabel? AddNote(string text, FLabelAlignment alignment = FLabelAlignment.Right, float span = 1f, bool enabled = true)
    {
        return AddNote(text, null, alignment, span, enabled);
    }

    public OpLabel? AddNote(string text, string value, FLabelAlignment alignment = FLabelAlignment.Right, float span = 1f)
    {
        return AddNote(text, value, alignment, span, true);
    }

    public void AddTitle(string? title = null, string? description = null, FLabelAlignment alignment = FLabelAlignment.Center, bool enabled = true)
    {
        if (!enabled)
        {
            return;
        }

        FinishRow();
        _pos = new(_marginX.x, _edge - Spacing);

        if (title != null)
        {
            AddLabel(title, bigText: true, alignment: alignment);
        }
        if (description != null)
        {
            AddLabel(description, alignment: alignment);
        }
    }

    public OpLabelLong? AddParagraph(string text, float height, float? span = null, FLabelAlignment alignment = FLabelAlignment.Left, bool enabled = true)
    {
        if (_currentTab == null || !enabled || height <= 0f)
        {
            return null;
        }

        BeginBlock(span);

        float width = BlockWidth(span);
        OpLabelLong paragraph = new(
            new Vector2(_pos.x + Gap, _pos.y - height),
            new Vector2(width - Gap * 2f, height),
            Translate(text),
            true,
            alignment
        );

        AddElements(paragraph);
        _pos.y -= height;
        AddRow(0.5f);

        return paragraph;
    }

    private protected void BeginBox()
    {
        if (_currentTab == null || _box != null)
        {
            return;
        }

        FinishRow();
        _box = new(new Vector2(_marginX.x, _pos.y), new Vector2(Width, Spacing));
        AddElements(_box);
        _boxIndex = _elements.Count;
        _marginX = new(_marginX.x + Gap * 2f, _marginX.y - Gap * 2f);
        AddRow(0.5f);
    }

    private protected OpRect? EndBox()
    {
        if (_box is not { } box)
        {
            return null;
        }

        FinishRow();

        float top = box.GetPos().y;
        float bottom = float.MaxValue;

        foreach (var element in _elements.Skip(_boxIndex).Where(element => element.isRectangular))
        {
            top = Mathf.Max(top, element.GetPos().y + element.size.y);
            bottom = Mathf.Min(bottom, element.GetPos().y);
        }

        if (bottom > top)
        {
            bottom = _pos.y;
        }

        top += Gap;
        bottom -= Gap;
        _marginX = new(box.GetPos().x, box.GetPos().x + box.size.x);
        _box = null;
        box.SetPos(new(box.GetPos().x, bottom));
        box.size = new(box.size.x, top - bottom);
        _edge = bottom;
        _pos.y = bottom;
        AddRow(0.5f);

        return box;
    }

    private protected OpLabel? AddDescription(string text)
    {
        if (_currentTab == null || text.Length == 0)
        {
            return null;
        }

        FinishRow();

        _pos.y += Spacing * 0.5f;

        return AddLabel(text, null, false, FLabelAlignment.Left, true);
    }

    private void FinishRow()
    {
        if (_currentColumn > 0)
        {
            AddRow(1.5f);
        }
    }

    private OpLabel? AddLabel(string text, string? value, bool bigText, FLabelAlignment alignment, bool enabled)
    {
        if (_currentTab == null || !enabled)
        {
            return null;
        }

        FinishRow();

        string content = value == null ? Translate(text) : Translate(text).FillPlaceholders(value);
        OpLabel label = new(
            new(_pos.x, _pos.y - Spacing * 0.5f),
            new(Width, Spacing),
            content,
            alignment,
            bigText
        )
        {
            autoWrap = true,
        };

        label.Change();

        float extraHeight = LabelTest.LineHeight(bigText) * (label.GetLineCount() - 1);

        if (extraHeight > 0f)
        {
            label.pos -= new Vector2(0f, extraHeight);
            label.size += new Vector2(0f, extraHeight);
        }

        AddElements(label);
        AddRow(1f + extraHeight / Spacing);

        return label;
    }

    private OpLabel? AddNote(string text, string? value, FLabelAlignment alignment, float span, bool enabled)
    {
        if (!enabled || !BeginElement(span))
        {
            return null;
        }

        string content = Translate(text);

        if (value != null)
        {
            content = content.FillPlaceholders(value);
        }

        OpLabel label = new(
            new Vector2(_pos.x + Gap, _pos.y - Spacing * 0.5f),
            new(ElementWidth * span - Gap * 2f, Spacing),
            content,
            alignment
        );

        AddColumn(span);
        AddElements(label);

        return label;
    }

    private OpLabel AddBlockLabel(string text, string description, float width)
    {
        OpLabel label = new(
            new Vector2(_pos.x + Gap, _pos.y - Spacing * 0.5f),
            new Vector2(width - Gap * 2f, Spacing),
            Translate(text),
            FLabelAlignment.Left
        )
        {
            description = description
        };

        AddElements(label);
        _pos.y -= Spacing;

        return label;
    }

    private void BeginBlock(float? span)
    {
        if (span is { } blockSpan)
        {
            BeginElement(blockSpan);
        }
        else
        {
            FinishRow();
        }
    }

    private float BlockWidth(float? span)
    {
        return span is > 0f ? Mathf.Min(Width, ElementWidth * span.Value) : Width;
    }

    private bool BeginElement(float span)
    {
        if (_currentTab == null)
        {
            return false;
        }

        ValidateSpan(span);

        if (_currentColumn > _columns - span + 0.5f)
        {
            FinishRow();
        }

        return true;
    }

    private void ValidateSpan(float span)
    {
        if (span <= 0f || span > _columns || float.IsNaN(span) || float.IsInfinity(span))
        {
            throw new ArgumentOutOfRangeException(nameof(span), $"Span must be finite, greater than zero, and no larger than the current column count ({_columns}).");
        }
    }

    private void AddElements(params UIelement[] elements)
    {
        if (_currentTab == null)
        {
            return;
        }

        _elements.AddRange(elements);

        foreach (var element in elements)
        {
            _edge = Mathf.Min(_edge, element.GetPos().y);
        }
    }

}
