using AbsoluteFriends.Options;

namespace AbsoluteFriends.Items;

internal static class Config
{
    public static Configurable<bool> Rock = null!;

    public static Configurable<bool> Spear = null!;

    public static Configurable<bool> ExplosiveSpear = null!;

    public static Configurable<bool> ElectricSpear = null!;

    public static Configurable<bool> HellSpear = null!;

    public static Configurable<bool> PoisonSpear = null!;

    public static Configurable<bool> LilyPuck = null!;

    public static Configurable<bool> ScavengerBomb = null!;

    public static Configurable<bool> SingularityBomb = null!;

    public static Configurable<bool> FireEgg = null!;

    public static Configurable<bool> SporePlant = null!;

    public static Configurable<bool> Boomerang = null!;

    public static Configurable<bool> FlareBomb = null!;

    public static Configurable<bool> PuffBall = null!;

    public static Configurable<bool> WaterNut = null!;

    public static Configurable<bool> FirecrackerPlant = null!;

    public static Configurable<bool> GraffitiBomb = null!;

    public static Configurable<bool> DangleFruit = null!;

    public static Configurable<bool> JellyFish = null!;

    public static Configurable<bool> Pomegranate = null!;

    public static Configurable<bool> Mushroom = null!;

    public static Configurable<bool> Snail = null!;

    public static Configurable<bool> TubeWorm = null!;

    public static Configurable<bool> Frog = null!;

