using AbsoluteFriends.Core;
using AbsoluteFriends.Utils;

namespace AbsoluteFriends.Manager;

internal static class FriendForgetting
{
    private static readonly List<Action<AbstractCreature>> _remoteForgets = [];

    internal static void RegisterRemoteForget(Action<AbstractCreature> forget)
    {
        if (forget != null && !_remoteForgets.Contains(forget))
        {
            _remoteForgets.Add(forget);
        }
    }

    internal static void UnregisterRemoteForget(Action<AbstractCreature> forget) => _remoteForgets.Remove(forget);

    internal static void ClearFriendship(AbstractCreature abstractCreature)
    {
        abstractCreature.state?.socialMemory?.relationShips?.RemoveAll(relationship => relationship.subjectID.IsKnownSlugcat);

        if (abstractCreature.abstractAI?.RealAI?.friendTracker is { } tracker && tracker.friend?.abstractCreature is { IsSlugcat: true })
        {
            tracker.friend = null;
            tracker.friendRel = null;
        }
    }

    extension(AbstractCreature abstractCreature)
    {
        internal bool HasFriendTracker => abstractCreature.IsSlugpup || abstractCreature.creatureTemplate is { IsLizard: true } or { IsCicada: true };

        internal bool IsForgettable => abstractCreature is { IsPlayer: false, HasFriendTracker: true }
            && !FriendSaveData.IsReadOnly
            && (abstractCreature.HasAuthority || _remoteForgets.Count > 0);

        internal void Forget()
        {
            if (!abstractCreature.IsForgettable)
            {
                return;
            }

            if (abstractCreature.HasAuthority)
            {
                ClearFriendship(abstractCreature);
            }
            else
            {
                _remoteForgets.ForEach(forget => forget(abstractCreature));
            }

            abstractCreature.SetNickname(null);
            abstractCreature.ClearFriendData();
            abstractCreature.Untrack();
        }
    }
}
