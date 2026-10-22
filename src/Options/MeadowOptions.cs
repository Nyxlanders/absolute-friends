using System.Text;
using AbsoluteFriends.Addons;
using AbsoluteFriends.Diagnostics;
using AbsoluteFriends.Hooks;
using AbsoluteFriends.Profiles;
using BepInEx.Bootstrap;

namespace AbsoluteFriends.Options;

public static class MeadowOptions
{
    public const string MeadowGUID = "henpemaz.rainmeadow";

    private const string EnabledKey = "";

    private const int CheckInterval = 40;

    private static Dictionary<ConfigurableBase, string>? _localValues;

    private static Dictionary<string, Dictionary<string, string>>? _hostValues;

    private static string? _hostState;

    private static int _checkTimer;

    public static bool IsMeadowInstalled => Chainloader.PluginInfos.ContainsKey(MeadowGUID);

    public static bool IsInLobby { get; private set; }

    public static string Capture()
    {
        StringBuilder state = new();

        foreach (var addon in AddonRegistry.Applied)
        {
            foreach (var option in OptionsOf(addon))
            {
                string value = ProfileManager.ValueOf(option.Value);

                if (option.Key != EnabledKey && IsSame(value, option.Value.defaultValue))
                {
                    continue;
                }

                Append(state, addon.Id);
                Append(state, option.Key);
                Append(state, value);
            }
        }

        return state.ToString();
    }

    public static void ApplyHost(string state)
    {
        if (state == _hostState)
        {
            if (_hostValues != null && ++_checkTimer >= CheckInterval)
            {
                _checkTimer = 0;

                if (Enforce(_hostValues))
                {
                    Reapply("Host options enforced");
                }
            }

            return;
        }

        if (!TryParse(state, out Dictionary<string, Dictionary<string, string>> hostValues))
        {
            Reporter.LogWarning("Host options could not be read");

            return;
        }

        _localValues ??= AddonRegistry.Applied
            .SelectMany(OptionsOf)
            .ToDictionary(option => option.Value, option => ProfileManager.ValueOf(option.Value));

        Enforce(hostValues);

        _hostValues = hostValues;
        _hostState = state;

        Reapply("Host options applied");
    }

    public static void SetInLobby(bool isInLobby)
    {
        if (IsInLobby == isInLobby)
        {
            return;
        }

        IsInLobby = isInLobby;

        Reapply(isInLobby ? "Joined a Meadow lobby" : "Left the Meadow lobby");
    }

    public static void RestoreLocal()
    {
        if (_localValues is not { } localValues)
        {
            return;
        }

        _localValues = null;
        _hostValues = null;
        _hostState = null;

        foreach (var pair in localValues)
        {
            Set(pair.Key, pair.Value);
        }

        Reapply("Local options restored");
    }

    private static void Reapply(string note)
    {
        HookManager.Refresh();
        Reporter.LogInfo(note);
        Reporter.SaveReport();
    }

    private static IEnumerable<KeyValuePair<string, ConfigurableBase>> OptionsOf(Addon addon)
    {
        if (addon.Enabled is { } enabled)
        {
            yield return new(EnabledKey, enabled);
        }

        foreach (var option in addon.ProfileOptions)
        {
            if (!option.Value.IsUnsyncedInMeadow)
            {
                yield return option;
            }
        }
    }

    private static bool Enforce(Dictionary<string, Dictionary<string, string>> hostValues)
    {
        bool changed = false;

        foreach (var addon in AddonRegistry.Applied)
        {
            if (!hostValues.TryGetValue(addon.Id, out Dictionary<string, string> values))
            {
                changed |= Set(addon.Enabled, "false");

                continue;
            }

            foreach (var option in OptionsOf(addon))
            {
                changed |= Set(option.Value, values.TryGetValue(option.Key, out string value) ? value : option.Value.defaultValue);
            }
        }

        return changed;
    }

    private static bool Set(ConfigurableBase? configurable, string value)
    {
        if (configurable == null || IsSame(ProfileManager.ValueOf(configurable), value))
        {
            return false;
        }

        try
        {
            configurable.BoxedValue = value;

            return true;
        }
        catch (Exception exception)
        {
            Reporter.LogError($"Could not apply Meadow option {configurable.key}", exception);

            return false;
        }
    }

    private static bool IsSame(string value, string other) => string.Equals(value, other, StringComparison.OrdinalIgnoreCase);

    private static void Append(StringBuilder state, string value)
    {
        state.Append(value.Length).Append(':').Append(value);
    }

    private static bool TryParse(string state, out Dictionary<string, Dictionary<string, string>> values)
    {
        values = [];

        int index = 0;

        while (index < state.Length)
        {
            if (!TryRead(state, ref index, out string id) || !TryRead(state, ref index, out string key) || !TryRead(state, ref index, out string value))
            {
                return false;
            }

            if (!values.TryGetValue(id, out Dictionary<string, string> options))
            {
                values[id] = options = [];
            }

            options[key] = value;
        }

        return true;
    }

    private static bool TryRead(string state, ref int index, out string value)
    {
        value = "";

        int separator = state.IndexOf(':', index);

        if (separator < 0 || !int.TryParse(state.Substring(index, separator - index), out int length) || length < 0 || separator + 1 + length > state.Length)
        {
            return false;
        }

        value = state.Substring(separator + 1, length);
        index = separator + 1 + length;

        return true;
    }
}
