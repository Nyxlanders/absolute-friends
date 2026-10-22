using AbsoluteFriends.Hooks;
using MonoMod.Cil;

namespace AbsoluteFriends.Diagnostics;

public static class HookGuard
{
    private static readonly GuardContract _bindingContract = new(
        typeof(HookBinding),
        nameof(HookBinding.Failed),
        nameof(HookBinding.Handler),
        nameof(HookBinding.HandleError),
        nameof(HookBinding.Ran)
    );

    private static HookBinding? Manipulating { get; set; }

    internal static Delegate? Wrap(HookBinding binding, Type handlerType)
    {
        try
        {
            return handlerType == typeof(ILContext.Manipulator)
                ? CreateManipulatorGuard(binding)
                : GuardEmitter.Wrap(handlerType, binding, _bindingContract, $"AbsoluteFriends_Guard_{binding.HookName}_{binding.MethodName}");
        }
        catch (Exception exception)
        {
            Reporter.LogWarning($"{binding.FullName}: could not be guarded, binding it directly instead", exception);

            binding.MarkWarned();

            return null;
        }
    }

    private static ILContext.Manipulator CreateManipulatorGuard(HookBinding binding)
    {
        ILContext.Manipulator manipulator = (ILContext.Manipulator)binding.Handler;

        return il =>
        {
            ILSnapshot snapshot = ILVerifier.Capture(il);
            HookBinding? manipulated = Manipulating;

            Manipulating = binding;

            try
            {
                manipulator(il);
            }
            catch (Exception exception)
            {
                binding.MarkFailed(exception);

                throw;
            }
            finally
            {
                Manipulating = manipulated;
            }

            try
            {
                binding.MarkVerified(ILVerifier.Verify(binding.FullName, snapshot, il, binding.Indices, binding.Anchors));
            }
            catch (Exception exception)
            {
                Reporter.LogWarning($"{binding.FullName}: verification itself failed", exception);
            }
        };
    }

    private static bool ShouldRun(HookBinding? binding)
    {
        if (binding == null)
        {
            return true;
        }

        binding.Ran = true;

        return !binding.Failed;
    }

    private static void MarkFailed(HookBinding? binding, Exception exception)
    {
        if (binding == null)
        {
            Reporter.LogError($"Emitted code threw {exception.GetType().Name} and was disabled", exception);

            return;
        }

        binding.MarkFailed(exception);
    }

    private class EmittedGuard(HookBinding? binding, Delegate body, Delegate? fallback) : IEmittedGuard
    {
        public Delegate Body => body;

        public Delegate? Fallback => fallback;

        public bool ShouldRun() => HookGuard.ShouldRun(binding);

        public void MarkFailed(Exception exception) => HookGuard.MarkFailed(binding, exception);
    }

    extension(ILCursor c)
    {
        public void EmitGuarded<T>(T body, T? fallback = null) where T : Delegate
        {
            HookBinding? binding = Manipulating;
            string name = $"{binding?.HookName ?? "Anonymous"}_{binding?.MethodName ?? "Emitted"}";

            T? guarded = null;

            try
            {
                guarded = GuardEmitter.WrapEmitted(typeof(T), new EmittedGuard(binding, body, fallback), $"AbsoluteFriends_Emit_{name}") as T;
            }
            catch (Exception exception)
            {
                Reporter.LogWarning($"{binding?.FullName ?? "Emitted code"}: could not be guarded, emitting it directly instead", exception);

                binding?.MarkWarned();
            }

            c.EmitDelegate(guarded ?? body);
        }
    }
}
