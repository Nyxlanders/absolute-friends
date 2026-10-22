using AbsoluteFriends.Core;
using AbsoluteFriends.Hooks;
using MonoMod.Cil;

namespace AbsoluteFriends.Players;

internal class SaintTongueHooks : DownpourHooks
{
    protected override Configurable<bool>[] Options => [Config.SaintTongue];

    [HookPatch(typeof(IL.Player.Tongue), nameof(IL.Player.Tongue.Update))]
    [HookTest([200], ["ldfld CollisionResult::chunk"])]
    private static void IL_Player_Tongue_Update(ILContext il) => il.TongueUpdate<Player.Tongue>(tongue => tongue?.player);
}
