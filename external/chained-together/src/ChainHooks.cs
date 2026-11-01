using AbsoluteFriends.Hooks;

namespace AbsoluteFriends.ChainedTogether;

internal class ChainHooks : BaseHooks
{
    protected override Configurable<bool>[] Options => [Config.Enabled];

    [HookPatch(typeof(On.Room), nameof(On.Room.Update))]
    private static void On_Room_Update(On.Room.orig_Update orig, Room self)
    {
        orig(self);

        Chain.Scan(self);
    }

    [HookPatch(typeof(On.RainWorldGame), nameof(On.RainWorldGame.ShutDownProcess))]
    private static void On_RainWorldGame_ShutDownProcess(On.RainWorldGame.orig_ShutDownProcess orig, RainWorldGame self)
    {
        orig(self);

        Chain.Clear();
    }
}
