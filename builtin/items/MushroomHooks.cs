using AbsoluteFriends.Core;
using AbsoluteFriends.Diagnostics;
using AbsoluteFriends.Hooks;
using Mono.Cecil.Cil;
using MonoMod.Cil;

namespace AbsoluteFriends.Items;

internal class MushroomHooks : BaseHooks
{
    protected override Configurable<bool>[] Options => [Config.Mushroom];

    [HookPatch(typeof(IL.Player), nameof(IL.Player.Update))]
    [HookTest([1716], ["callvirt List`1::GetEnumerator"])]
    private static void IL_Player_Update(ILContext il)
    {
        ILCursor c = new(il);

        if (c.TryGotoNext(i => i.MatchCallvirt<RainWorldGame>("get_AlivePlayers")))
        {
            c.Remove();
            c.Emit(OpCodes.Ldarg_0);
            c.EmitGuarded((RainWorldGame game, Player source) =>
            {
                if (!FriendUtils.IsFriendSession)
                {
                    return game.AlivePlayers;
                }

                List<AbstractCreature> friends = [];

                if (!source.IsTracked || source.inShortcut)
                {
                    return friends;
                }

                foreach (var abstractCreature in FriendUtils.TrackedFriendsIncludingPlayers)
                {
                    if (abstractCreature.world?.game == game && abstractCreature.realizedCreature is Player slugcat && !slugcat.dead && !slugcat.inShortcut && source.IsFriend(slugcat))
                    {
                        friends.Add(abstractCreature);
                    }
                }

                return friends;
            }, (game, _) => game.AlivePlayers);
        }
    }

    [HookPatch(typeof(On.RainWorldGame), nameof(On.RainWorldGame.Update))]
    private static void On_RainWorldGame_Update(On.RainWorldGame.orig_Update orig, RainWorldGame self)
    {
        orig(self);

        List<Player> slugcats = [];
        Dictionary<Player, (int Counter, float Effect)> active = [];

        foreach (var abstractCreature in FriendUtils.TrackedFriendsIncludingPlayers)
        {
            if (abstractCreature.world?.game == self && abstractCreature.realizedCreature is Player { dead: false } slugcat)
            {
                slugcats.Add(slugcat);

                if (!slugcat.inShortcut)
                {
                    active[slugcat] = (slugcat.mushroomCounter, slugcat.mushroomEffect);
                }
            }
        }

        if (active.Count == 0)
        {
            return;
        }

        foreach (var slugcat in slugcats)
        {
            List<(int Counter, float Effect)> sources = [.. active
                .Where(source => source.Key == slugcat || slugcat.IsFriend(source.Key))
                .Select(source => source.Value)];

            if (sources.Count > 0)
            {
                slugcat.mushroomCounter = sources.Select(source => source.Counter).Aggregate(Math.Max);
                slugcat.mushroomEffect = sources.Select(source => source.Effect).Aggregate(Math.Max);
            }
        }
    }
}
