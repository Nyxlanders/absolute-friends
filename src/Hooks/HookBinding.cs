using System.Reflection;
using AbsoluteFriends.Diagnostics;

namespace AbsoluteFriends.Hooks;

internal class HookBinding(BaseHooks owner, string methodName, EventInfo eventInfo, Delegate handler, HookTestAttribute? test)
{
    public readonly string MethodName = methodName;

    public readonly Delegate Handler = handler;

    public bool Failed;

    public bool Warned;

    public bool Ran;

    private bool _isReported;

    private bool _isPassedThrough;

    private string? _reportedSignature;

    private Delegate? _applied;

    private Delegate? _guard;

    public string HookName => owner.Name;

    public string FullName => $"{HookName}.{MethodName}";

    public int[] Indices => test?.Indices ?? [];

    public string[] Anchors => test?.Anchors ?? [];

    public bool IsApplied => _applied != null;

    public ILPatchResult? Patch { get; private set; }

    private bool IsShared => HookManager.Bindings.Count(binding => binding.IsApplied && binding.Shares(eventInfo)) > 1;

    public void Apply()
    {
        Remove();

        Failed = false;
        Ran = false;
        _guard ??= HookGuard.Wrap(this, eventInfo.EventHandlerType);
        _applied = _guard ?? Handler;

        eventInfo.AddEventHandler(null, _applied);
    }

    public void Remove()
    {
        ClearReport();

        if (_applied == null)
        {
            return;
        }

        eventInfo.RemoveEventHandler(null, _applied);

        _applied = null;
    }

    public void ClearReport()
    {
        Patch = null;
        _isReported = false;
        _reportedSignature = null;
    }

    public void MarkVerified(ILPatchResult patch)
    {
        Patch = patch;

        if (_isReported)
        {
            Report();
        }
    }

    public void Report()
    {
        _isReported = true;

        if (Patch is not { } patch || patch.Signature == _reportedSignature)
        {
            return;
        }

        _reportedSignature = patch.Signature;

        patch.Report(IsShared);

        if (!patch.IsHealthy)
        {
            MarkWarned();
        }
    }

    public bool Shares(EventInfo other) => eventInfo.DeclaringType == other.DeclaringType && eventInfo.Name == other.Name;

    public void MarkFailed(Exception exception)
    {
        owner.MarkFailed();

        Reporter.LogError($"{FullName} ({eventInfo.DeclaringType?.FullName}.{eventInfo.Name}) threw {exception.GetType().Name}", exception);

        HookNotifier.Queue(owner.Title);
    }

    public void MarkWarned()
    {
        Warned = true;

        owner.MarkWarned();
    }

    public bool HandleError(Exception exception)
    {
        if (CameFromOriginal(exception))
        {
            if (!_isPassedThrough)
            {
                _isPassedThrough = true;

                Reporter.LogInfo($"{FullName}: {exception.GetType().Name} was thrown inside {eventInfo.DeclaringType?.Name}.{eventInfo.Name} itself and was left to the game");
            }

            return false;
        }

        MarkFailed(exception);

        return true;
    }

    private bool CameFromOriginal(Exception exception)
    {
        try
        {
            return exception.StackTrace is { } trace
                && (trace.Contains($"::{eventInfo.Name}>") || trace.Contains($"::.{eventInfo.Name}>"))
                && trace.Contains(eventInfo.DeclaringType?.Name ?? "");
        }
        catch
        {
            return false;
        }
    }
}
