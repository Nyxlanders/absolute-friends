using AbsoluteFriends.Core;
using RainMeadow;

namespace AbsoluteFriends.Meadow;

internal class OwnerData : OnlineEntity.EntityData
{
    internal OnlineEntity.EntityId? OwnerId;

    private AbstractCreature? _owner;

    private PhysicalObject? _mirroredObject;

    private AbstractCreature? _mirroredOwner;

    internal AbstractCreature? Owner
    {
        get
        {
            if (_owner == null && OwnerId is { } ownerId)
            {
                _owner = (ownerId.FindEntity(true) as OnlineCreature)?.abstractCreature;
            }

            return _owner;
        }
    }

    public override EntityDataState MakeState(OnlineEntity onlineEntity, OnlineResource inResource) => new State(onlineEntity);

    private void Mirror(OnlineEntity onlineEntity)
    {
        if (onlineEntity is not OnlinePhysicalObject { apo.realizedObject: { } physicalObject })
        {
            return;
        }

        AbstractCreature? owner = Owner;

        if (physicalObject == _mirroredObject && owner == _mirroredOwner)
        {
            return;
        }

        _mirroredObject = physicalObject;
        _mirroredOwner = owner;

        physicalObject.SetOwner();

        if (owner != null)
        {
            physicalObject.SetOwner(owner);
        }
    }

    internal class State : EntityDataState
    {
        [OnlineField(nullable = true)]
        public OnlineEntity.EntityId? owner;

        public State()
        {
        }

        public State(OnlineEntity onlineEntity)
        {
            if (
                onlineEntity is OnlinePhysicalObject { apo.realizedObject: { } physicalObject }
                && physicalObject.AssignedOwner is { } assignedOwner
                && assignedOwner.GetOnlineObject() is { } onlineOwner
            )
            {
                owner = onlineOwner.id;
            }
        }

        public override void ReadTo(OnlineEntity.EntityData data, OnlineEntity onlineEntity)
        {
            if (data is not OwnerData ownerData)
            {
                return;
            }

            if (!Equals(ownerData.OwnerId, owner))
            {
                ownerData.OwnerId = owner;
                ownerData._owner = null;
            }

            ownerData.Mirror(onlineEntity);
        }

        public override Type GetDataType() => typeof(OwnerData);
    }
}
