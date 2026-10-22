using AbsoluteFriends.Core;
using AbsoluteFriends.Hooks;
using AbsoluteFriends.Utils;

namespace AbsoluteFriends.Pilgrimage;

internal class AnyShelterHooks : BaseHooks
{
    protected override Configurable<bool>[] Options => [Config.AnyShelter];

    [HookPatch(typeof(On.RegionState), nameof(On.RegionState.CreatureToStringInDenPos))]
    private static string On_RegionState_CreatureToStringInDenPos(On.RegionState.orig_CreatureToStringInDenPos orig, RegionState self, AbstractCreature critter, int validSaveShelter, int activeGate)
    {
        if (
            self.world.GetAbstractRoom(critter.pos) is { shelter: true } shelter
            && ShelterDoor.IsTileInsideShelterRange(shelter, critter.pos.Tile)
            && (critter.IsTracked || critter.IsSlugpup || self.world.game.AllPlayers.Any(abstractPlayer => critter.Like(abstractPlayer)))
        )
        {
            validSaveShelter = critter.pos.room;
        }

        return orig(self, critter, validSaveShelter, activeGate);
    }

    [HookPatch(typeof(On.ShelterDoor), nameof(On.ShelterDoor.Close))]
    private static void On_ShelterDoor_Close(On.ShelterDoor.orig_Close orig, ShelterDoor self)
    {
        if (!self.TryCloseApart())
        {
            orig(self);
        }
    }

    [HookPatch(typeof(On.ShelterDoor), nameof(On.ShelterDoor.DoorClosed))]
    private static void On_ShelterDoor_DoorClosed(On.ShelterDoor.orig_DoorClosed orig, ShelterDoor self)
    {
        if (!self.TryEndApart())
        {
            orig(self);
        }
    }

    [HookPatch(typeof(On.SaveState), nameof(On.SaveState.SessionEnded))]
    private static void On_SaveState_SessionEnded(On.SaveState.orig_SessionEnded orig, SaveState self, RainWorldGame game, bool survived, bool newMalnourished)
    {
        if (survived)
        {
            self.EatApart(game);
        }

        orig(self, game, survived, newMalnourished);
    }
}
