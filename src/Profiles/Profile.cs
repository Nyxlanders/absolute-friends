using System.Runtime.Serialization;

namespace AbsoluteFriends.Profiles;

[DataContract]
internal class Profile
{
    [DataMember(Order = 0)]
    public int FormatVersion { get; set; } = 1;

    [DataMember(Order = 1)]
    public string Id { get; set; } = "";

    [DataMember(Order = 2)]
    public string Name { get; set; } = "";

    [DataMember(Order = 3)]
    public string CreatedAt { get; set; } = "";

    [DataMember(Order = 4)]
    public string ModifiedAt { get; set; } = "";

    [DataMember(Order = 5)]
    public Dictionary<string, ProfileAddon> Addons { get; set; } = [];
}

[DataContract]
internal class ProfileAddon
{
    [DataMember(Order = 0)]
    public string Name { get; set; } = "";

    [DataMember(Order = 1)]
    public string Version { get; set; } = "";

    [DataMember(Order = 2)]
    public string Enabled { get; set; } = "true";

    [DataMember(Order = 3)]
    public Dictionary<string, string> Options { get; set; } = [];
}

internal readonly struct ProfileResult(int addons, int options, int unavailableAddons, int unavailableOptions)
{
    internal int Addons { get; } = addons;

    internal int Options { get; } = options;

    internal int UnavailableAddons { get; } = unavailableAddons;

    internal int UnavailableOptions { get; } = unavailableOptions;
}

internal readonly struct ProfileSummary(int enabledAddons, int addons, int enabledOptions, int options, int settings, int unavailableAddons, int unavailableOptions)
{
    internal int EnabledAddons { get; } = enabledAddons;

    internal int Addons { get; } = addons;

    internal int EnabledOptions { get; } = enabledOptions;

    internal int Options { get; } = options;

    internal int Settings { get; } = settings;

    internal int UnavailableAddons { get; } = unavailableAddons;

    internal int UnavailableOptions { get; } = unavailableOptions;
}
