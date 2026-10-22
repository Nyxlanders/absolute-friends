using AbsoluteFriends.Diagnostics;

namespace AbsoluteFriends.Addons;

public class AddonLogger
{
    private readonly string _tag;

    internal AddonLogger(string name) => _tag = $"({name}) ";

    public void LogConsole(string message) => Reporter.LogConsole(_tag + message);

    public void LogInfo(string message) => Reporter.LogInfo(_tag + message);

    public void LogWarning(string message) => Reporter.LogWarning(_tag + message);

    public void LogWarning(string message, Exception exception) => Reporter.LogWarning(_tag + message, exception);

    public void LogError(string message) => Reporter.LogError(_tag + message);

    public void LogError(string message, Exception exception) => Reporter.LogError(_tag + message, exception);
}
