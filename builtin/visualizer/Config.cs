using AbsoluteFriends.Options;

namespace AbsoluteFriends.Visualizer;

internal static class Config
{
    public static Configurable<bool> FriendMarker = null!;

    public static Configurable<bool> FriendMeter = null!;

    public static Configurable<bool> BumpEffect = null!;

    public static Configurable<bool> Icon = null!;

    public static Configurable<int> IconStyle = null!;

    public static Configurable<bool> Name = null!;

    public static Configurable<bool> Link = null!;

    public static Configurable<bool> FriendRelationship = null!;

    public static Configurable<int> ShownFriendship = null!;

    public static Configurable<bool> Owner = null!;

    public static Configurable<int> UpdateInterval = null!;

    public static void Bind(Addon addon)
    {
        FriendMarker = addon.Bind("FriendMarker", true, new ConfigurableInfo("Shows tracked friends' arrows and names in their colors, and their icons at the screen edge when in another room.", tags: ["Friend Marker"]))
            .UnsyncInMeadow();
        FriendMeter = addon.Bind("FriendMeter", true, new ConfigurableInfo("Shows tracked friends' icons at the bottom right, pulsing when left behind or unconscious.", tags: ["Friend Meter"]))
            .UnsyncInMeadow();
        BumpEffect = addon.Bind("BumpEffect", true, new ConfigurableInfo("Plays animations when tracked friends die and when friends or players revive.", tags: ["Bump Effect"]))
            .UnsyncInMeadow();

        Icon = addon.Bind("Icon", false, new ConfigurableInfo("Draws icons above shown friends, and owners' icons above owned things. Dead creatures' icons are dimmed.", tags: ["Relationship Icon"]))
            .UnsyncInMeadow();
        IconStyle = addon.Bind("IconStyle", (int)RelationshipIconStyle.Creature, new ConfigurableInfo("Chooses creature artwork or friendship symbols for relationship icons.", new ConfigAcceptableRange<int>(0, 1), tags: ["Icon Style"]))
            .Require(Icon)
            .UnsyncInMeadow();
        Name = addon.Bind("Name", false, new ConfigurableInfo("Draws names above shown friends, and owners' names above owned things. Dead creatures' names are dimmed.", tags: ["Relationship Name"]))
            .UnsyncInMeadow();
        Link = addon.Bind("Link", false, new ConfigurableInfo("Draws lines between shown friends and between owners and owned things. Ownership lines are thickest at the owner's end.", tags: ["Relationship Link"]))
            .UnsyncInMeadow();

        FriendRelationship = addon.Bind("FriendRelationship", true, new ConfigurableInfo("Includes friendship in relationship overlays.", tags: ["Friend Relationship"]))
            .RequireAny(Icon, Name, Link)
            .UnsyncInMeadow();
        ShownFriendship = addon.Bind("ShownFriendship", (int)ShownFriendshipChoice.Indirect, new ConfigurableInfo("Sets the weakest friendship that relationship overlays show. Moving right shows more friends.", new ConfigAcceptableRange<int>(0, 3), tags: ["Shown Friendship"]))
            .Require(FriendRelationship)
            .UnsyncInMeadow();
        Owner = addon.Bind("Owner", false, new ConfigurableInfo("Includes ownership in relationship overlays.", tags: ["Owner Relationship"]))
            .RequireAny(Icon, Name, Link)
            .UnsyncInMeadow();
        UpdateInterval = addon.Bind("UpdateInterval", 10, new ConfigurableInfo("Sets how many frames pass between friend and owner checks.", new ConfigAcceptableRange<int>(1, 40), tags: ["Update Interval"]))
            .RequireAny(FriendMarker, FriendMeter, Icon, Name, Link)
            .UnsyncInMeadow();

        addon.Group("Friend Display", "Marks tracked friends.", FriendMarker, FriendMeter, BumpEffect);
        addon.Group("Relationship Overlay", "Visualizes relationships with icons, names, and links.", Icon, Name, Link);
    }
}
