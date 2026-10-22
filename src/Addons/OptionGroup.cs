namespace AbsoluteFriends.Addons;

public class OptionGroup
{
    internal OptionGroup(Addon addon, string label, string description, IReadOnlyList<Configurable<bool>> options)
    {
        Addon = addon;
        Label = label;
        Description = description;
        Options = options;
    }

    public Addon Addon { get; }

    public string Label { get; }

    public string Description { get; }

    public IReadOnlyList<Configurable<bool>> Options { get; }
}
