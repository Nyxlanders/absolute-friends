using System.Runtime.CompilerServices;
using AbsoluteFriends.Diagnostics;
using AbsoluteFriends.Options;
using AbsoluteFriends.Utils;
using JollyCoop;

namespace AbsoluteFriends.Pilgrimage;

internal static class ShelterDoorUtils
{
    private const float CloseSpeed = 0.003125f;

    private static readonly AddonLogger _logger = Reporter.GetLogger(Plugin.Name);

    private static readonly ConditionalWeakTable<RainWorldGame, ShelterDoor[]> _closingApart = new();

    internal static bool CanSleepApart(RainWorldGame? game) => Config.AnyShelter.IsActive && !MeadowOptions.IsInLobby && ModManager.CoopAvailable && game is { IsStorySession: true };

    internal static IEnumerable<AbstractCreature> Sleepers(RainWorldGame game) => Config.Perseverance.IsActive ? game.AlivePlayers : game.PlayersToProgressOrWin;

    internal static IEnumerable<AbstractCreature> SleepersIn(ShelterDoor shelterDoor, IEnumerable<AbstractCreature>? abstractPlayers) => CanSleepApart(shelterDoor.room?.game)
        ? (abstractPlayers ?? []).Where(abstractPlayer => abstractPlayer.Room == shelterDoor.room?.abstractRoom)
        : abstractPlayers ?? [];

    private static ShelterDoor[]? ClosingApart(RainWorldGame game) => _closingApart.TryGetValue(game, out ShelterDoor[] shelters) ? shelters : null;

    private static ShelterDoor? ShelterOf(AbstractCreature abstractPlayer) =>
        abstractPlayer.realizedCreature is Player { room.shelterDoor: { } shelterDoor } player && shelterDoor.IsInShelterDoor(player) ? shelterDoor : null;

    private static ShelterDoor[]? SeparateShelters(RainWorldGame game)
    {
        ShelterDoor?[] shelters = [.. Sleepers(game).Select(ShelterOf)];

        if (shelters.Length == 0 || shelters.Any(shelter => shelter == null))
        {
            return null;
        }

        ShelterDoor[] distinct = [.. shelters.OfType<ShelterDoor>().Distinct()];

        return distinct.Length > 1 ? distinct : null;
    }

    private static Player? FoodTarget(RainWorldGame game)
    {
        if (game.Players.FirstOrDefault() is { state: PlayerState { permaDead: false }, realizedCreature: Player { room: not null } first })
        {
            return first;
        }

        return game.FirstAlivePlayer?.realizedCreature as Player;
    }

    private static int FoodApart(RainWorldGame game, ShelterDoor[] shelters, bool eat)
    {
        if (FoodTarget(game) is not { room: { } targetRoom } target)
        {
            return 0;
        }

        int food = eat ? 0 : target.FoodInRoom(targetRoom, false);

        foreach (var shelter in shelters)
        {
            if (shelter.room == targetRoom || Sleepers(game).Select(abstractPlayer => abstractPlayer.realizedCreature).OfType<Player>().FirstOrDefault(player => player.room == shelter.room) is not { } player)
            {
                continue;
            }

            int extra = Math.Max(0, player.FoodInRoom(shelter.room, eat) - player.FoodInStomach);

            if (eat)
            {
                target.AddFood(extra);
            }
            else
            {
                food += extra;
            }
        }

        return food;
    }

    extension(ShelterDoor shelterDoor)
    {
        internal bool TryCloseApart()
        {
            if (shelterDoor.room?.game is not { } game || !CanSleepApart(game) || shelterDoor.Broken)
            {
                return false;
            }

            if (ClosingApart(game) is { } closing)
            {
                return closing.Contains(shelterDoor);
            }

            if (Sleepers(game).Any(abstractPlayer => abstractPlayer.state is { dead: false } && abstractPlayer.realizedCreature == null))
            {
                return true;
            }

            if (SeparateShelters(game) is not { } shelters || !shelters.Contains(shelterDoor))
            {
                return false;
            }

            Player[] players = [.. Sleepers(game).Select(abstractPlayer => abstractPlayer.realizedCreature).OfType<Player>().Where(player => !player.dead)];

            if (!players.All(player => player.ReadyForWinJolly) && !players.Any(player => player.ReadyForStarveJolly))
            {
                return true;
            }

            foreach (var shelter in shelters)
            {
                if (!shelter.IsClosing)
                {
                    shelter.closeSpeed = CloseSpeed;
                }
            }

            _closingApart.Remove(game);
            _closingApart.Add(game, shelters);

            Dictionary<string, List<string>> regionsVisited = game.rainWorld.progression.miscProgressionData.regionsVisited;

            if (!regionsVisited.TryGetValue(shelterDoor.room.world.name, out List<string> visitors))
            {
                regionsVisited[shelterDoor.room.world.name] = visitors = [];
            }

            if (!visitors.Contains(game.StoryCharacter.value))
            {
                visitors.Add(game.StoryCharacter.value);
            }

            return true;
        }

        internal bool TryEndApart()
        {
            if (shelterDoor.room?.game is not { } game || !CanSleepApart(game) || ClosingApart(game) is not { } shelters || !shelters.Contains(shelterDoor))
            {
                return false;
            }

            if (game.manager.upcomingProcess != null)
            {
                return true;
            }

            int food = FoodApart(game, shelters, false);

            foreach (var abstractPlayer in game.Players.ToArray())
            {
                if (shelters.Any(shelter => shelter.room?.abstractRoom == abstractPlayer.Room))
                {
                    continue;
                }

                try
                {
                    JollyCustom.WarpAndRevivePlayer(abstractPlayer, shelterDoor.room.abstractRoom, shelterDoor.room.LocalCoordinateOfNode(0));
                }
                catch (Exception exception)
                {
                    _logger.LogWarning($"Could not bring player {abstractPlayer} to the shelter", exception);
                }
            }

            game.Win(food < SlugcatStats.SlugcatFoodMeter(game.StoryCharacter).y, false);

            return true;
        }
    }

    extension(SaveState saveState)
    {
        internal void EatApart(RainWorldGame game)
        {
            if (CanSleepApart(game) && !saveState.sessionEndingFromSpinningTopEncounter && ClosingApart(game) is { } shelters)
            {
                FoodApart(game, shelters, true);
            }
        }
    }
}
