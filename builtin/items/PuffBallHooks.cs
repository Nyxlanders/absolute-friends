using AbsoluteFriends.Core;
using AbsoluteFriends.Hooks;
using MonoMod.Cil;

namespace AbsoluteFriends.Items;

internal class DownpourPuffBallHooks : DownpourHooks
{
    protected override Configurable<bool>[] Options => [Config.PuffBall];

    [HookPatch(typeof(IL.JokeRifle), nameof(IL.JokeRifle.Use))]
    [HookTest([1072], ["dup; ldarg.0; ldc.i4"])]
    private static void IL_JokeRifle_Use(ILContext il) => il.JokeRifleUse(JokeRifle.AbstractRifle.AmmoType.Ash);
}
