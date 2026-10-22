using AbsoluteFriends.Utils;

namespace AbsoluteFriends.Core;

public static class FriendDisplayNames
{
    private static readonly List<Func<AbstractCreature, string?>> _nameRules = [];

    private static readonly List<Func<AbstractCreature, string?>> _playerNameRules = [];

    public static void RegisterNameRule(Func<AbstractCreature, string?> rule)
    {
        if (rule != null && !_nameRules.Contains(rule))
        {
            _nameRules.Add(rule);
        }
    }

    public static void UnregisterNameRule(Func<AbstractCreature, string?> rule) => _nameRules.Remove(rule);

    public static void RegisterPlayerNameRule(Func<AbstractCreature, string?> rule)
    {
        if (rule != null && !_playerNameRules.Contains(rule))
        {
            _playerNameRules.Add(rule);
        }
    }

    public static void UnregisterPlayerNameRule(Func<AbstractCreature, string?> rule) => _playerNameRules.Remove(rule);

    extension(AbstractCreature abstractCreature)
    {
        public string PlayerName => _playerNameRules.Select(rule => rule(abstractCreature)).FirstOrDefault(name => !string.IsNullOrEmpty(name))
            ?? JollyCoop.JollyCustom.GetPlayerName(abstractCreature.state is PlayerState state ? state.playerNumber : 0);

        public string FriendName => abstractCreature.IsPlayer
            ? abstractCreature.PlayerName
            : _nameRules.Select(rule => rule(abstractCreature)).FirstOrDefault(name => !string.IsNullOrEmpty(name)) ?? abstractCreature.DefaultName;
    }

    extension(AbstractPhysicalObject? abstractPhysicalObject)
    {
        public string DefaultName => abstractPhysicalObject switch
        {
            AbstractOwner owner => owner.DisplayName,
            AbstractCreature { creatureTemplate.name: { } name } creature => $"{name} {creature.ID.number}",
            { type: { } type } => $"{type} {abstractPhysicalObject.ID.number}",
            _ => string.Empty,
        };
    }
}
