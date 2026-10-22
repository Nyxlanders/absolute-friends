using AbsoluteFriends.Options;
using AbsoluteFriends.Utils;

namespace AbsoluteFriends.Meadow;

internal static class ArrivingAvatars
{
    internal static void Register()
    {
        On.AbstractCreature.ChangeRooms -= On_AbstractCreature_ChangeRooms;
        On.AbstractCreature.ChangeRooms += On_AbstractCreature_ChangeRooms;
    }

    internal static void Unregister() => On.AbstractCreature.ChangeRooms -= On_AbstractCreature_ChangeRooms;

    private static void On_AbstractCreature_ChangeRooms(On.AbstractCreature.orig_ChangeRooms orig, AbstractCreature self, WorldCoordinate newCoord)
    {
        if (
            MeadowOptions.IsInLobby
            && self.IsSlugcat
            && self.realizedCreature == null
            && self.Room is { realizedRoom: null }
            && self.world.GetAbstractRoom(newCoord) is { realizedRoom.shortCutsReady: true } nextRoom
        )
        {
            WorldCoordinate pos = self.pos;

            self.pos = newCoord;
            self.Realize();
            self.pos = pos;

            if (self.realizedCreature is { } creature)
            {
                creature.inShortcut = true;
                self.world.game.shortcuts.CreatureEnterFromAbstractRoom(creature, nextRoom, newCoord.abstractNode);
            }
        }

        orig(self, newCoord);
    }
}
