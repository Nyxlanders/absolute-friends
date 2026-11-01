using System.Runtime.CompilerServices;
using AbsoluteFriends.Utils;

namespace AbsoluteFriends.ChainedTogether;

internal static class ScriptedPlayers
{
    private const string Key = Plugin.GUID + ".scripted";

    private static readonly ConditionalWeakTable<AbstractPhysicalObject, StrongBox<bool>> _remoteScripted = new();

    internal static void Register() => AuthorityUtils.RegisterSyncedValue(Key, Read, Write);

    internal static void Unregister() => AuthorityUtils.UnregisterSyncedValue(Key);

    private static int? Read(AbstractPhysicalObject abstractPhysicalObject) => abstractPhysicalObject.realizedObject is Player player
        ? player.controller != null ? 1 : 0
        : null;

    private static void Write(AbstractPhysicalObject abstractPhysicalObject, int isScripted) => _remoteScripted.GetOrCreateValue(abstractPhysicalObject).Value = isScripted != 0;

    extension(Creature creature)
    {
        internal bool IsScripted => creature is Player player && (player.HasAuthority
            ? player.controller != null
            : _remoteScripted.TryGetValue(player.abstractCreature, out var isScripted) && isScripted.Value);
    }
}
