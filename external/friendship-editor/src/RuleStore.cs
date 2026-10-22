using System.Runtime.CompilerServices;
using AbsoluteFriends.Manager;

namespace AbsoluteFriends.FriendshipEditor;

internal static class RuleStore
{
    private const string SectionName = "ABSOLUTEFRIENDS_EDITOR";

    private const string SectionKey = SectionName + "<dpB>";

    private static readonly ConditionalWeakTable<SaveState, Rules> _saves = new();

    private static readonly ConditionalWeakTable<RainWorldGame, Rules> _sessions = new();

    private static string? _hostContent;

    private static Rules _hostRules = new();

    private static Rules HostRules
    {
        get
        {
            string content = FriendManager.GetHostData(SectionName) ?? string.Empty;

            if (content != _hostContent)
            {
                _hostContent = content;
                _hostRules = content.Length == 0 ? new() : Rules.Deserialize(content);
            }

            return _hostRules;
        }
    }

    internal static void Register() => FriendManager.RegisterHostData(SectionName, game => For(game).Serialize());

    internal static void Unregister() => FriendManager.UnregisterHostData(SectionName);

    internal static Rules For(RainWorldGame game)
    {
        if (FriendManager.IsReadOnly)
        {
            return HostRules;
        }

        return game.session is StoryGameSession { saveState: { } save }
            ? _saves.GetValue(save, Load)
            : _sessions.GetValue(game, _ => new());
    }

    internal static void Save(RainWorldGame game)
    {
        if (FriendManager.IsReadOnly || game.session is not StoryGameSession { saveState.deathPersistentSaveData.unrecognizedSaveStrings: { } sections })
        {
            return;
        }

        Rules rules = For(game);

        sections.RemoveAll(section => section == SectionName || section.StartsWith(SectionName + "<", StringComparison.Ordinal));

        if (!rules.IsEmpty)
        {
            sections.Add(SectionKey + rules.Serialize());
        }
    }

    private static Rules Load(SaveState save)
    {
        string? section = save.deathPersistentSaveData?.unrecognizedSaveStrings?.LastOrDefault(entry => entry.StartsWith(SectionKey, StringComparison.Ordinal));

        return section == null ? new() : Rules.Deserialize(section.Substring(SectionKey.Length));
    }
}
