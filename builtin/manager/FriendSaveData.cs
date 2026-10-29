using System.Runtime.CompilerServices;
using System.Text;
using AbsoluteFriends.Core;
using AbsoluteFriends.Utils;

namespace AbsoluteFriends.Manager;

internal static class FriendSaveData
{
    private const string SectionSeparator = "<dpB>";

    private const string EntrySeparator = "<dpC>";

    private const string LegacySectionSeparator = "<svB>";

    private const string LegacyEntrySeparator = "<svC>";

    private const char HostSectionSeparator = '\u0001';

    private const char HostEntrySeparator = '\u0002';

    private const string HostDataPrefix = "DATA:";

    private const string HostIdsKey = "ABSOLUTEFRIENDS_IDS";

    private const string UnmappedKey = "C?";

    private static readonly List<Action> _resets = [];

    private static readonly Dictionary<string, Func<RainWorldGame, string?>> _hostData = new(StringComparer.Ordinal);

    private static readonly List<Func<AbstractCreature, string?>> _sharedIdRules = [];

    private static readonly List<Func<AbstractCreature, EntityID?>> _originIdRules = [];

    private static Dictionary<string, string[]>? _hostSections;

    private static Dictionary<string, EntityID>? _hostIds;

    private static string? _hostState;

    internal static bool IsReadOnly => _hostSections != null;

    internal static void Reset() => _resets.ForEach(reset => reset());

    internal static string Encode(string value) => Convert.ToBase64String(Encoding.UTF8.GetBytes(value));

    internal static string Decode(string value) => Encoding.UTF8.GetString(Convert.FromBase64String(value));

    internal static bool Assign<TKey>(Dictionary<TKey, string> values, TKey key, string? value)
    {
        if (value == null)
        {
            return values.Remove(key);
        }

        if (values.TryGetValue(key, out string previous) && previous == value)
        {
            return false;
        }

        values[key] = value;

        return true;
    }

    internal static void RegisterHostData(string key, Func<RainWorldGame, string?> export)
    {
        if (!string.IsNullOrEmpty(key) && export != null)
        {
            _hostData[key] = export;
        }
    }

    internal static void UnregisterHostData(string key) => _hostData.Remove(key);

    internal static void RegisterSharedIdRule(Func<AbstractCreature, string?> rule)
    {
        if (rule != null && !_sharedIdRules.Contains(rule))
        {
            _sharedIdRules.Add(rule);
        }
    }

    internal static void UnregisterSharedIdRule(Func<AbstractCreature, string?> rule) => _sharedIdRules.Remove(rule);

    internal static void RegisterOriginIdRule(Func<AbstractCreature, EntityID?> rule)
    {
        if (rule != null && !_originIdRules.Contains(rule))
        {
            _originIdRules.Add(rule);
        }
    }

    internal static void UnregisterOriginIdRule(Func<AbstractCreature, EntityID?> rule) => _originIdRules.Remove(rule);

    internal static string? GetHostData(string key) => _hostSections != null
        && _hostSections.TryGetValue(HostDataPrefix + key, out string[] entries)
        && entries.Length > 0
            ? Decode(entries[0])
            : null;

    internal static string Export(RainWorldGame game) => string.Join(HostSectionSeparator.ToString(), ((IEnumerable<(string Key, IEnumerable<string> Entries)>)[FriendNicknames.Export(game), FriendStorage.Export(game), (HostIdsKey, HostIds())])
        .Concat(_hostData.Select(data => (Key: HostDataPrefix + data.Key, Entries: data.Value(game) is { } value ? (IEnumerable<string>)[Encode(value)] : [])))
        .Select(section => string.Join(HostEntrySeparator.ToString(), [section.Key, .. section.Entries])));

    internal static void ApplyHost(string? state)
    {
        if (state == _hostState)
        {
            return;
        }

        _hostState = state;
        _hostSections = state == null ? null : state
            .Split([HostSectionSeparator], StringSplitOptions.RemoveEmptyEntries)
            .Select(section => section.Split(HostEntrySeparator))
            .GroupBy(parts => parts[0], StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Last().Skip(1).ToArray(), StringComparer.Ordinal);
        _hostIds = _hostSections == null ? null : ReadHostIds(_hostSections.TryGetValue(HostIdsKey, out string[] ids) ? ids : []);

        Reset();
    }

