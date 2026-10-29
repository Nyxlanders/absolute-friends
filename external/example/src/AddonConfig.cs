using AbsoluteFriends.Addons;
using AbsoluteFriends.Options;

namespace AbsoluteFriendsExample;

// A separate config class is only an organizational choice; addon options may be bound elsewhere.
internal static class AddonConfig
{
    internal static Configurable<bool> Friendship = null!;

    internal static Configurable<bool> AggressiveHunter = null!;

    internal static Configurable<bool> HunterDaddyOwnership = null!;

    internal static Configurable<bool> HunterName = null!;

    internal static Configurable<bool> GreenNeuronOwnership = null!;

    internal static void Bind(Addon addon)
    {
        // Addon.Bind makes settings profile-aware. Require affects both the UI and runtime IsActive checks.
        Friendship = addon.Bind("Friendship", true, new ConfigurableInfo("Treats Hunter Long Legs as a friend of players.", tags: ["Hunter Long Legs"]))
            .Require(AbsoluteFriends.Core.Config.FriendCreature);
        AggressiveHunter = addon.Bind("AggressiveHunter", true, new ConfigurableInfo("Preserves Hunter's hostile relationship with Hunter Long Legs.", tags: ["Aggressive Hunter"]))
            .Require(Friendship);
        HunterDaddyOwnership = addon.Bind("HunterDaddyOwnership", true, new ConfigurableInfo("Makes Hunter Long Legs own anything held by its tentacles.", tags: ["Tentacle Ownership"]));
        HunterName = addon.Bind("HunterName", true, new ConfigurableInfo("Gives each Hunter Long Legs a name.", tags: ["Long Legs Name"]));
        GreenNeuronOwnership = addon.Bind("GreenNeuronOwnership", true, new ConfigurableInfo("Makes every green Neuron self-owned.", tags: ["Green Neuron Ownership"]));

        // Every option here works in Rain Meadow. Mark one that does not, such as one that changes only this player's game, with DisableInMeadow:
        // it is off in Rain Meadow whatever its value, IsActive is false for it and for every option that requires it,
        // and players with Rain Meadow installed see it marked in the menu.
        // LocalOption = addon.Bind("LocalOption", true, new ConfigurableInfo("Works only outside Rain Meadow.", tags: ["Local Option"])).DisableInMeadow();

        // In Rain Meadow every player uses the host's values for this addon's options. Mark an option that changes only what this player sees,
        // such as a visual effect, with UnsyncInMeadow, so each player keeps their own value for it while the rest follow the host:
        // VisualOption = addon.Bind("VisualOption", true, new ConfigurableInfo("Shows an effect only this player sees.", tags: ["Visual Option"])).UnsyncInMeadow();
        // Players with Rain Meadow installed see a note under each such option's description. Mark each option; the addon's enable toggle is always shared.

        // Group puts one switch in the Simple view of the framework's Addons tab that turns all of its options on or off together.
        // An addon may group only its own Boolean options, and each option may belong to only one group.
        // Options left out of every group appear only in the Detailed view.
        addon.Group("Hunter Long Legs", "Treats Hunter Long Legs as a friend and lets it own what it holds.", Friendship, AggressiveHunter, HunterDaddyOwnership);
        addon.Group("Long Legs Name", "Gives each Hunter Long Legs a name.", HunterName);
        addon.Group("Green Neuron Ownership", "Makes every green Neuron self-owned.", GreenNeuronOwnership);
    }
}
