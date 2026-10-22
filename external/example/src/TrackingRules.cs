using AbsoluteFriends.Core;
using AbsoluteFriends.Manager;
using AbsoluteFriends.Options;

namespace AbsoluteFriendsExample;

internal static class TrackingRules
{
    internal static void Register()
    {
        // A tracking rule returns false to exclude a creature. A creature stays tracked only while every rule returns true.
        // Untracked friends are still friends; other addons simply stop treating them as companions, for example at gates.
        // Players are always tracked, so rules are never asked about them.
        FriendUtils.RegisterTrackingRule(IsTrackedHunterDaddy);

        // A name rule returns a name, or null to let the next rule or the default name decide.
        // The first non-empty name wins, and rules run in registration order.
        // The Manager's nickname rule is one of these rules, so a Manager nickname can hide this name or be hidden by it, depending on which addon registered first.
        FriendDisplayNames.RegisterNameRule(HunterDaddyName);

        // Players are named by player name rules instead, registered with FriendDisplayNames.RegisterPlayerNameRule; the Meadow addon uses one for online names.
    }

    internal static void Unregister()
    {
        FriendUtils.UnregisterTrackingRule(IsTrackedHunterDaddy);
        FriendDisplayNames.UnregisterNameRule(HunterDaddyName);
    }

    private static bool IsTrackedHunterDaddy(AbstractCreature abstractCreature) => !AddonConfig.UntrackedHunter.IsActive || !abstractCreature.IsHunterDaddy;

    // Name rules run whenever a name is drawn, so keep them cheap.
    // In Rain Meadow, each player's game gives the same creature its own EntityID. HostIdOf returns the ID the host knows it by, so every player sees the same name.
    // It returns the creature's own ID outside Rain Meadow. The host sends IDs only for the friends it lists, so it returns null for any other creature, which keeps the default name.
    private static string? HunterDaddyName(AbstractCreature abstractCreature) => AddonConfig.HunterName.IsActive
        && abstractCreature.IsHunterDaddy
        && FriendManager.HostIdOf(abstractCreature) is { } id
            ? $"Player {Math.Abs(id.number)}"
            : null;
}
