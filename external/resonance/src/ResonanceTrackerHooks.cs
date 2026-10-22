using AbsoluteFriends.Hooks;
using AbsoluteFriends.Utils;

namespace AbsoluteFriends.Resonance;

internal class ResonanceTrackerHooks : BaseHooks
{
    protected override Configurable<bool>[] Options => [Config.ResonanceGate, Config.ResonanceRoom, Config.ResonanceGrab];

    protected override string? Subject => "Resonance Tracker";

    [HookPatch(typeof(On.RainWorldGame), nameof(On.RainWorldGame.ctor))]
    private static void On_RainWorldGame_ctor(On.RainWorldGame.orig_ctor orig, RainWorldGame self, ProcessManager manager)
    {
        orig(self, manager);

        ResonanceUtils.ResetTrackers();
    }

    [HookPatch(typeof(On.RainWorldGame), nameof(On.RainWorldGame.Update))]
    private static void On_RainWorldGame_Update(On.RainWorldGame.orig_Update orig, RainWorldGame self)
    {
        orig(self);

        ResonanceUtils.UpdatePendingWarps(self);
    }

    [HookPatch(typeof(On.Creature), nameof(On.Creature.Die))]
    private static void On_Creature_Die(On.Creature.orig_Die orig, Creature self)
    {
        bool wasDead = self.dead;

        orig(self);

        if (!wasDead && self.abstractCreature is { } abstractCreature)
        {
            abstractCreature.Tracker.Death = 0;
        }
    }

    [HookPatch(typeof(On.Creature), nameof(On.Creature.Update))]
    private static void On_Creature_Update(On.Creature.orig_Update orig, Creature self, bool eu)
    {
        orig(self, eu);

        if (self.abstractCreature?.FindTracker() is not { } tracker)
        {
            return;
        }

        if (self.dead)
        {
            ++tracker.Death;

            return;
        }

        if (tracker.Reflection > 0)
        {
            --tracker.Reflection;

            self.Vibrate(tracker.Progress);
        }

        if (self.HasAuthority)
        {
            self.Stagger(tracker.Severity(self.room));
        }
    }
}
