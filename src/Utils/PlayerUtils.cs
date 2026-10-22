using MoreSlugcats;

namespace AbsoluteFriends.Utils;

public static class PlayerUtils
{
    private static readonly List<Func<RainWorldGame, IEnumerable<AbstractCreature>>> _playerSources = [];

    public static void RegisterPlayerSource(Func<RainWorldGame, IEnumerable<AbstractCreature>> source)
    {
        if (source != null && !_playerSources.Contains(source))
        {
            _playerSources.Add(source);
        }
    }

    public static void UnregisterPlayerSource(Func<RainWorldGame, IEnumerable<AbstractCreature>> source) => _playerSources.Remove(source);

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
        public IEnumerable<AbstractCreature> AllPlayers
        {
            get
            {
                if (game == null)
                {
                    yield break;
                }

                HashSet<AbstractCreature>? seen = _playerSources.Count > 0 ? [] : null;

                foreach (var abstractPlayer in game.Players)
                {
                    if (abstractPlayer != null && seen?.Add(abstractPlayer) != false)
                    {
                        yield return abstractPlayer;
                    }
                }

                foreach (var source in _playerSources.ToArray())
                {
                    foreach (var abstractPlayer in source(game) ?? [])
                    {
                        if (abstractPlayer is { IsPlayer: true } && seen?.Add(abstractPlayer) != false)
                        {
                            yield return abstractPlayer;
                        }
                    }
                }
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
