using AbsoluteFriends.Hooks;
using JollyCoop;
using JollyCoop.JollyHUD;

namespace AbsoluteFriends.Resonance;

internal class ResonanceRevivalHooks : BaseHooks
{
    protected override Configurable<bool>[] Options => [Config.ResonanceGate, Config.ResonanceRoom, Config.ResonanceGrab];

    protected override string? Subject => "Resonance Revival";

    [HookPatch(typeof(On.JollyCoop.JollyHUD.JollyMeter.PlayerIcon), nameof(On.JollyCoop.JollyHUD.JollyMeter.PlayerIcon.Update))]
    private static void On_PlayerIcon_Update(On.JollyCoop.JollyHUD.JollyMeter.PlayerIcon.orig_Update orig, JollyMeter.PlayerIcon self)
    {
        orig(self);

        if (!self.dead || self.playerState is not { dead: false, permaDead: false })
        {
            return;
        }

        self.iconSprite.RemoveFromContainer();
        self.iconSprite = new FSprite("Kill_Slugcat") { scale = self.rad };
        self.meter.fContainer.AddChild(self.iconSprite);
        self.dead = false;
        self.meter.customFade = 5f;
        self.blink = 3f;
        self.gradient.color = JollyCustom.ColorClamp(self.color, -1f, 360f, 60f, 360f, -1f, 360f);
    }

    [HookPatch(typeof(On.JollyCoop.JollyHUD.JollyPlayerSpecificHud), nameof(On.JollyCoop.JollyHUD.JollyPlayerSpecificHud.Update))]
    private static void On_JollyPlayerSpecificHud_Update(On.JollyCoop.JollyHUD.JollyPlayerSpecificHud.orig_Update orig, JollyPlayerSpecificHud self)
    {
        if (self.addedDeathBumpThisSession && self.PlayerState is { dead: false, permaDead: false })
        {
            self.addedDeathBumpThisSession = false;
            self.deathBump?.slatedForDeletion = true;
        }

        orig(self);
    }
}
