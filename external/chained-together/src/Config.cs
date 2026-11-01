using AbsoluteFriends.Options;

namespace AbsoluteFriends.ChainedTogether;

internal static class Config
{
    public static Configurable<bool> Enabled = null!;

    public static Configurable<int> ChainLength = null!;

    public static Configurable<float> ChainElasticity = null!;

    public static Configurable<string> Style = null!;

    public static Configurable<float> SoundVolume = null!;

    public static void Bind(Addon addon)
    {
        Enabled = addon.Enabled!;
        ChainLength = addon.Bind("ChainLength", 300, new ConfigurableInfo("Sets the length of each chain between a player and a friend.", new ConfigAcceptableRange<int>(100, 1000), tags: ["Chain Length"]));
        ChainElasticity = addon.Bind("ChainElasticity", 0.3f, new ConfigurableInfo("Sets how far the chain stretches and how softly it pulls. A value of 0 keeps it rigid, and 1 lets it stretch to twice its length.", new ConfigAcceptableRange<float>(0f, 1f), tags: ["Chain Elasticity"]));
        Style = addon.Bind("ChainStyle", ChainStyle.DefaultKey, new ConfigurableInfo("Chooses how the chains look and sound.", tags: ["Chain Style"]))
            .UnsyncInMeadow();
        SoundVolume = addon.Bind("SoundVolume", 1f, new ConfigurableInfo("Sets the volume of the chains' sounds.", new ConfigAcceptableRange<float>(0f, 1f), tags: ["Chain Sound Volume"]))
            .UnsyncInMeadow();
    }
}
