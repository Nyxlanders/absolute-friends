using System.Reflection;
using AbsoluteFriends.Diagnostics;
using AbsoluteFriends.Options;

namespace AbsoluteFriends.Hooks;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public class HookPatchAttribute(Type targetType, string eventName) : Attribute
{
    public Type TargetType { get; } = targetType;

    public string EventName { get; } = eventName;
}

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public class HookTestAttribute(int[] indices, string[] anchors) : Attribute
{
    public int[] Indices { get; } = indices;

    public string[] Anchors { get; } = anchors;
}

public abstract class BaseHooks
{
    private readonly List<HookBinding> _bindings = [];

    private bool _isInit;

    public bool HasFailed { get; private set; }

    public bool HasWarned { get; private set; }

    public virtual bool IsEnabled => IsOptionEnabled && !HasFailed;

    public string Name => field ??= GetType().Name;

    public string Title => Subject ?? Options.FirstOrDefault().Label ?? Name;

    internal IEnumerable<HookBinding> Bindings => _bindings;

    internal bool IsApplied => _bindings.Any(binding => binding.IsApplied);

    protected abstract Configurable<bool>[] Options { get; }

    protected virtual string? Subject => null;

    protected virtual bool IsOptionEnabled => Options.Any(option => option.IsActive);

    internal void MarkWarned() => HasWarned = true;

    internal void ClearWarnings()
    {
        HasWarned = false;

        foreach (var binding in _bindings)
        {
            binding.Warned = false;

            binding.ClearReport();
        }
    }

    internal void MarkFailed()
    {
        HasFailed = true;

        foreach (var binding in _bindings)
        {
            binding.Failed = true;
        }
    }

    internal bool Owns(Configurable<bool>? option) => option != null && Options.Any(owned => ReferenceEquals(owned, option));

    internal void Enable()
    {
        Initialize();
        Disable();

        if (!IsEnabled)
        {
            return;
        }

        int isApplied = 0;

        foreach (var binding in _bindings)
        {
            try
            {
                binding.Apply();

                isApplied++;
            }
            catch (Exception exception)
            {
                try
                {
                    binding.Remove();
                }
                catch
                {
                }

                MarkFailed();

                Reporter.LogError($"{binding.FullName} could not be applied", exception);
                HookNotifier.Queue(Title);

                break;
            }
        }

        Reporter.LogInfo($"{Name}: Hooks applied ({isApplied}/{_bindings.Count})");
    }

    internal void Disable()
    {
        foreach (var binding in _bindings)
        {
            try
            {
                binding.Remove();
            }
            catch (Exception exception)
            {
                Reporter.LogWarning($"{binding.FullName}: Unhook failed", exception);
            }
        }
    }

    private void Initialize()
    {
        if (_isInit)
        {
            return;
        }

        _isInit = true;

        try
        {
            Type type = GetType();

            foreach (var methodInfo in type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
            {
                Bind(methodInfo);
            }
        }
        catch (Exception exception)
        {
            MarkFailed();
            Disable();

            Reporter.LogError($"{Name}: Hooks failed", exception);
            HookNotifier.Queue(Title);
        }
    }

    private void Bind(MethodInfo method)
    {
        if (method.GetCustomAttribute<HookPatchAttribute>() is not { } patch)
        {
            return;
        }

        try
        {
            EventInfo eventInfo = patch.TargetType?.GetEvent(patch.EventName, BindingFlags.Static | BindingFlags.Public)
                ?? throw new InvalidOperationException($"Failed to bind event {patch.EventName} on {patch.TargetType?.Name}");
            Delegate handler = Delegate.CreateDelegate(eventInfo.EventHandlerType, method.IsStatic ? null : this, method);

            _bindings.Add(new(this, method.Name, eventInfo, handler, method.GetCustomAttribute<HookTestAttribute>()));
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException($"{patch.EventName} failed", exception);
        }
    }
}

public abstract class DownpourHooks : BaseHooks
{
    public override bool IsEnabled => base.IsEnabled && ModManager.MSC;
}

public abstract class WatcherHooks : BaseHooks
{
    public override bool IsEnabled => base.IsEnabled && ModManager.Watcher;
}
