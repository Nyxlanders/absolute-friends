using MoreSlugcats;
using UnityEngine;

namespace AbsoluteFriends.Utils;

public static class PlayerUtils
{
    private static readonly List<Func<RainWorldGame, IEnumerable<AbstractCreature>>> _playerSources = [];

    private static List<AbstractCreature> _players = [];

    private static RainWorldGame? _playersGame;

    private static int _playersClock = -1;

    private static int _playersFrame = -1;

    private static int _playersCount = -1;

    public static void RegisterPlayerSource(Func<RainWorldGame, IEnumerable<AbstractCreature>> source)
    {
        if (source != null && !_playerSources.Contains(source))
        {
            _playerSources.Add(source);
            _playersGame = null;
        }
    }

    public static void UnregisterPlayerSource(Func<RainWorldGame, IEnumerable<AbstractCreature>> source)
    {
        _playerSources.Remove(source);
        _playersGame = null;
    }

    private static List<AbstractCreature> CollectPlayers(RainWorldGame game)
    {
        List<AbstractCreature> players = [];
        HashSet<AbstractCreature> seen = [];

        foreach (var abstractPlayer in game.Players)
        {
            if (abstractPlayer != null && seen.Add(abstractPlayer))
            {
                players.Add(abstractPlayer);
            }
        }

        foreach (var source in _playerSources.ToArray())
        {
            foreach (var abstractPlayer in source(game) ?? [])
            {
                if (abstractPlayer is { IsPlayer: true } && seen.Add(abstractPlayer))
                {
                    players.Add(abstractPlayer);
                }
            }
        }

        return players;
    }

    extension(AbstractPhysicalObject? abstractPhysicalObject)
    {
        public bool IsPlayer => abstractPhysicalObject.Template == CreatureTemplate.Type.Slugcat;

        public bool IsSlugpup => abstractPhysicalObject.Template is { } template && template == MoreSlugcatsEnums.CreatureTemplateType.SlugNPC;

        public bool IsSlugcat => abstractPhysicalObject.IsPlayer || abstractPhysicalObject.IsSlugpup;

        private CreatureTemplate.Type? Template => (abstractPhysicalObject as AbstractCreature)?.creatureTemplate?.type;
    }

    extension(PhysicalObject? physicalObject)
    {
        public bool IsPlayer => physicalObject?.abstractPhysicalObject.IsPlayer == true;

        public bool IsSlugpup => physicalObject?.abstractPhysicalObject.IsSlugpup == true;

        public bool IsSlugcat => physicalObject?.abstractPhysicalObject.IsSlugcat == true;
    }

    extension(RainWorldGame? game)
    {
        public IReadOnlyList<AbstractCreature> AllPlayers
        {
            get
            {
                if (game == null)
                {
                    return [];
                }

                int frame = game.GamePaused ? Time.frameCount : -1;

                if (_playersGame != game || _playersClock != game.clock || _playersFrame != frame || _playersCount != game.Players.Count)
                {
                    _players = CollectPlayers(game);
                    _playersGame = game;
                    _playersClock = game.clock;
                    _playersFrame = frame;
                    _playersCount = game.Players.Count;
                }

                return _players;
            }
        }

        public IEnumerable<Player> RealizedPlayers
        {
            get
            {
                foreach (var abstractPlayer in game.AllPlayers)
                {
                    if (abstractPlayer?.realizedCreature is Player player)
                    {
                        yield return player;
                    }
                }
            }
        }
    }
}
