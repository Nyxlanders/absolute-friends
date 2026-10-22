using RWCustom;
using UnityEngine;

namespace AbsoluteFriends.Visualizer;

internal class SubjectTag
{
    public SideTag Friend { get; } = new();

    public SideTag Owner { get; } = new();

    public IEnumerable<FNode> Nodes => [.. Friend.Nodes, .. Owner.Nodes];

    public bool IsVisible
    {
        set
        {
            Friend.IsVisible = value;
            Owner.IsVisible = value;
        }
    }

    public bool IsDistant
    {
        set
        {
            Friend.IsDistant = value;
            Owner.IsDistant = value;
        }
    }

    public void Clear()
    {
        Friend.Clear();
        Owner.Clear();
    }
}

internal class SideTag
{
    private const string Placeholder = "Futile_White";

    private const float TextSize = 0.75f;

    private const float IconSize = 24f;

    private const float IconOffset = 4f;

    private const int Front = 2;

    private static readonly Vector2[] _offsets = [new(1f, -1f), new(-1f, 1f), Vector2.zero];

    private readonly FLabel[] _names = [CreateLabel(true), CreateLabel(true), CreateLabel(false)];

    private readonly FSprite[] _icons = [CreateSprite(true), CreateSprite(true), CreateSprite(false)];

    private string _symbolName = Placeholder;

    private bool _isFound;

    private bool _isDistant;

    private bool _hasName;

    private bool _hasIcon;

    public IEnumerable<FNode> Nodes => [.. _names, .. _icons];

    public bool IsVisible
    {
        set
        {
            Show(_names, value && _hasName);
            Show(_icons, value && _hasIcon && _isFound);
        }
    }

    public bool IsDistant
    {
        set
        {
            if (_isDistant != value)
            {
                _isDistant = value;
                Name.IsHologram = value;
                Icon.IsHologram = value;
            }
        }
    }

    private FLabel Name => _names[Front];

    private FSprite Icon => _icons[Front];

    public void Clear()
    {
        _hasName = false;
        _hasIcon = false;
    }

    public void SetName(string text, Color color, Vector2 position, float gap)
    {
        _hasName = true;
        Name.color = color;

        if (Name.text != text)
        {
            foreach (var label in _names)
            {
                label.text = text;
            }
        }

        MoveTo(_names, position + new Vector2(0f, gap));
    }

    public void SetIcon(string symbolName, Color color, Vector2 position, float gap)
    {
        _hasIcon = true;
        Icon.color = color;

        if (_symbolName != symbolName)
        {
            _symbolName = symbolName;
            _isFound = Futile.atlasManager.DoesContainElementWithName(symbolName);

            if (_isFound)
            {
                foreach (var sprite in _icons)
                {
                    sprite.FitSymbol(symbolName, IconSize);
                }
            }
        }

        MoveTo(_icons, position + new Vector2(0f, gap - IconOffset));
    }

    private static void Show(FNode[] nodes, bool isVisible)
    {
        foreach (var node in nodes)
        {
            node.isVisible = isVisible;
        }
    }

    private static void MoveTo(FNode[] nodes, Vector2 position)
    {
        for (int index = 0; index < nodes.Length; ++index)
        {
            nodes[index].SetPosition(position + _offsets[index]);
        }
    }

    private static FLabel CreateLabel(bool isShadow) => new(Custom.GetDisplayFont(), "")
    {
        anchorX = 0.5f,
        anchorY = 0.5f,
        scale = TextSize,
        color = isShadow ? Color.black : Color.white,
        isVisible = false,
    };

    private static FSprite CreateSprite(bool isShadow) => new(Placeholder, true)
    {
        color = isShadow ? Color.black : Color.white,
        isVisible = false,
    };
}
