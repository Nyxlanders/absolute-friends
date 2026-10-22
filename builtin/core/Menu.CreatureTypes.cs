using AbsoluteFriends.Options;
using AbsoluteFriends.Utils;
using Menu.Remix.MixedUI;
using UnityEngine;

namespace AbsoluteFriends.Core;

internal static partial class Menu
{
    private const ushort VisibleRuleItems = 3;

    private static MenuBuilder? _rulesMenu;

    private static OnEventHandler? _resetHandler;

    private static OnEventHandler? _saveHandler;

    private static void BuildCreatureTypes(MenuBuilder menu)
    {
        CreatureRules.Refresh();

        ListItem[] rules =
        [
            RuleItem(CreatureRule.Allow),
            RuleItem(CreatureRule.AllowNeutral),
            RuleItem(CreatureRule.Deny),
        ];
        ListItem[] types = [.. CreatureRules.Options.Select(type => new ListItem(type.Key))];

        if (types.Length > 0 && !types.Any(item => item.name == Config.SelectedCreatureType.Value))
        {
            Config.SelectedCreatureType.Value = types[0].name;
        }

        if (!rules.Any(item => item.name == Config.SelectedCreatureRule.Value))
        {
            Config.SelectedCreatureRule.Value = nameof(CreatureRule.Allow);
        }

        OpComboBox? typePicker = null;
        OpComboBox? rulePicker = null;
        OpListBox? configuredRules = null;

        void ShowType(string type)
        {
            CreatureRule rule = CreatureRules.Get(type);
            string value = (rule == CreatureRule.Inherit ? CreatureRule.Allow : rule).ToString();

            if (rulePicker != null && rulePicker.value != value)
            {
                rulePicker.value = value;
            }

            if (configuredRules is not { } list)
            {
                return;
            }

            if (rule == CreatureRule.Inherit || list.GetItemList().All(item => item.name != type))
            {
                list.ForceValue(string.Empty);
            }
            else if (list.value != type)
            {
                list.value = type;
            }
        }

        void ShowConfiguredRule(string type)
        {
            if (typePicker == null)
            {
                return;
            }

            if (CreatureRules.Get(type) == CreatureRule.Inherit)
            {
                ShowType(typePicker.value);

                return;
            }

            if (typePicker.value != type)
            {
                typePicker.value = type;
            }

            ShowType(type);
        }

        void RefreshConfiguredRules(string? selection = null)
        {
            if (configuredRules == null)
            {
                return;
            }

            selection ??= configuredRules.value;

            string[] previous = [.. configuredRules.GetItemList().Select(item => item.name)];
            ListItem[] current = ConfiguredRuleItems();

            configuredRules._listTop = 0;
            configuredRules.AddItems(sort: false, [new ListItem("__refresh", " ")]);
            configuredRules.RemoveItems(selectNext: false, previous);
            configuredRules.AddItems(sort: false, current);
            configuredRules.RemoveItems(selectNext: false, ["__refresh"]);

            if (current.Any(item => item.name == selection && CreatureRules.Get(item.name) != CreatureRule.Inherit))
            {
                configuredRules.value = selection;
            }
            else
            {
                ShowType(typePicker?.value ?? "");
            }
        }

        CreatureRules.HandleChanged = () => RefreshConfiguredRules();

        if (_rulesMenu != null)
        {
            _rulesMenu.OnConfigReset -= _resetHandler;
            _rulesMenu.OnConfigChanged -= _saveHandler;
        }

        _rulesMenu = menu;
        _resetHandler = () =>
        {
            CreatureRules.Reset();
            RefreshConfiguredRules();

            rulePicker?.value = nameof(CreatureRule.Allow);
        };
        _saveHandler = () => CreatureRules.KeepChanges(menu.config.pendingReset);
        menu.OnConfigReset += _resetHandler;
        menu.OnConfigChanged += _saveHandler;

        ListItem[] initialRules = ConfiguredRuleItems();

        if (CreatureRules.Get(Config.SelectedConfiguredCreatureRules.Value) == CreatureRule.Inherit
            || !initialRules.Any(item => item.name == Config.SelectedConfiguredCreatureRules.Value))
        {
            Config.SelectedConfiguredCreatureRules.Value = initialRules[0].name;
        }

        menu.AddParallelColumns(
            () =>
            {
                menu.AddLabel("Creature Type", alignment: FLabelAlignment.Left);
                typePicker = menu.AddComboBox(Config.SelectedCreatureType, types);
                menu.AddLabel("Creature Rule", alignment: FLabelAlignment.Left);
                rulePicker = menu.AddComboBox(
                    Config.SelectedCreatureRule,
                    rules,
                    static (item, label, selectedRow) => ApplyRuleColor(label, CreatureRules.Parse(item.name), selectedRow)
                );
            },
            () =>
            {
                configuredRules = menu.AddListBox(
                    Config.SelectedConfiguredCreatureRules,
                    initialRules,
                    static (item, label, selectedRow) => ApplyRuleColor(label, CreatureRules.Get(item.name), selectedRow),
                    "Configured Creature Rules",
                    visibleItems: VisibleRuleItems
                );
                configuredRules?.DASHES = string.Empty;
            }
        );

        typePicker?.OnValueUpdate += (_, value, _) => ShowType(value);
        configuredRules?.OnValueUpdate += (_, value, _) => ShowConfiguredRule(value);

        if (configuredRules != null && CreatureRules.Get(configuredRules.value) != CreatureRule.Inherit)
        {
            ShowConfiguredRule(configuredRules.value);
        }
        else
        {
            ShowType(typePicker?.value ?? "");
        }

        menu.AddRow(0.5f);
        menu.SetColumns(2);
        menu.AddSimpleButton("Apply Creature Rule", "Add or update the selected creature rule.", () =>
        {
            string type = typePicker?.value ?? "";

            CreatureRules.Set(type, rulePicker?.value ?? "");
            RefreshConfiguredRules(type);
        });
        menu.AddSimpleButton("Remove Creature Rule", "Remove the selected creature rule and use the global friendship options again.", () =>
        {
            if (configuredRules is { } list && CreatureRules.Get(list.value) != CreatureRule.Inherit)
            {
                CreatureRules.Set(list.value, nameof(CreatureRule.Inherit));
            }
        });
    }

