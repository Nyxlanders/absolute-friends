namespace AbsoluteFriends.Manager;

public static class FriendStorage
{
    private const string SaveKey = "ABSOLUTEFRIENDS_STORAGE";

    private static readonly FriendSaveData.Store<State> _states = new(save => new(save));

    internal static (string Key, IEnumerable<string> Entries) Export(RainWorldGame game) => (SaveKey, _states.For(game).Entries);

    private static bool TryReadEntry(string entry, out (string Creature, string Key) key, out string value)
    {
        key = default;
        value = string.Empty;

        int creatureEnd = entry.IndexOf('=');

        if (creatureEnd <= 0)
        {
            return false;
        }

        int keyStart = creatureEnd + 1;
        int keyEnd = entry.IndexOf('=', keyStart);

        while (keyEnd >= 0 && (keyEnd - keyStart) % 4 != 0)
        {
            keyEnd = entry.IndexOf('=', keyEnd + 1);
        }

        if (keyEnd <= keyStart)
        {
            return false;
        }

        try
        {
            key = (entry.Substring(0, creatureEnd), FriendSaveData.Decode(entry.Substring(keyStart, keyEnd - keyStart)));
            value = FriendSaveData.Decode(entry.Substring(keyEnd + 1));

            return key.Key.Length > 0;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private class State
    {
        internal readonly Dictionary<(string Creature, string Key), string> Values = [];

        private readonly SaveState? _save;

        internal State(SaveState? save)
        {
            _save = save;

            foreach (var entry in FriendSaveData.ReadEntries(save, SaveKey))
            {
                if (TryReadEntry(entry, out var key, out string value))
                {
                    Values[key] = value;
                }
            }
        }

        internal IEnumerable<string> Entries => Values
            .OrderBy(pair => pair.Key.Creature, StringComparer.Ordinal)
            .ThenBy(pair => pair.Key.Key, StringComparer.Ordinal)
            .Select(pair => $"{pair.Key.Creature}={FriendSaveData.Encode(pair.Key.Key)}={FriendSaveData.Encode(pair.Value)}");

        internal void Write() => FriendSaveData.WriteEntries(_save, SaveKey, Entries);
    }

    extension(AbstractCreature abstractCreature)
    {
        public string? GetFriendData(string key) => !string.IsNullOrEmpty(key) && _states.For(abstractCreature) is { } state
            && state.Values.TryGetValue((abstractCreature.SaveDataKey, key), out string value)
            ? value
            : null;

        public void SetFriendData(string key, string? value)
        {
            if (string.IsNullOrEmpty(key) || FriendSaveData.IsReadOnly || _states.For(abstractCreature) is not { } state)
            {
                return;
            }

            if (FriendSaveData.Assign(state.Values, (abstractCreature.SaveDataKey, key), value))
            {
                state.Write();
            }
        }

        internal void ClearFriendData()
        {
            if (FriendSaveData.IsReadOnly || _states.For(abstractCreature) is not { } state)
            {
                return;
            }

            string creature = abstractCreature.SaveDataKey;

            if (state.Values.Keys.Where(entry => entry.Creature == creature).ToList() is { Count: > 0 } entries)
            {
                entries.ForEach(entry => state.Values.Remove(entry));
                state.Write();
            }
        }
    }
}
