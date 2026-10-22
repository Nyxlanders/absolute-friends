namespace AbsoluteFriends.Manager;

public static class FriendManager
{
    private static readonly List<TabRegistration> _tabs = [];

    public static bool IsReadOnly => FriendSaveData.IsReadOnly;

    internal static IReadOnlyList<TabRegistration> Tabs => _tabs;

    public static string ExportHostState(RainWorldGame game) => FriendSaveData.Export(game);

    public static void ApplyHostState(string? state) => FriendSaveData.ApplyHost(state);

    public static void RegisterHostData(string key, Func<RainWorldGame, string?> export) => FriendSaveData.RegisterHostData(key, export);

    public static void UnregisterHostData(string key) => FriendSaveData.UnregisterHostData(key);

    public static string? GetHostData(string key) => FriendSaveData.GetHostData(key);

    public static void RegisterSharedIdRule(Func<AbstractCreature, string?> rule) => FriendSaveData.RegisterSharedIdRule(rule);

    public static void UnregisterSharedIdRule(Func<AbstractCreature, string?> rule) => FriendSaveData.UnregisterSharedIdRule(rule);

    public static void RegisterOriginIdRule(Func<AbstractCreature, EntityID?> rule) => FriendSaveData.RegisterOriginIdRule(rule);

    public static void UnregisterOriginIdRule(Func<AbstractCreature, EntityID?> rule) => FriendSaveData.UnregisterOriginIdRule(rule);

    public static EntityID? HostIdOf(AbstractCreature abstractCreature) => abstractCreature.HostID;

    public static void RegisterRemoteForget(Action<AbstractCreature> forget) => FriendTracking.RegisterRemoteForget(forget);

    public static void UnregisterRemoteForget(Action<AbstractCreature> forget) => FriendTracking.UnregisterRemoteForget(forget);

    public static void ClearFriendship(AbstractCreature abstractCreature) => FriendTracking.ClearFriendship(abstractCreature);

    public static void RegisterTab(string id, string title, Action<FriendTab> build, Func<AbstractCreature, bool>? isVisible = null)
    {
        if (string.IsNullOrEmpty(id))
        {
            throw new ArgumentException("A tab needs an ID.", nameof(id));
        }

        TabRegistration registration = new(id, title ?? throw new ArgumentNullException(nameof(title)), build ?? throw new ArgumentNullException(nameof(build)), isVisible);
        int index = _tabs.FindIndex(tab => tab.Id == id);

        if (index >= 0)
        {
            _tabs[index] = registration;
        }
        else
        {
            _tabs.Add(registration);
        }
    }

    public static void UnregisterTab(string id) => _tabs.RemoveAll(tab => tab.Id == id);

    internal class TabRegistration(string id, string title, Action<FriendTab> build, Func<AbstractCreature, bool>? isVisible)
    {
        internal string Id { get; } = id;

        internal string Title { get; } = title;

        internal Action<FriendTab> Build { get; } = build;

        internal bool IsVisibleFor(AbstractCreature creature) => isVisible?.Invoke(creature) ?? true;
    }
}
