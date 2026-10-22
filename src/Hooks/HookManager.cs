using System.Reflection;
using AbsoluteFriends.Addons;
using AbsoluteFriends.Diagnostics;

namespace AbsoluteFriends.Hooks;

internal static class HookManager
{
    private static readonly List<BaseHooks> _activeHooks = [];

    private static readonly HashSet<Assembly> _assemblies = [];

    private static bool _isInit;

    private static bool _isBinding;

    internal static IEnumerable<HookBinding> Bindings => _activeHooks.SelectMany(hook => hook.Bindings);

    internal static void Register(Assembly assembly)
    {
        if (!_assemblies.Add(assembly))
        {
            return;
        }

        try
        {
            IEnumerable<Type> hookTypes = LoadableTypes(assembly).Where(
                type => type.IsClass && !type.IsAbstract && type.IsSubclassOf(typeof(BaseHooks))
            );

            foreach (var type in hookTypes)
            {
                if (Activator.CreateInstance(type) is BaseHooks hook)
                {
                    _activeHooks.Add(hook);
                }
            }
        }
        catch (Exception exception)
        {
            Reporter.LogError($"{assembly.GetName().Name}: Hooks could not be collected", exception);
        }

        if (_isInit && !_isBinding)
        {
            OnEnable();
        }
    }

    internal static void Unregister(Assembly assembly)
    {
        if (!_assemblies.Remove(assembly))
        {
            return;
        }

        foreach (var hook in _activeHooks.Where(hook => hook.GetType().Assembly == assembly).ToArray())
        {
            hook.Disable();

            _activeHooks.Remove(hook);
        }
    }

    internal static bool IsFailed(Configurable<bool>? option) => option != null && _activeHooks.Any(hook => hook.HasFailed && hook.Owns(option));

    internal static bool IsWarned(Configurable<bool>? option) => option != null && _activeHooks.Any(hook => hook.HasWarned && hook.Owns(option));

    internal static void Initialize()
    {
        if (_isInit)
        {
            return;
        }

        _isInit = true;

        try
        {
            On.OptionInterface._SaveConfigFile -= On_OptionInterface__SaveConfigFile;
            On.OptionInterface._SaveConfigFile += On_OptionInterface__SaveConfigFile;

            HookNotifier.Start();

            Register(Assembly.GetExecutingAssembly());
        }
        catch (Exception exception)
        {
            Reporter.LogError("Hook Manager initialization failed", exception);
        }
    }

    internal static void OnEnable()
    {
        if (_isBinding)
        {
            return;
        }

        _isBinding = true;

        try
        {
            Reporter.BeginSession();

            Initialize();

            AddonRegistry.UpdateRunningStates();

            foreach (var hook in _activeHooks)
            {
                hook.ClearWarnings();
            }

            foreach (var hook in _activeHooks.ToArray())
            {
                hook.Enable();
            }

            foreach (var binding in Bindings)
            {
                binding.Report();
            }

            Reporter.EndSession();
        }
        finally
        {
            _isBinding = false;
        }
    }

    internal static void Refresh()
    {
        if (_isBinding)
        {
            return;
        }

        _isBinding = true;

        try
        {
            Initialize();

            AddonRegistry.UpdateRunningStates();

            foreach (var hook in _activeHooks.ToArray())
            {
                if (hook.IsEnabled == hook.IsApplied)
                {
                    continue;
                }

                if (hook.IsEnabled)
                {
                    hook.Enable();
                }
                else
                {
                    hook.Disable();

                    Reporter.LogInfo($"{hook.Name}: Hooks removed");
                }
            }

            foreach (var binding in Bindings)
            {
                binding.Report();
            }
        }
        finally
        {
            _isBinding = false;
        }
    }

    internal static void OnDisable()
    {
        _isInit = false;

        On.OptionInterface._SaveConfigFile -= On_OptionInterface__SaveConfigFile;

        HookNotifier.Stop();

        AddonRegistry.Reset();

        foreach (var hooks in _activeHooks)
        {
            hooks.Disable();
        }

        _activeHooks.Clear();
        _assemblies.Clear();
    }

    internal static void On_OptionInterface__SaveConfigFile(On.OptionInterface.orig__SaveConfigFile orig, OptionInterface self)
    {
        orig(self);

        if (self != Options.RemixMenu.Instance)
        {
            return;
        }

        Options.RemixMenu.Instance.KeepSelectedProfile();
        OnEnable();

        Reporter.LogInfo("Configurations saved");
    }

    private static IEnumerable<Type> LoadableTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException exception)
        {
            return exception.Types.Where(type => type != null);
        }
    }
}