    private static ListItem RuleItem(CreatureRule rule)
    {
        return new(rule.ToString(), Translation.Of(RuleLabel(rule)));
    }

    private static string RuleLabel(CreatureRule rule) => rule switch
    {
        CreatureRule.Allow => "Allow friendly",
        CreatureRule.AllowNeutral => "Allow friendly & neutral",
        CreatureRule.Deny => "Deny friendship",
        _ => ""
    };

    private static ListItem[] ConfiguredRuleItems()
    {
        List<ListItem> items = [];

        foreach (var pair in CreatureRules.ConfiguredRules)
        {
            items.Add(new(pair.Key, items.Count));
        }

        if (items.Count == 0)
        {
            items.Add(new(Config.NoConfiguredRule, Translation.Of("No creature rules"), 0));
        }

        for (int index = items.Count; index < VisibleRuleItems; index++)
        {
            items.Add(new($"__none{index}", " ", int.MaxValue));
        }

        return [.. items];
    }

    private static void ApplyRuleColor(FLabel label, CreatureRule rule, bool selectedRow)
    {
        if (rule == CreatureRule.Inherit)
        {
            return;
        }

        Color color = rule switch
        {
            CreatureRule.Allow => Palette.Friendly,
            CreatureRule.AllowNeutral => Palette.Neutral,
            _ => Palette.Unfriendly
        };

        label.color = color * (selectedRow ? 0.7f : 1f);
    }
}
