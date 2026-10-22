using AbsoluteFriends.Options;

namespace AbsoluteFriends.Pilgrimage;

public static class Config
{
    public static Configurable<bool> Gate = null!;

    public static Configurable<float> GateTime = null!;

    public static Configurable<bool> GateForce = null!;

    public static Configurable<float> GateForceTime = null!;

    public static Configurable<bool> AnyShelter = null!;

    public static Configurable<bool> Perseverance = null!;

    public static Configurable<bool> Echo = null!;

    public static Configurable<bool> TempleGuard = null!;

    public static Configurable<bool> VoidSea = null!;

    public static void Bind(Addon addon)
    {
        Gate = addon.Bind("Gate", true, new ConfigurableInfo(Options.Config.Watcher(
            "Shelters and karma gates wait for tracked friends and will not activate while any player is entering control inputs.",
            "Shelters, karma gates, and warp points wait for tracked friends and will not activate while any player is entering control inputs."
        ), tags: ["Gate Delay"]));
        GateTime = addon.Bind("GateTime", 1f, new ConfigurableInfo(Options.Config.Watcher(
            "Sets the minimum time (in seconds) of no control input from the player required for shelters and karma gates to activate.",
            "Sets the minimum time (in seconds) of no control input from the player required for shelters, karma gates, and warp points to activate."
        ), new ConfigAcceptableRange<float>(0f, 5f)))
            .Require(Gate);
        GateForce = addon.Bind("GateForce", true, new ConfigurableInfo(Options.Config.Watcher(
            "Shelters and karma gates forcefully activate, ignoring non-player friends.",
            "Shelters, karma gates, and warp points forcefully activate, ignoring non-player friends."
        ), tags: ["Gate Force"]))
            .Require(Gate);
        GateForceTime = addon.Bind("GateForceTime", 3f, new ConfigurableInfo(Options.Config.Watcher(
            "Sets how much longer (in seconds) the player must go without control input for shelters and karma gates to forcefully activate, ignoring non-player friends.",
            "Sets how much longer (in seconds) the player must go without control input for shelters, karma gates, and warp points to forcefully activate, ignoring non-player friends."
        ), new ConfigAcceptableRange<float>(0f, 5f)))
            .Require(GateForce);
        AnyShelter = addon.Bind("AnyShelter", true, new ConfigurableInfo("Players and friends can sleep in different shelters.", tags: ["Any Shelter"]));
        Perseverance = addon.Bind("Perseverance", true, new ConfigurableInfo("As on Jolly Co-op's Easy, dead players neither block shelters and gates nor end the game.", tags: ["Perseverance"]));
        Echo = addon.Bind("Echo", true, new ConfigurableInfo("Echoes do not put tracked friends to sleep or send them back to their dens.", tags: ["Echo"]));
        TempleGuard = addon.Bind("TempleGuard", true, new ConfigurableInfo("Guardians do not target tracked friends or move them with telekinesis when the players meet the Karma requirement to pass.", tags: ["Guardian"]))
            .DisableInMeadow();
        VoidSea = addon.Bind("VoidSea", true, new ConfigurableInfo(Options.Config.Watcher(
            "Tracked friends follow the player through the Void Sea and return to the last shelter after ascension. Returned friends stay in the shelter after disabling this option.",
            "Tracked friends follow the player through the Void Sea and the final warps, and return to the last shelter after ascension. Returned friends stay in the shelter after disabling this option."
        ), tags: ["Void Sea"]))
            .DisableInMeadow();

        addon.Group(
            "Gates",
            Options.Config.Watcher("Shelters and karma gates wait for tracked friends.", "Shelters, karma gates, and warp points wait for tracked friends."),
            Gate, GateForce
        );
        addon.Group("Any Shelter", "Players and friends can sleep in different shelters.", AnyShelter);
        addon.Group("Perseverance", "As on Jolly Co-op's Easy, dead players neither block shelters and gates nor end the game.", Perseverance);
        addon.Group("Echo", "Echoes do not put tracked friends to sleep or send them back to their dens.", Echo);
        addon.Group("Ascension", "Guardians spare tracked friends, who follow the player through the Void Sea.", TempleGuard, VoidSea);
    }
}
