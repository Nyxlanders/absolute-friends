using AbsoluteFriends.Core;
using AbsoluteFriends.Utils;

namespace AbsoluteFriends.Manager;

internal static class FriendTracking
{
    private const int ScanInterval = 10;

    private static readonly FriendSaveData.Store<TrackingChoices> _choices = new(save => new(save));

    private static readonly FriendSaveData.Store<HashSet<AbstractCreature>> _metFriends = new(_ => []);

    private static readonly List<Action<AbstractCreature>> _remoteForgets = [];

    private static int _scanTimer;

    internal static void Register() => FriendUtils.RegisterTrackingRule(IsIncluded);

    internal static void Unregister() => FriendUtils.UnregisterTrackingRule(IsIncluded);

    internal static void RegisterRemoteForget(Action<AbstractCreature> forget)
    {
        if (forget != null && !_remoteForgets.Contains(forget))
        {
            _remoteForgets.Add(forget);
        }
    }

    internal static void UnregisterRemoteForget(Action<AbstractCreature> forget) => _remoteForgets.Remove(forget);

    internal static IEnumerable<(string Key, IEnumerable<string> Entries)> Export(RainWorldGame game) => [
        (TrackingChoices.SaveKey, _choices.For(game).Entries),
        (TrackingChoices.DefaultKey, [Config.TrackNewFriends.Value ? TrackingChoices.IncludedValue : TrackingChoices.ExcludedValue]),
    ];

    internal static void MeetNewFriends(RainWorldGame game)
    {
        if (++_scanTimer < ScanInterval)
        {
            return;
        }

        _scanTimer = 0;

        HashSet<AbstractCreature> metFriends = _metFriends.For(game);

        foreach (var abstractCreature in FriendUtils.TrackingCandidates)
        {
            if (abstractCreature.IsPlayer || !metFriends.Add(abstractCreature))
            {
                continue;
            }

            abstractCreature.ApplyDefaultTracking();
        }
    }

    internal static void ClearFriendship(AbstractCreature abstractCreature)
    {
        abstractCreature.state?.socialMemory?.relationShips?.RemoveAll(relationship => relationship.subjectID.IsKnownSlugcat);

        if (abstractCreature.abstractAI?.RealAI?.friendTracker is { } tracker && tracker.friend?.abstractCreature is { IsSlugcat: true })
        {
            tracker.friend = null;
            tracker.friendRel = null;
        }
    }

    private static bool IsIncluded(AbstractCreature abstractCreature) => !abstractCreature.IsExcluded;

    private class TrackingChoices
    {
        internal const string SaveKey = "ABSOLUTEFRIENDS_TRACKING";

        internal const string DefaultKey = "ABSOLUTEFRIENDS_TRACKING_DEFAULT";

        internal const string ExcludedValue = "0";

        internal const string IncludedValue = "1";

        private const string ExcludedSuffix = "=" + ExcludedValue;

        private const string IncludedSuffix = "=" + IncludedValue;

        private const string PlayerPrefix = "P";

        internal readonly HashSet<string> Exclusions = new(StringComparer.Ordinal);

        internal readonly HashSet<string> Inclusions = new(StringComparer.Ordinal);

        private readonly SaveState? _save;

        private readonly bool _excludeUnchosen;

        internal TrackingChoices(SaveState? save)
        {
            _save = save;
            _excludeUnchosen = FriendSaveData.ReadEntries(save, DefaultKey).FirstOrDefault() == ExcludedValue;

            foreach (var entry in FriendSaveData.ReadEntries(save, SaveKey))
            {
                if (entry.StartsWith(PlayerPrefix, StringComparison.Ordinal))
                {
                    continue;
                }

                if (entry.EndsWith(ExcludedSuffix, StringComparison.Ordinal))
                {
                    Choose(entry.Substring(0, entry.Length - ExcludedSuffix.Length), false);
                }
                else if (entry.EndsWith(IncludedSuffix, StringComparison.Ordinal))
                {
                    Choose(entry.Substring(0, entry.Length - IncludedSuffix.Length), true);
                }
            }
        }

        internal IEnumerable<string> Entries => Exclusions
            .Select(key => key + ExcludedSuffix)
            .Concat(Inclusions.Select(key => key + IncludedSuffix))
            .OrderBy(entry => entry, StringComparer.Ordinal);

        internal bool IsExcluded(string key) => Exclusions.Contains(key) || (_excludeUnchosen && !Inclusions.Contains(key));

        internal bool Choose(string key, bool tracked) => tracked
            ? Inclusions.Add(key) | Exclusions.Remove(key)
            : Exclusions.Add(key) | Inclusions.Remove(key);

        internal void Write() => FriendSaveData.WriteEntries(_save, SaveKey, Entries);
    }

    extension(AbstractCreature abstractCreature)
    {
        internal bool IsExcluded => _choices.For(abstractCreature)?.IsExcluded(abstractCreature.SaveDataKey) == true;

        internal bool IsTrackingLocked => !abstractCreature.IsPlayer && FriendSaveData.IsReadOnly;

        internal bool HasFriendTracker => abstractCreature.IsSlugpup || abstractCreature.creatureTemplate is { IsLizard: true } or { IsCicada: true };

        internal bool IsForgettable => abstractCreature is { IsPlayer: false, HasFriendTracker: true }
            && !FriendSaveData.IsReadOnly
            && (abstractCreature.HasAuthority || _remoteForgets.Count > 0);

        internal void SetTracked(bool tracked)
        {
            if (abstractCreature.IsPlayer || FriendSaveData.IsReadOnly || _choices.For(abstractCreature) is not { } choices)
            {
                return;
            }

            if (choices.Choose(abstractCreature.SaveDataKey, tracked))
            {
                choices.Write();
            }
        }

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

            string key = abstractCreature.SaveDataKey;

            if (_choices.For(abstractCreature) is { } choices && (choices.Exclusions.Remove(key) | choices.Inclusions.Remove(key)))
            {
                choices.Write();
            }

            _metFriends.For(abstractCreature)?.Remove(abstractCreature);
            abstractCreature.SetNickname(null);
            abstractCreature.ClearFriendData();
            abstractCreature.Untrack();
        }

        private void ApplyDefaultTracking()
        {
            if (FriendSaveData.IsReadOnly || _choices.For(abstractCreature) is not { } choices)
            {
                return;
            }

            string key = abstractCreature.SaveDataKey;

            if (!choices.Exclusions.Contains(key) && !choices.Inclusions.Contains(key) && choices.Choose(key, Config.TrackNewFriends.Value))
            {
                choices.Write();
            }
        }
    }
}
