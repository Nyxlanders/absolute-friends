using System.Reflection;
using AbsoluteFriends.Diagnostics;
using AbsoluteFriends.Hooks;
using AbsoluteFriends.Options;

namespace AbsoluteFriends.Addons;

public class Addon
{
    private static ModManager.Mod? _host;

    private readonly List<Configurable<bool>> _switches = [];

    private readonly List<OptionGroup> _groups = [];

    private readonly List<string> _keywords = [];

    private readonly Dictionary<string, ConfigurableBase> _options = [];

    private bool _isRunning;

    internal Addon(AddonPlugin plugin, Action<MenuBuilder> buildMenu)
    {
        ModManager.Mod? mod = Owner(plugin.Info.Location);

        _host ??= Owner(typeof(Addon).Assembly.Location);

        Plugin = plugin;
        Id = plugin.Info.Metadata.GUID;
        Name = plugin.Info.Metadata.Name;
        Version = plugin.Info.Metadata.Version.ToString();
        Author = mod?.authors ?? "";
        AbsoluteFriendsVersion = plugin.AbsoluteFriendsVersion;
        AddonDependencyIds = plugin.AddonDependencies;
        IsBuiltIn = mod != null && _host != null && mod.id == _host.id;
        AddonDescription = plugin.AddonDescription;
        BuildMenu = buildMenu;
        HasMenu = plugin.GetType().GetMethod("BuildMenu", BindingFlags.Instance | BindingFlags.NonPublic)?.DeclaringType != typeof(AddonPlugin);
        Prefix = PrefixOf(Id);
        Enabled = RemixMenu.Instance.Bind($"{Prefix}_Addon", true, new ConfigurableInfo("Enables this addon.", tags: [Name]));
    }

    internal Addon(ModManager.Mod mod)
    {
        Id = mod.id;
        Name = mod.LocalizedName;
        Version = mod.version;
        Author = mod.authors == ModManager.Mod.authorBlank ? "" : mod.authors;
        AbsoluteFriendsVersion = "";
        AddonDependencyIds = [];
        IsBuiltIn = false;
        AddonDescription = mod.LocalizedDescription;
        BuildMenu = _ => { };
        Prefix = PrefixOf(Id);

        _keywords.Add(mod.name);
    }

    public string Id { get; }

    public string Name { get; }

    public string Version { get; }

    public string Author { get; }

    public string AbsoluteFriendsVersion { get; }

    public bool IsBuiltIn { get; }

    public bool IsApplied => Plugin != null;

    public bool IsMismatched => AbsoluteFriendsVersion.Length > 0 && AbsoluteFriendsVersion != AbsoluteFriends.Plugin.Version;

    public string AddonDescription { get; }

    public IReadOnlyList<string> AddonDependencyIds { get; }

    public bool AddonDependenciesApplied => AddonDependencyIds.All(id => AddonRegistry.Find(id)?.IsApplied == true);

    public int OptionCount => _switches.Count;

    public int EnabledOptionCount => _switches.Count(option => option.IsSelected);

    public Configurable<bool>? Enabled { get; }

    public IReadOnlyList<OptionGroup> Groups => _groups;

    internal bool HasUngroupedOptions => _options.Count > _groups.Sum(group => group.Options.Count);

    internal IEnumerable<string> Keywords => [Name, .. AddonDependencyIds.Select(AddonRegistry.RawNameOf), .. _keywords, .. HasDisabledInMeadow ? (string[])["Meadow"] : []];

    internal bool HasMenu { get; }

    internal Action<MenuBuilder> BuildMenu { get; }

    internal IReadOnlyDictionary<string, ConfigurableBase> ProfileOptions => _options;

    private bool HasDisabledInMeadow => MeadowOptions.IsMeadowInstalled && _options.Values.Any(option => option.IsOffInMeadow);

    private AddonPlugin? Plugin { get; }

    private string Prefix { get; }

    private Assembly? Assembly => Plugin?.GetType().Assembly;

    public Configurable<T> Bind<T>(string key, T defaultValue, ConfigurableInfo? info = null)
    {
        Configurable<T> option = RemixMenu.Instance.Bind($"{Prefix}_{key}", defaultValue, info).Require(Enabled!);

        _options[key] = option;

        if (option is Configurable<bool> boolOption && !_switches.Contains(boolOption))
        {
            _switches.Add(boolOption);
        }

        foreach (var tag in info?.Tags ?? [])
        {
            if (tag is string keyword && keyword.Length > 0 && !_keywords.Contains(keyword))
            {
                _keywords.Add(keyword);
            }
        }

        return option;
    }

    public OptionGroup? Group(string label, string description, params Configurable<bool>[] options)
    {
        AddonLogger logger = Reporter.GetLogger(Name);
        List<Configurable<bool>> members = [];

        foreach (var option in options ?? [])
        {
            if (option == null || !_switches.Contains(option))
            {
                logger.LogWarning($"Group {label} skipped {option?.key ?? "a missing option"} because it is not an option of this addon");

                continue;
            }

            if (members.Contains(option) || _groups.Any(group => group.Options.Contains(option)))
            {
                logger.LogWarning($"Group {label} skipped {option.key} because it already belongs to a group");

                continue;
            }

            members.Add(option);
        }

        if (members.Count == 0)
        {
            logger.LogWarning($"Group {label} was not created because it has no options");

            return null;
        }

        OptionGroup group = new(this, label, description, members);

        _groups.Add(group);

        return group;
    }

    internal bool HasOption(string query)
    {
        return _keywords.Any(keyword => keyword.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0);
    }

    internal void UpdateRunningState() => SetRunning(Enabled?.IsActive == true && AddonDependencyIds.All(AddonRegistry.IsRunning));

    internal void Stop() => SetRunning(false);

    private static string PrefixOf(string id) => string.Concat(id.Select(character => char.IsLetterOrDigit(character) ? character : '_'));

    private static ModManager.Mod? Owner(string location)
    {
        try
        {
            string path = Storage.NormalizePath(location);

            return ModManager.ActiveMods.FirstOrDefault(mod => path.StartsWith(Storage.NormalizePath(mod.path) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase));
        }
        catch (Exception)
        {
            return null;
        }
    }

    private void SetRunning(bool isRunning)
    {
        if (_isRunning == isRunning)
        {
            return;
        }

        _isRunning = isRunning;

        if (isRunning)
        {
            HookManager.Register(Assembly!);

            Plugin!.Activate();
        }
        else
        {
            Plugin!.Deactivate();

            HookManager.Unregister(Assembly!);
        }
    }
}
