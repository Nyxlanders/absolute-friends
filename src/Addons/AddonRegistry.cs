using AbsoluteFriends.Diagnostics;
using AbsoluteFriends.Options;

namespace AbsoluteFriends.Addons;

internal static class AddonRegistry
{
    private static readonly string[] _builtIn = [
        "pjhgamma.absolutefriends.core",
        "pjhgamma.absolutefriends.manager",
        "pjhgamma.absolutefriends.meadow",
        "pjhgamma.absolutefriends.visualizer",
        "pjhgamma.absolutefriends.general",
        "pjhgamma.absolutefriends.items",
        "pjhgamma.absolutefriends.players",
        "pjhgamma.absolutefriends.creatures",
        "pjhgamma.absolutefriends.iterators",
        "pjhgamma.absolutefriends.pilgrimage",
    ];

    private static readonly List<Addon> _addons = [];

    internal static IEnumerable<Addon> Ordered => _addons.Concat(DiscoverUnavailable())
        .OrderByDescending(addon => addon.IsBuiltIn)
        .ThenBy(Rank)
        .ThenBy(addon => addon.Name, StringComparer.Ordinal);

    internal static IReadOnlyList<Addon> Applied => _addons;

    public static Addon Add(AddonPlugin plugin, Action<MenuBuilder> buildMenu)
    {
        Remove(plugin);

        Addon addon = new(plugin, buildMenu);

        _addons.Add(addon);

        BindDependencies();

        return addon;
    }

    public static void Remove(AddonPlugin plugin)
    {
        foreach (var addon in _addons.Where(addon => addon.Id == plugin.Info.Metadata.GUID).ToArray())
        {
            addon.Stop();

            _addons.Remove(addon);
        }
    }

    internal static void BuildMenu(Addon addon, MenuBuilder menu)
    {
        try
        {
            addon.BuildMenu(menu);
        }
        catch (Exception exception)
        {
            Reporter.GetLogger(addon.Name).LogError("Could not build the menu and was disabled", exception);

            addon.Stop();
            _addons.Remove(addon);
        }
    }

    internal static void UpdateRunningStates()
    {
        foreach (var addon in _addons.ToArray())
        {
            addon.UpdateRunningState();
        }
    }

    internal static void Reset()
    {
        foreach (var addon in _addons.ToArray())
        {
            addon.Stop();
        }
    }

    internal static Addon? Find(string id) => _addons.FirstOrDefault(addon => addon.Id == id);

    internal static bool IsRunning(string id) => Find(id)?.Enabled?.IsActive == true;

    internal static string RawNameOf(string id) => Find(id)?.Name ?? id;

    private static void BindDependencies()
    {
        foreach (var addon in _addons)
        {
            foreach (var dependency in addon.AddonDependencyIds.Select(Find).OfType<Addon>())
            {
                addon.Enabled!.Require(dependency.Enabled!);
            }
        }
    }

    private static IEnumerable<Addon> DiscoverUnavailable()
    {
        foreach (var mod in ModManager.InstalledMods)
        {
            if (!mod.enabled && mod.requirements.Contains(Plugin.GUID) && _addons.All(addon => addon.Id != mod.id))
            {
                yield return new(mod);
            }
        }
    }

    private static int Rank(Addon addon)
    {
        int index = Array.IndexOf(_builtIn, addon.Id);

        return index < 0 ? int.MaxValue : index;
    }
}
