using AbsoluteFriends.Core;
using AbsoluteFriends.Utils;
using RainMeadow;

namespace AbsoluteFriends.Meadow;

internal class MemoryData : OnlineEntity.EntityData
{
    private int[] _subjects = [];

    private OnlineEntity.EntityId[] _entities = [];

    private bool _hasTracker;

    private OnlineEntity.EntityId? _friend;

    private List<SocialMemory.Relationship>? _received;

    private List<SocialMemory.Relationship>? _translated;

    private bool _isDirty;

    public override EntityDataState MakeState(OnlineEntity onlineEntity, OnlineResource inResource) => new State(onlineEntity);

    internal void Translate(AbstractCreature abstractCreature)
    {
        if (abstractCreature.state?.socialMemory is not { } socialMemory)
        {
            return;
        }

        TranslateMemory(socialMemory);

        if (_hasTracker && abstractCreature.abstractAI?.RealAI?.friendTracker is { } tracker)
        {
            if (_friend is null)
            {
                tracker.friend = null;
            }

            tracker.friendRel = tracker.friend is { } friend
                ? socialMemory.relationShips.FirstOrDefault(relationship => relationship.subjectID == friend.abstractCreature.ID)
                : null;
        }
    }

    private void TranslateMemory(SocialMemory socialMemory)
    {
        if (!ReferenceEquals(socialMemory.relationShips, _translated))
        {
            _received = socialMemory.relationShips;
            _isDirty = true;
        }

        if (!_isDirty || _received == null)
        {
            return;
        }

        _isDirty = false;
        _translated = [.. _received.Select(Translate).OfType<SocialMemory.Relationship>()];
        socialMemory.relationShips = _translated;
    }

    private SocialMemory.Relationship? Translate(SocialMemory.Relationship relationship)
    {
        EntityID subjectID = relationship.subjectID;
        int index = Array.IndexOf(_subjects, subjectID.number);

        if (index >= 0)
        {
            if ((_entities[index].FindEntity(true) as OnlineCreature)?.abstractCreature is not { } subject)
            {
                _isDirty = true;

                return null;
            }

            subjectID = subject.ID;
        }
        else if (subjectID.IsKnownSlugcat)
        {
            return null;
        }

        return new SocialMemory.Relationship(subjectID)
        {
            like = relationship.like,
            fear = relationship.fear,
            know = relationship.know,
            tempLike = relationship.tempLike,
            tempFear = relationship.tempFear,
        };
    }

    internal class State : EntityDataState
    {
        [OnlineField]
        public int[] subjects = [];

        [OnlineField]
        public OnlineEntity.EntityId[] entities = [];

        [OnlineField]
        public bool hasTracker;

        [OnlineField(nullable = true)]
        public OnlineEntity.EntityId? friend;

        public State()
        {
        }

        public State(OnlineEntity onlineEntity)
        {
            if (onlineEntity is OnlineCreature { abstractCreature.abstractAI.RealAI.friendTracker: { } tracker })
            {
                hasTracker = true;
                friend = tracker.friend?.abstractCreature.GetOnlineObject()?.id;
            }

            if (onlineEntity is not OnlineCreature { abstractCreature: { state.socialMemory.relationShips: { Count: > 0 } relationships } abstractCreature })
            {
                return;
            }

            List<int> subjectList = [];
            List<OnlineEntity.EntityId> entityList = [];

            foreach (var abstractSlugcat in (abstractCreature.world?.game).AllPlayers.Concat(FriendUtils.KnownSlugcats).Distinct())
            {
                if (relationships.Any(relationship => relationship.subjectID == abstractSlugcat.ID) && abstractSlugcat.GetOnlineObject() is { } onlineSlugcat)
                {
                    subjectList.Add(abstractSlugcat.ID.number);
                    entityList.Add(onlineSlugcat.id);
                }
            }

            subjects = [.. subjectList];
            entities = [.. entityList];
        }

        public override void ReadTo(OnlineEntity.EntityData data, OnlineEntity onlineEntity)
        {
            if (data is not MemoryData memoryData)
            {
                return;
            }

            memoryData._hasTracker = hasTracker;
            memoryData._friend = friend;

            if (memoryData._subjects.SequenceEqual(subjects) && memoryData._entities.SequenceEqual(entities))
            {
                return;
            }

            memoryData._subjects = [.. subjects];
            memoryData._entities = [.. entities];
            memoryData._isDirty = true;
        }

        public override Type GetDataType() => typeof(MemoryData);
    }
}