    public static void Bind(Addon addon)
    {
        Rock = addon.Bind("Rock", true, new ConfigurableInfo(Options.Config.Downpour(
            "Will not be hit by a rock thrown by friends.",
            "Will not be hit by a rock thrown by friends. This also applies to joke rifle bullets."
        ), tags: ["Rock"]));
        Spear = addon.Bind("Spear", true, new ConfigurableInfo("Will not be hit by a spear thrown by friends.", tags: ["Spear"]));
        ExplosiveSpear = addon.Bind("ExplosiveSpear", true, new ConfigurableInfo("Will not be hit by an explosive spear thrown by friends. However, this does not apply to the explosion.", tags: ["Explosive Spear"]));
        ElectricSpear = addon.Bind("ElectricSpear", true, new ConfigurableInfo("Will not be hit by an electric spear thrown by friends.", tags: ["Electric Spear"]));
        HellSpear = addon.Bind("HellSpear", true, new ConfigurableInfo("Will not be hit by a fire spear thrown by friends.", tags: ["Fire Spear"]));
        PoisonSpear = addon.Bind("PoisonSpear", true, new ConfigurableInfo("Will not be hit by a poison spear thrown by friends.", tags: ["Poison Spear"]));
        LilyPuck = addon.Bind("LilyPuck", true, new ConfigurableInfo("Will not be hit by a lilypuck thrown by friends.", tags: ["Lilypuck"]));
        ScavengerBomb = addon.Bind("ScavengerBomb", true, new ConfigurableInfo(Options.Config.Downpour(
            "Will not be hit by a grenade thrown by friends. However, this does not apply to the explosion.",
            "Will not be hit by a grenade thrown by friends. However, this does not apply to the explosion. This also applies to joke rifle bullets."
        ), tags: ["Grenade"]));
        SingularityBomb = addon.Bind("SingularityBomb", true, new ConfigurableInfo("Will not be sucked in or instantly killed by a singularity bomb thrown by friends. However, this does not apply to the explosion. This also applies to joke rifle bullets.", tags: ["Singularity Bomb"]));
        FireEgg = addon.Bind("FireEgg", true, new ConfigurableInfo("Will not be attached by a fire egg thrown by friends. However, this does not apply to the explosion. This also applies to joke rifle bullets.", tags: ["Fire Egg"]));
        SporePlant = addon.Bind("SporePlant", true, new ConfigurableInfo(Options.Config.Downpour(
            "Will not be caught by bees triggered by friends. However, this does not apply to those spawned by approaching a beehive.",
            "Will not be caught by bees triggered by friends. However, this does not apply to those spawned by approaching a beehive. This also applies to joke rifle bullets."
        ), tags: ["Beehive"]));
        Boomerang = addon.Bind("Boomerang", true, new ConfigurableInfo("Will not be hit by a boomerang thrown by friends.", tags: ["Boomerang"]));
        FlareBomb = addon.Bind("FlareBomb", true, new ConfigurableInfo(Options.Config.Downpour(
            "Will not be hit by a flash bang thrown by friends.",
            "Will not be hit by a flash bang thrown by friends. This also applies to joke rifle bullets."
        ), tags: ["Flash Bang"]));
        PuffBall = addon.Bind("PuffBall", true, new ConfigurableInfo(Options.Config.Downpour(
            "Will not be hit by a spore puff thrown by friends.",
            "Will not be hit by a spore puff thrown by friends. This also applies to joke rifle bullets."
        ), tags: ["Spore Puff"]));
        WaterNut = addon.Bind("WaterNut", true, new ConfigurableInfo("Will not be hit by an unswollen bubble fruit thrown by friends.", tags: ["Bubble Fruit"]));
        FirecrackerPlant = addon.Bind("FirecrackerPlant", true, new ConfigurableInfo(Options.Config.Downpour(
            "Will not be stunned by a cherrybomb thrown by friends. However, this does not apply to the final explosion.",
            "Will not be stunned by a cherrybomb thrown by friends. However, this does not apply to the final explosion. This also applies to joke rifle bullets."
        ), tags: ["Cherrybomb"]));
        GraffitiBomb = addon.Bind("GraffitiBomb", true, new ConfigurableInfo("Will not be hit by a graffiti bomb thrown by friends.", tags: ["Graffiti Bomb"]));
        DangleFruit = addon.Bind("DangleFruit", true, new ConfigurableInfo("Will not be hit by a blue fruit fired from a joke rifle by friends.", tags: ["Blue Fruit"]));
        JellyFish = addon.Bind("JellyFish", true, new ConfigurableInfo("Will not be caught or stunned by the tentacles of a jellyfish held by a friend, and will not be stunned by a jellyfish thrown by friends.", tags: ["Jellyfish"]));
        Pomegranate = addon.Bind("Pomegranate", true, new ConfigurableInfo("Will not take damage from a pomegranate dropped by friends.", tags: ["Pomegranate"]));
        Mushroom = addon.Bind("Mushroom", true, new ConfigurableInfo("Will share the effects of mushrooms with slugcat friends.", tags: ["Mushroom"]))
            .Require(Core.Config.FriendSlugcat)
            .DisableInMeadow();
        Snail = addon.Bind("Snail", true, new ConfigurableInfo("Will not be stunned by the next pop from a snail thrown, hit, or killed by a friend.", tags: ["Snail"]));
        TubeWorm = addon.Bind("TubeWorm", true, new ConfigurableInfo("Will not be caught by a grappling worm's tongue used by friends.", tags: ["Grappling Worm"]));
        Frog = addon.Bind("Frog", true, new ConfigurableInfo("Will not be attached by a frog thrown by friends.", tags: ["Frog"]));

        addon.Group(
            "Weapons",
            "Will not be hurt by items friends throw or use.",
            Rock, Spear, ExplosiveSpear, ElectricSpear, HellSpear, PoisonSpear, LilyPuck, ScavengerBomb, SingularityBomb, FireEgg, SporePlant, Boomerang, FlareBomb, PuffBall, WaterNut, FirecrackerPlant, GraffitiBomb, DangleFruit, JellyFish, Pomegranate
        );
        addon.Group("Mushroom", "Will share the effects of mushrooms with slugcat friends.", Mushroom);
        addon.Group("Small Creatures", "Will not be caught or stunned by creatures friends throw or use.", Snail, TubeWorm, Frog);
    }
}