    internal static IEnumerable<string> ReadEntries(SaveState? save, string sectionKey)
    {
        if (_hostSections != null)
        {
            return _hostSections.TryGetValue(sectionKey, out string[] entries) ? entries : [];
        }

        return ReadSaveEntries(save, sectionKey);
    }

    internal static void WriteEntries(SaveState? save, string sectionKey, IEnumerable<string> entries)
    {
        if (IsReadOnly || save?.deathPersistentSaveData?.unrecognizedSaveStrings is not { } sections)
        {
            return;
        }

        string content = string.Join(EntrySeparator, entries);

        sections.RemoveAll(section => section == sectionKey
            || section.StartsWith(sectionKey + SectionSeparator, StringComparison.Ordinal)
            || section.StartsWith(sectionKey + LegacySectionSeparator, StringComparison.Ordinal));
        save.unrecognizedSaveStrings?.RemoveAll(section => section.StartsWith(sectionKey + LegacySectionSeparator, StringComparison.Ordinal));

        if (content.Length > 0)
        {
            sections.Add(sectionKey + SectionSeparator + content);
        }
    }

    private static string? SharedIdOf(AbstractCreature abstractCreature) => _sharedIdRules.Select(rule => rule(abstractCreature)).FirstOrDefault(id => !string.IsNullOrEmpty(id));

    private static IEnumerable<string> HostIds() => _sharedIdRules.Count == 0 ? [] : FriendUtils.TrackingCandidates
        .Where(friend => !friend.IsPlayer)
        .Select(friend => SharedIdOf(friend) is { } shared ? $"{shared}={friend.OriginID}" : null)
        .OfType<string>();

    private static Dictionary<string, EntityID> ReadHostIds(IEnumerable<string> entries)
    {
        Dictionary<string, EntityID> ids = new(StringComparer.Ordinal);

        foreach (var entry in entries)
        {
            int separator = entry.LastIndexOf('=');

            if (separator <= 0)
            {
                continue;
            }

            try
            {
                ids[entry.Substring(0, separator)] = EntityID.FromString(entry.Substring(separator + 1));
            }
            catch (Exception exception) when (exception is FormatException or IndexOutOfRangeException or OverflowException)
            {
                continue;
            }
        }

        return ids;
    }

    private static IEnumerable<string> ReadSaveEntries(SaveState? save, string sectionKey)
    {
        IEnumerable<(string Section, string Prefix, string Separator)> sections = [
            .. (save?.unrecognizedSaveStrings ?? []).Select(section => (section, sectionKey + LegacySectionSeparator, LegacyEntrySeparator)),
            .. (save?.deathPersistentSaveData?.unrecognizedSaveStrings ?? []).Select(section => (section, sectionKey + SectionSeparator, EntrySeparator))
        ];

        foreach (var (section, prefix, separator) in sections)
        {
            if (!section.StartsWith(prefix, StringComparison.Ordinal))
            {
                continue;
            }

            foreach (var entry in section.Substring(prefix.Length).Split([separator], StringSplitOptions.RemoveEmptyEntries))
            {
                yield return entry;
            }
        }
    }

    internal class Store<T> where T : class
    {
        private readonly Func<SaveState?, T> _create;

        private ConditionalWeakTable<RainWorldGame, T> _states = new();

        internal Store(Func<SaveState?, T> create)
        {
            _create = create;
            _resets.Add(() => _states = new());
        }

        internal T For(RainWorldGame game) => _states.GetValue(game, game => _create(game.StorySaveState));

        internal T? For(AbstractCreature abstractCreature) => abstractCreature.Game is { } game ? For(game) : null;
    }

    extension(AbstractCreature abstractCreature)
    {
        internal string SaveDataKey => abstractCreature is { IsPlayer: true, state: PlayerState playerState }
            ? $"P{playerState.playerNumber}"
            : abstractCreature.HostID is { } id ? $"C{id}" : UnmappedKey;

        internal EntityID OriginID => _originIdRules.Select(rule => rule(abstractCreature)).FirstOrDefault(id => id != null) ?? abstractCreature.ID;

        internal EntityID? HostID => _hostIds == null
            ? abstractCreature.OriginID
            : SharedIdOf(abstractCreature) is { } shared && _hostIds.TryGetValue(shared, out EntityID id) ? id : null;

        internal RainWorldGame? Game => abstractCreature.world?.game ?? RainWorldUtils.CurrentGame;
    }

    extension(RainWorldGame game)
    {
        internal SaveState? StorySaveState => game.session is StoryGameSession story ? story.saveState : null;
    }
}
