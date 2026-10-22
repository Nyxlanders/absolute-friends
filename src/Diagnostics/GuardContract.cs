using System.Reflection;

namespace AbsoluteFriends.Diagnostics;

internal static class GuardContexts
{
    public static object[] Slots = [];

    private static readonly object _lock = new();

    public static int Register(object context)
    {
        lock (_lock)
        {
            object[] slots = Slots;
            object[] grown = new object[slots.Length + 1];

            Array.Copy(slots, grown, slots.Length);

            grown[slots.Length] = context;
            Slots = grown;

            return slots.Length;
        }
    }
}

internal class GuardContract
{
    public readonly Type ContextType;

    public readonly FieldInfo FailedField;

    public readonly FieldInfo HandlerField;

    public readonly MethodInfo HandleErrorMethod;

    public readonly FieldInfo RanField;

    public GuardContract(Type contextType, string failedFieldName, string handlerFieldName, string handleErrorMethodName, string ranFieldName)
    {
        ContextType = contextType;
        FailedField = contextType.GetField(failedFieldName) ?? throw new ArgumentException($"{contextType.Name} has no public field {failedFieldName}");
        HandlerField = contextType.GetField(handlerFieldName) ?? throw new ArgumentException($"{contextType.Name} has no public field {handlerFieldName}");
        HandleErrorMethod = contextType.GetMethod(handleErrorMethodName) ?? throw new ArgumentException($"{contextType.Name} has no public method {handleErrorMethodName}");
        RanField = contextType.GetField(ranFieldName) ?? throw new ArgumentException($"{contextType.Name} has no public field {ranFieldName}");

        if (RanField.FieldType != typeof(bool))
        {
            throw new ArgumentException($"{ranFieldName} must be a bool");
        }

        if (FailedField.FieldType != typeof(bool))
        {
            throw new ArgumentException($"{failedFieldName} must be a bool");
        }

        if (!typeof(Delegate).IsAssignableFrom(HandlerField.FieldType))
        {
            throw new ArgumentException($"{handlerFieldName} must be a delegate");
        }

        if (HandleErrorMethod.ReturnType != typeof(bool))
        {
            throw new ArgumentException($"{handleErrorMethodName} must return whether the hook is to blame");
        }
    }
}

internal interface IEmittedGuard
{
    Delegate Body { get; }

    Delegate? Fallback { get; }

    bool ShouldRun();

    void MarkFailed(Exception exception);
}
