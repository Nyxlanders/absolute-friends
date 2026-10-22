using System.Runtime.CompilerServices;
using AbsoluteFriends.Options;
using AbsoluteFriends.Utils;

namespace AbsoluteFriends.Core;

public static class OwnerUtils
{
    private static readonly ConditionalWeakTable<PhysicalObject, AbstractOwner>.CreateValueCallback _createSelfOwner = physicalObject => new(physicalObject);

    private static readonly List<Func<PhysicalObject, bool>> _selfOwnershipRules = [];

    private static readonly List<Func<PhysicalObject, AbstractCreature?>> _remoteOwnerSources = [];

    private static ConditionalWeakTable<UpdatableAndDeletable, AbstractCreature> _owners = new();

    private static ConditionalWeakTable<PhysicalObject, AbstractOwner> _selfOwners = new();

    public static void RegisterSelfOwnershipRule(Func<PhysicalObject, bool> rule)
    {
        if (rule != null && !_selfOwnershipRules.Contains(rule))
        {
            _selfOwnershipRules.Add(rule);
        }
    }

    public static void UnregisterSelfOwnershipRule(Func<PhysicalObject, bool> rule) => _selfOwnershipRules.Remove(rule);

    public static void RegisterRemoteOwnerSource(Func<PhysicalObject, AbstractCreature?> source)
    {
        if (source != null && !_remoteOwnerSources.Contains(source))
        {
            _remoteOwnerSources.Add(source);
        }
    }

    public static void UnregisterRemoteOwnerSource(Func<PhysicalObject, AbstractCreature?> source) => _remoteOwnerSources.Remove(source);

    internal static void ClearOwners()
    {
        _owners = new();
        _selfOwners = new();
    }

    extension(PhysicalObject? physicalObject)
    {
        public Creature? Grabber => physicalObject?.grabbedBy is { Count: > 0 } grasps ? grasps[0]?.grabber : (physicalObject as Player)?.onBack;

        public AbstractOwner? SelfOwner => physicalObject == null ? null : _selfOwners.GetValue(physicalObject, _createSelfOwner);

        private AbstractCreature? DeclaredOwner
        {
            get
            {
                if (physicalObject == null)
                {
                    return null;
                }

                for (int index = 0; index < _selfOwnershipRules.Count; index++)
                {
                    if (_selfOwnershipRules[index](physicalObject))
                    {
                        return physicalObject.SelfOwner;
                    }
                }

                return null;
            }
        }

        public void PropagateOwnerTo(UpdatableAndDeletable? target)
        {
            if (physicalObject.Owner is { } owner)
            {
                target.SetOwner(owner);
            }
        }
    }

    extension(AbstractCreature? owner)
    {
        public UpdatableAndDeletable? RealizedOwner => owner is AbstractOwner abstractOwner ? abstractOwner.PhysicalObject : owner?.realizedCreature;
    }

    extension(Creature? creature)
    {
        public IEnumerable<PhysicalObject> HeldObjects
        {
            get
            {
                foreach (var grasp in creature?.grasps ?? [])
                {
                    if (grasp?.grabbed is { } grabbed)
                    {
                        yield return grabbed;
                    }
                }

                if ((creature as Player)?.slugOnBack?.slugcat is { } slugcat)
                {
                    yield return slugcat;
                }
            }
        }
    }

    extension(UpdatableAndDeletable? source)
    {
        public AbstractCreature? Owner
        {
            get
            {
                if (source == null)
                {
                    return null;
                }

                if (_remoteOwnerSources.Count > 0 && source is PhysicalObject { HasAuthority: false } remoteObject)
                {
                    foreach (var ownerSource in _remoteOwnerSources)
                    {
                        if (ownerSource(remoteObject) is { } remoteOwner)
                        {
                            return remoteOwner;
                        }
                    }
                }

                if (_owners.TryGetValue(source, out AbstractCreature abstractCreature))
                {
                    return abstractCreature;
                }

                if (source is LizardSpit lizardSpit)
                {
                    return lizardSpit.lizard?.abstractCreature;
                }

                if (source is PhysicalObject physicalObject)
                {
                    if (physicalObject.DeclaredOwner is { } declaredOwner)
                    {
                        return declaredOwner;
                    }

                    if (
                        Config.FriendGrabbed.IsActive
                        && physicalObject.Grabber is { abstractCreature: { } abstractGrabber } grabber
                        && (
                            !Config.FriendGrabbedForce.IsActive
                            || source is not Creature
                            || grabber is not Player slugcat
                            || !slugcat.input[0].pckp
                        )
                    )
                    {
                        return abstractGrabber;
                    }
                }

                return null;
            }
        }

        public AbstractCreature? AssignedOwner => source != null && _owners.TryGetValue(source, out AbstractCreature owner) ? owner : null;

        public AbstractCreature? ExternalOwner => source.Owner is { } owner && !ReferenceEquals(owner.RealizedOwner, source) ? owner : null;

        public void SetOwner(AbstractCreature? target = null)
        {
            if (source == null)
            {
                return;
            }

            if (target == null)
            {
                _owners.Remove(source);
            }
            else if (!_owners.TryGetValue(source, out _))
            {
                _owners.Add(source, target);
            }
        }

        public void SetOwner(Creature? target) => source.SetOwner(target?.abstractCreature);
    }

    extension(AbstractPhysicalObject? source)
    {
        public AbstractCreature? Owner => source?.realizedObject.Owner;

        public AbstractCreature? ExternalOwner => source?.realizedObject.ExternalOwner;

        public void SetOwner(AbstractCreature? target = null) => source?.realizedObject.SetOwner(target);

        public void SetOwner(Creature? target) => source.SetOwner(target?.abstractCreature);
    }
}
