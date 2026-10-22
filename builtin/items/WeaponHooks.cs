using AbsoluteFriends.Core;
using AbsoluteFriends.Hooks;
using AbsoluteFriends.Options;
using MoreSlugcats;
using RWCustom;
using UnityEngine;

namespace AbsoluteFriends.Items;

internal class WeaponHooks : BaseHooks
{
    protected override Configurable<bool>[] Options => [
        Config.Rock,
        Config.Spear,
        Config.ExplosiveSpear,
        Config.ElectricSpear,
        Config.HellSpear,
        Config.PoisonSpear,
        Config.LilyPuck,
        Config.ScavengerBomb,
        Config.SingularityBomb,
        Config.FireEgg,
        Config.SporePlant,
        Config.Boomerang,
        Config.Mushroom,
        Config.FlareBomb,
        Config.PuffBall,
        Config.WaterNut,
        Config.FirecrackerPlant,
        Config.GraffitiBomb,
        Config.JellyFish,
        Config.Pomegranate,
        Config.Snail,
        Config.TubeWorm,
        Config.Frog
    ];

    protected override string? Subject => "Weapon";

    [HookPatch(typeof(On.Weapon), nameof(On.Weapon.Thrown))]
    private static void On_Weapon_Thrown(On.Weapon.orig_Thrown orig, Weapon self, Creature thrownBy, Vector2 thrownPos, Vector2? firstFrameTraceFromPos, IntVector2 throwDir, float frc, bool eu)
    {
        if (self switch
        {
            WaterNut waterNut => Config.WaterNut.IsActive && !waterNut.AbstrNut.swollen,
            Rock => Config.Rock.IsActive,
            ExplosiveSpear => Config.ExplosiveSpear.IsActive,
            ElectricSpear => Config.ElectricSpear.IsActive,
            Spear { abstractSpear: { } abstractSpear } when abstractSpear.hue != 0f || abstractSpear.poison > 0f
                => (abstractSpear.hue != 0f && Config.HellSpear.IsActive) || (abstractSpear.poison > 0f && Config.PoisonSpear.IsActive),
            Spear => Config.Spear.IsActive,
            LillyPuck => Config.LilyPuck.IsActive,
            ScavengerBomb => Config.ScavengerBomb.IsActive,
            SingularityBomb => Config.SingularityBomb.IsActive,
            SporePlant => Config.SporePlant.IsActive,
            Boomerang => Config.Boomerang.IsActive,
            FlareBomb => Config.FlareBomb.IsActive,
            PuffBall => Config.PuffBall.IsActive,
            FirecrackerPlant => Config.FirecrackerPlant.IsActive,
            GraffitiBomb => Config.GraffitiBomb.IsActive,
            _ => false
        })
        {
            self.SetOwner(thrownBy);
        }

        orig(self, thrownBy, thrownPos, firstFrameTraceFromPos, throwDir, frc, eu);
    }

    [HookPatch(typeof(On.Weapon), nameof(On.Weapon.Update))]
    private static void On_Weapon_Update(On.Weapon.orig_Update orig, Weapon self, bool eu)
    {
        orig(self, eu);

        if (self.lastMode == Weapon.Mode.Thrown && self.mode != Weapon.Mode.Thrown && self is not (ExplosiveSpear or ScavengerBomb or SingularityBomb or SporePlant or FlareBomb or FirecrackerPlant))
        {
            self.SetOwner();
        }
    }

    [HookPatch(typeof(On.Weapon), nameof(On.Weapon.HitThisObject))]
    private static bool On_Weapon_HitThisObject(On.Weapon.orig_HitThisObject orig, Weapon self, PhysicalObject obj)
    {
        return !self.IsFriend(obj) && orig(self, obj);
    }

    [HookPatch(typeof(On.SocialEventRecognizer), nameof(On.SocialEventRecognizer.WeaponAttack))]
    private static void On_SocialEventRecognizer_WeaponAttack(On.SocialEventRecognizer.orig_WeaponAttack orig, SocialEventRecognizer self, PhysicalObject weapon, Creature thrower, Creature victim, bool hit)
    {
        if (!hit && weapon.IsFriend(victim))
        {
            return;
        }

        orig(self, weapon, thrower, victim, hit);
    }
}
