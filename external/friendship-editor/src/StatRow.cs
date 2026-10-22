using AbsoluteFriends.Manager;
using AbsoluteFriends.Utils;
using Menu;
using Menu.Remix.MixedUI;
using UnityEngine;

namespace AbsoluteFriends.FriendshipEditor;

internal class StatRow
{
    internal const float LabelWidth = 90f;

    internal const float Height = 34f;

    private const float ModeX = LabelWidth;

    private const float ModeWidth = 150f;

    private const float ValueX = ModeX + ModeWidth + 16f;

    private const float PercentWidth = 44f;

    private const int DefaultIntensity = 50;

    private readonly OpComboBox _mode;

    private readonly OpSlider _value;

    private readonly OpLabel _percent;

    private Rule? _resolved;

    internal StatRow(EditorView view, Stat stat, float y)
    {
        float width = view.Tab.Size.x;

        Stat = stat;
        EditorUi.Label(view.Add, EditorUi.StatName(stat), new Vector2(0f, y), LabelWidth);
        _mode = EditorUi.Combo(view.Add, Items(false), EditorUi.Inherit, new Vector2(ModeX, y), ModeWidth,
            Translation.Of(stat == Stat.Relationship ? "Sets the relationship and its intensity." : "Sets the rule for this value."));
        _mode.OnValueUpdate += delegate { Refresh(_resolved); };
        _value = EditorUi.Slider(view.Add, new Vector2(ValueX, y), width - PercentWidth - 8f - ValueX, stat is Stat.Know or Stat.Relationship ? 0 : -100,
            stat == Stat.Relationship ? DefaultIntensity : 0);
        _percent = EditorUi.Label(view.Add, string.Empty, new Vector2(width - PercentWidth, y), PercentWidth, FLabelAlignment.Right);
        _value.OnValueUpdate += delegate { ShowPercent(); };
    }

    internal Stat Stat { get; }

    internal Rule? EditedRule => _mode.value == EditorUi.Inherit ? null
        : Enum.TryParse(_mode.value, out RuleMode mode) ? new Rule(mode, Value)
        : new Rule(RuleMode.Fixed, Value, _mode.value);

    private float Value => EditorUi.PercentOf(_value) / 100f;

    private bool IsLimit => EditedRule is { Enforces: true };

    internal void SetModes(bool topLevel) => EditorUi.SetItems(_mode, Items(topLevel), _mode.value);

    internal void Load(Rule? rule)
    {
        string mode = rule?.Relation ?? rule?.Mode.ToString() ?? EditorUi.Inherit;

        EditorUi.ShowValue(_mode, _mode.GetItemList().Any(item => item.name == mode) ? mode : EditorUi.Inherit);

        if (rule is { Enforces: true } enforced)
        {
            ShowRuleValue(enforced);
        }
    }

    internal void Refresh(Rule? resolved)
    {
        bool isLimit = IsLimit;
        Rule? inherited = !isLimit && _mode.value == EditorUi.Inherit && resolved is { Enforces: true } broader ? broader : null;

        _resolved = resolved;
        _mode.greyedOut = FriendManager.IsReadOnly;
        _value.greyedOut = !isLimit || FriendManager.IsReadOnly;

        if (inherited is { } rule)
        {
            ShowRuleValue(rule);
        }

        ShowPercent();
    }

    private IEnumerable<(string Key, string Name)> Items(bool topLevel) => Stat == Stat.Relationship ? EditorUi.RelationshipItems(topLevel) : EditorUi.ModeItems(topLevel);

    private void ShowRuleValue(Rule rule) => EditorUi.ShowValue(_value, Mathf.RoundToInt(rule.Value * 100f).ToString());

    private void ShowPercent()
    {
        _percent.text = $"{EditorUi.PercentOf(_value)}%";
        _percent.color = _value.greyedOut ? MenuColorEffect.rgbMediumGrey : MenuColorEffect.rgbWhite;
    }
}
