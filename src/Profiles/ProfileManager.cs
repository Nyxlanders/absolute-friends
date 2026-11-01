using AbsoluteFriends.Addons;
using AbsoluteFriends.Diagnostics;
using AbsoluteFriends.Options;
using Menu.Remix;

namespace AbsoluteFriends.Profiles;

internal static class ProfileManager
{
    internal static (Profile Profile, ProfileResult Result) Create(string name)
    {
        string now = Timestamp();
        Profile profile = new()
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = NormalizeName(name),
            CreatedAt = now,
            ModifiedAt = now,
        };

        ProfileResult result = Capture(profile, ValueOf, out _);
        ProfileStore.Save(profile);

        return (profile, result);
    }

    internal static bool Keep(Profile profile)
    {
        Capture(profile, SavedValueOf, out bool changed);

        if (!changed)
        {
            return false;
        }

        profile.ModifiedAt = Timestamp();
        ProfileStore.Save(profile);

        return true;
    }

    internal static void Rename(Profile profile, string name)
    {
        profile.Name = NormalizeName(name);
        profile.ModifiedAt = Timestamp();

        ProfileStore.Save(profile);
    }

    internal static ProfileResult Apply(Profile profile)
    {
        int addons = 0;
        int options = 0;
        int unavailableAddons = 0;
        int unavailableOptions = 0;

        foreach (var pair in profile.Addons)
        {
            Addon? addon = AddonRegistry.Find(pair.Key);

            if (addon == null)
            {
                unavailableAddons++;
                unavailableOptions += CountUnavailableOptions(null, pair.Value);

                continue;
            }

            addons++;
            Set(addon.Enabled, pair.Value.Enabled, ref unavailableOptions);
        }

        foreach (var pair in profile.Addons)
        {
            Addon? addon = AddonRegistry.Find(pair.Key);

            if (addon == null)
            {
                continue;
            }

            options += ApplyOptions(addon, pair.Value, ref unavailableOptions);
        }

        return new(addons, options, unavailableAddons, unavailableOptions);
    }

    internal static void Remove(Profile profile) => ProfileStore.Remove(profile);

    internal static ProfileSummary Summarize(Profile profile)
    {
        int enabledAddons = 0;
        int enabledOptions = 0;
        int options = 0;
        int settings = 0;
        int unavailableAddons = 0;
        int unavailableOptions = 0;

        foreach (var pair in profile.Addons)
        {
            Addon? addon = AddonRegistry.Find(pair.Key);

            if (addon == null)
            {
                unavailableAddons++;
            }
            else if (addon.Enabled is { } enabled && IsEnabled(profile, enabled))
            {
                enabledAddons++;
            }

            foreach (var option in pair.Value.Options)
            {
                if (addon?.ProfileOptions.TryGetValue(option.Key, out ConfigurableBase configurable) == true)
                {
                    if (configurable is Configurable<bool>)
                    {
                        options++;

                        if (IsEnabled(profile, configurable))
                        {
                            enabledOptions++;
                        }
                    }
                    else
                    {
                        settings++;
                    }
                }
                else
                {
                    if (IsBoolean(option))
                    {
                        options++;
                    }
                    else
                    {
                        settings++;
                    }

                    unavailableOptions++;
                }
            }
        }

        return new(enabledAddons, profile.Addons.Count, enabledOptions, options, settings, unavailableAddons, unavailableOptions);
    }

    internal static string ValueOf(ConfigurableBase? configurable)
    {
        return configurable?.BoundUIconfig?.value ?? SavedValueOf(configurable);
    }

    private static ProfileResult Capture(Profile profile, Func<ConfigurableBase?, string> valueOf, out bool changed)
    {
        int addons = 0;
        int options = 0;

        changed = false;

        foreach (var addon in AddonRegistry.Applied)
        {
            if (!profile.Addons.TryGetValue(addon.Id, out ProfileAddon saved))
            {
                profile.Addons[addon.Id] = saved = new();
                changed = true;
            }

            string enabled = valueOf(addon.Enabled);

            changed |= saved.Name != addon.Name || saved.Version != addon.Version || saved.Enabled != enabled;
            saved.Name = addon.Name;
            saved.Version = addon.Version;
            saved.Enabled = enabled;

            foreach (var option in addon.ProfileOptions)
            {
                string value = valueOf(option.Value);

                changed |= !saved.Options.TryGetValue(option.Key, out string previous) || previous != value;
                saved.Options[option.Key] = value;

                if (option.Value is Configurable<bool>)
                {
                    options++;
                }
            }

            addons++;
        }

        int unavailableAddons = profile.Addons.Keys.Count(id => AddonRegistry.Find(id) == null);
        int unavailableOptions = profile.Addons.Sum(pair => CountUnavailableOptions(AddonRegistry.Find(pair.Key), pair.Value));

        return new(addons, options, unavailableAddons, unavailableOptions);
    }

    private static string NormalizeName(string name)
    {
        string normalized = name.Trim();

        return normalized.Length switch
        {
            0 => "Profile",
            > 80 => normalized.Substring(0, 80),
            _ => normalized,
        };
    }

    private static string Timestamp() => $"{DateTime.UtcNow:O}";

    private static bool IsTrue(string value) => bool.TryParse(value, out bool enabled) && enabled;

    private static bool IsBoolean(KeyValuePair<string, string> option) => bool.TryParse(option.Value, out _);

    private static int CountUnavailableOptions(Addon? addon, ProfileAddon saved) => saved.Options.Count(option =>
        (addon == null || !addon.ProfileOptions.ContainsKey(option.Key)) && IsBoolean(option));

    private static int ApplyOptions(Addon addon, ProfileAddon saved, ref int unavailable)
    {
        int applied = 0;

        foreach (var option in saved.Options)
        {
            if (!addon.ProfileOptions.TryGetValue(option.Key, out ConfigurableBase configurable))
            {
                if (IsBoolean(option))
                {
                    unavailable++;
                }

                continue;
            }

            int before = unavailable;

            Set(configurable, option.Value, ref unavailable);

            if (configurable is Configurable<bool> && before == unavailable)
            {
                applied++;
            }
        }

        foreach (var option in addon.ProfileOptions)
        {
            if (!saved.Options.ContainsKey(option.Key))
            {
                Set(option.Value, option.Value.defaultValue, ref unavailable);
            }
        }

        return applied;
    }

    private static bool IsEnabled(Profile profile, ConfigurableBase configurable)
    {
        return IsTrue(ProfileValue(profile, configurable)) && configurable.Requirements.All(group => group.Any(master => IsEnabled(profile, master)));
    }

    private static string ProfileValue(Profile profile, ConfigurableBase configurable)
    {
        foreach (var addon in AddonRegistry.Applied)
        {
            if (!profile.Addons.TryGetValue(addon.Id, out ProfileAddon saved))
            {
                continue;
            }

            if (addon.Enabled == configurable)
            {
                return saved.Enabled;
            }

            foreach (var option in addon.ProfileOptions)
            {
                if (option.Value == configurable && saved.Options.TryGetValue(option.Key, out string value))
                {
                    return value;
                }
            }
        }

        return "false";
    }

    private static string SavedValueOf(ConfigurableBase? configurable) => configurable?.BoxedValue switch
    {
        null => "",
        bool value => value ? "true" : "false",
        { } value => ValueConverter.ConvertToString(value, configurable.settingType),
    };

    private static void Set(ConfigurableBase? configurable, string value, ref int unavailable)
    {
        try
        {
            if (configurable == null)
            {
                unavailable++;

                return;
            }

            if (configurable.BoundUIconfig is { } control)
            {
                control.value = value;
            }
            else
            {
                configurable.BoxedValue = value;
            }

            if (!string.Equals(ValueOf(configurable), value, StringComparison.OrdinalIgnoreCase))
            {
                unavailable++;
            }
        }
        catch (Exception exception)
        {
            unavailable++;
            Reporter.LogError($"Could not apply profile option {configurable?.key}", exception);
        }
    }
}
