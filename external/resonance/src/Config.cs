using AbsoluteFriends.Options;

namespace AbsoluteFriends.Resonance;

internal static class Config
{
    public static Configurable<bool> ResonanceGate = null!;

    public static Configurable<bool> ResonanceRoom = null!;

    public static Configurable<bool> ResonanceGrab = null!;

    public static Configurable<bool> ResonanceWarp = null!;

    public static Configurable<bool> ResonanceMend = null!;

    public static Configurable<bool> ResonanceCost = null!;

    public static Configurable<float> ResonanceCostRatio = null!;

    public static Configurable<bool> ResonanceAftershock = null!;

    public static Configurable<float> ResonanceAftershockRatio = null!;

    public static Configurable<bool> ResonanceMelt = null!;

    public static Configurable<bool> ResonanceShockwave = null!;

    public static void Bind(Addon addon)
    {
        ResonanceGate = addon.Bind("ResonanceGate", true, new ConfigurableInfo(Options.Config.Watcher(
            "Resonance occurs automatically when a shelter or karma gate closes, or a karma room activates, reaching tracked friends anywhere in the world.",
            "Resonance occurs automatically when a shelter or karma gate closes, or a karma room or warp point activates, reaching tracked friends anywhere in the world."
        ), tags: ["Gate Resonance"]));
        ResonanceRoom = addon.Bind("ResonanceRoom", true, new ConfigurableInfo("Resonance occurs when a player presses jump, reaching friends in the room. With Gate Resonance enabled, resonance inside a gate also reaches tracked friends in other rooms.", tags: ["Room Resonance"]));
        ResonanceGrab = addon.Bind("ResonanceGrab", true, new ConfigurableInfo("Resonance occurs when a player holds a tracked friend and presses jump, mending or reviving that friend.", tags: ["Grab Resonance"]));
        ResonanceWarp = addon.Bind("ResonanceWarp", true, new ConfigurableInfo("Warps eligible tracked friends to the player's location upon resonance. Warped friends stay where they arrived after disabling this option.", tags: ["Warp"]))
            .RequireAny(ResonanceGate, ResonanceRoom);
        ResonanceMend = addon.Bind("ResonanceMend", true, new ConfigurableInfo("Mends eligible tracked friends upon resonance, closing their wounds and bringing back the ones that have died. Revived friends stay alive after disabling this option.", tags: ["Mend"]))
            .RequireAny(ResonanceGate, ResonanceRoom, ResonanceGrab);
        ResonanceCost = addon.Bind("ResonanceCost", true, new ConfigurableInfo("A resonance demands a cost, which grows with how far away and how big each friend is and the time it has spent dead, and stuns everyone who resonated, leaving them out of breath.", tags: ["Cost"]))
            .RequireAny(ResonanceWarp, ResonanceMend);
        ResonanceCostRatio = addon.Bind("ResonanceCostRatio", 1f, new ConfigurableInfo("Sets how much of the resonance cost is demanded. A value of 1 demands it in full, and 0 makes a resonance instant and free.", new ConfigAcceptableRange<float>(0f, 1f)))
            .Require(ResonanceCost);
        ResonanceAftershock = addon.Bind("ResonanceAftershock", true, new ConfigurableInfo("The paid resonance cost lingers as an aftershock, stunning everyone who resonated now and then until the cycle ends.", tags: ["Aftershock"]))
            .Require(ResonanceCost);
        ResonanceAftershockRatio = addon.Bind("ResonanceAftershockRatio", 0.1f, new ConfigurableInfo("Sets how much of the paid resonance cost lingers as an aftershock. A value of 1 leaves all of it behind, and 0 leaves nothing.", new ConfigAcceptableRange<float>(0f, 1f)))
            .Require(ResonanceAftershock);
        ResonanceMelt = addon.Bind("ResonanceEffect", true, new ConfigurableInfo("Shows the room melting into the void during a gate resonance.", tags: ["Resonance Melt"]))
            .Require(ResonanceGate)
            .RequireAny(ResonanceWarp, ResonanceMend)
            .UnsyncInMeadow();
        ResonanceShockwave = addon.Bind("ResonanceShockwave", true, new ConfigurableInfo("Shows shockwaves bursting around everyone who resonated.", tags: ["Resonance Shockwave"]))
            .RequireAny(ResonanceWarp, ResonanceMend)
            .UnsyncInMeadow();

        addon.Group("Resonance", "Resonance warps and mends tracked friends.", ResonanceGate, ResonanceRoom, ResonanceGrab, ResonanceWarp, ResonanceMend);
        addon.Group("Backlash", "Resonance demands a cost and leaves an aftershock.", ResonanceCost, ResonanceAftershock);
        addon.Group("Visual Effect", "Adds visual effects of the room melting and shockwaves bursting during resonance.", ResonanceMelt, ResonanceShockwave);
    }
}
