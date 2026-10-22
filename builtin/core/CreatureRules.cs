using System.Text;
using AbsoluteFriends.Options;
using AbsoluteFriends.Utils;

namespace AbsoluteFriends.Core;

internal enum CreatureRule
{
    Inherit,
    Allow,
    AllowNeutral,
    Deny,
}

internal static class CreatureRules
{
    internal static Action? HandleChanged;

    private static readonly Dictionary<string, Configurable<string>> _options = new(StringComparer.Ordinal);

    private static readonly Dictionary<string, string> _changes = new(StringComparer.Ordinal);

    private static Addon? _addon;

    internal static IEnumerable<KeyValuePair<string, Configurable<string>>> Options => _options.OrderBy(pair => pair.Key, StringComparer.Ordinal);

    internal static IEnumerable<KeyValuePair<string, Configurable<string>>> ConfiguredRules => Options.Where(pair => IsConfiguredRule(pair.Value));

    internal static bool HasConfiguredRules => _options.Values.Any(IsConfiguredRule);

    internal static void Bind(Addon addon)
    {
        _addon = addon;
        _options.Clear();
        Refresh();
    }

    internal static void Refresh()
    {
        if (_addon == null)
        {
            return;
        }

        foreach (var name in CreatureTemplate.Type.values.entries)
        {
            if (name == CreatureTemplate.Type.Slugcat.value
                || name == MoreSlugcats.MoreSlugcatsEnums.CreatureTemplateType.SlugNPC?.value
                || _options.ContainsKey(name))
            {
                continue;
            }

            string key = "CreatureType_" + BitConverter.ToString(Encoding.UTF8.GetBytes(name)).Replace("-", "");

            Configurable<string> option = _addon.Bind(key, CreatureRule.Inherit.ToString(), new ConfigurableInfo(
                "Overrides how this creature type is treated as a friend without changing vanilla relationships.",
                tags: [name]
            ));

            option.OnChange += () => HandleChanged?.Invoke();
            _options.Add(name, option);
        }
    }

    internal static void Set(string type, string rule)
    {
        if (_options.TryGetValue(type, out Configurable<string> option))
        {
            option.Value = Parse(rule).ToString();
            _changes[type] = option.Value;
        }
    }

    internal static void Reset()
    {
        foreach (var option in _options.Values)
        {
            option.Value = CreatureRule.Inherit.ToString();
        }

        _changes.Clear();
    }

    internal static void KeepChanges(bool isReset)
    {
        if (isReset)
        {
            foreach (var pair in _changes)
            {
                _options[pair.Key].Value = pair.Value;
            }
        }

        _changes.Clear();
    }

    internal static CreatureRule Get(string type) => _options.TryGetValue(type, out Configurable<string> option) ? Parse(option.Value) : CreatureRule.Inherit;

    internal static CreatureRule Parse(string value) => value switch
    {
        nameof(CreatureRule.Allow) => CreatureRule.Allow,
        nameof(CreatureRule.AllowNeutral) => CreatureRule.AllowNeutral,
        nameof(CreatureRule.Deny) => CreatureRule.Deny,
        _ => CreatureRule.Inherit,
    };

    private static bool IsConfiguredRule(Configurable<string> option) => Parse(option.Value) != CreatureRule.Inherit;

    extension(AbstractCreature? abstractCreature)
    {
        internal bool IsDenied => abstractCreature.TypeRule == CreatureRule.Deny;

        internal bool IsFriendlyAllowed => abstractCreature.TypeRule switch
        {
            CreatureRule.Allow or CreatureRule.AllowNeutral => true,
            CreatureRule.Deny => false,
            _ => Config.FriendCreature.IsActive,
        };

        internal bool IsNeutralAllowed => abstractCreature.TypeRule switch
        {
            CreatureRule.AllowNeutral => true,
            CreatureRule.Allow or CreatureRule.Deny => false,
            _ => Config.FriendNeutralCreature.IsActive,
        };

        private CreatureRule TypeRule
        {
            get
            {
                if (abstractCreature == null || abstractCreature.IsSlugcat || abstractCreature.creatureTemplate?.type?.value is not { } name)
                {
                    return CreatureRule.Inherit;
                }

                return Get(name);
            }
        }
    }
}
