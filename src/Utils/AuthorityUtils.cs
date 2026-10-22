using AbsoluteFriends.Diagnostics;

namespace AbsoluteFriends.Utils;

public static class AuthorityUtils
{
    private static readonly List<Func<AbstractPhysicalObject, bool>> _remoteRules = [];

    private static readonly List<Func<AbstractPhysicalObject, string?>> _sharedKeyRules = [];

    private static readonly List<Func<AbstractPhysicalObject?, string, string, bool>> _remoteSenders = [];

    private static readonly Dictionary<string, Action<AbstractPhysicalObject?, string>> _remoteHandlers = [];

    private static readonly SortedDictionary<string, (Func<AbstractPhysicalObject, int?> Read, Action<AbstractPhysicalObject, int> Write)> _syncedValues = new(StringComparer.Ordinal);

    public static bool HasRemoteRules => _remoteRules.Count > 0;

    public static void RegisterRemoteRule(Func<AbstractPhysicalObject, bool> rule)
    {
        if (rule != null && !_remoteRules.Contains(rule))
        {
            _remoteRules.Add(rule);
        }
    }

    public static void UnregisterRemoteRule(Func<AbstractPhysicalObject, bool> rule) => _remoteRules.Remove(rule);

    public static void RegisterSharedKeyRule(Func<AbstractPhysicalObject, string?> rule)
    {
        if (rule != null && !_sharedKeyRules.Contains(rule))
        {
            _sharedKeyRules.Add(rule);
        }
    }

    public static void UnregisterSharedKeyRule(Func<AbstractPhysicalObject, string?> rule) => _sharedKeyRules.Remove(rule);

    public static void RegisterRemoteSender(Func<AbstractPhysicalObject?, string, string, bool> sender)
    {
        if (sender != null && !_remoteSenders.Contains(sender))
        {
            _remoteSenders.Add(sender);
        }
    }

    public static void UnregisterRemoteSender(Func<AbstractPhysicalObject?, string, string, bool> sender) => _remoteSenders.Remove(sender);

    public static void RegisterRemoteHandler(string channel, Action<AbstractPhysicalObject?, string> handler)
    {
        if (!string.IsNullOrEmpty(channel) && handler != null)
        {
            _remoteHandlers[channel] = handler;
        }
    }

    public static void UnregisterRemoteHandler(string channel, Action<AbstractPhysicalObject?, string> handler)
    {
        if (_remoteHandlers.TryGetValue(channel, out var registered) && registered == handler)
        {
            _remoteHandlers.Remove(channel);
        }
    }

    public static void RegisterSyncedValue(string key, Func<AbstractPhysicalObject, int?> read, Action<AbstractPhysicalObject, int> write)
    {
        if (!string.IsNullOrEmpty(key) && read != null && write != null)
        {
            _syncedValues[key] = (read, write);
        }
    }

    public static void UnregisterSyncedValue(string key) => _syncedValues.Remove(key);

    public static bool SendToOthers(string channel, string data) => _remoteSenders.Any(sender => sender(null, channel, data));

    public static void ReceiveRemote(AbstractPhysicalObject? abstractPhysicalObject, string channel, string data)
    {
        if (!_remoteHandlers.TryGetValue(channel, out var handler))
        {
            return;
        }

        try
        {
            handler(abstractPhysicalObject, data);
        }
        catch (Exception exception)
        {
            Reporter.LogWarning($"Remote handler {channel} failed", exception);
        }
    }

    extension(AbstractPhysicalObject abstractPhysicalObject)
    {
        public IEnumerable<(string Key, int Value)> SyncedValues
        {
            get
            {
                foreach (var pair in _syncedValues.ToArray())
                {
                    int? value = null;

                    try
                    {
                        value = pair.Value.Read(abstractPhysicalObject);
                    }
                    catch (Exception exception)
                    {
                        Reporter.LogWarning($"Synced value {pair.Key} could not be read", exception);
                    }

                    if (value is { } synced)
                    {
                        yield return (pair.Key, synced);
                    }
                }
            }
        }

        public void ReceiveSyncedValue(string key, int value)
        {
            if (!_syncedValues.TryGetValue(key, out var syncedValue))
            {
                return;
            }

            try
            {
                syncedValue.Write(abstractPhysicalObject, value);
            }
            catch (Exception exception)
            {
                Reporter.LogWarning($"Synced value {key} could not be written", exception);
            }
        }
    }

    extension(AbstractPhysicalObject? abstractPhysicalObject)
    {
        public bool HasAuthority => abstractPhysicalObject == null || !_remoteRules.Any(rule => rule(abstractPhysicalObject));

        public string SharedKey => abstractPhysicalObject == null
            ? ""
            : _sharedKeyRules.Select(rule => rule(abstractPhysicalObject)).FirstOrDefault(key => !string.IsNullOrEmpty(key)) ?? abstractPhysicalObject.ID.ToString();

        public bool SendToController(string channel, string data) => abstractPhysicalObject != null
            && !abstractPhysicalObject.HasAuthority
            && _remoteSenders.Any(sender => sender(abstractPhysicalObject, channel, data));
    }

    extension(PhysicalObject? physicalObject)
    {
        public bool HasAuthority => physicalObject?.abstractPhysicalObject.HasAuthority ?? true;
    }
}
