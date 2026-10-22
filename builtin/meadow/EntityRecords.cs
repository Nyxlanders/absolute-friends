using System.Runtime.CompilerServices;
using RainMeadow;

namespace AbsoluteFriends.Meadow;

internal static class EntityRecords
{
    private static readonly ConditionalWeakTable<AbstractPhysicalObject, StrongBox<EntityID>> _origins = new();

    private static readonly Dictionary<string, WeakReference<AbstractPhysicalObject>> _localEntities = [];

    private static readonly Dictionary<string, AbstractCreature> _avatars = [];

    private static ConditionalWeakTable<AbstractPhysicalObject, Entity> _entities = new();

    internal static void Clear()
    {
        _entities = new();
        _localEntities.Clear();
        _avatars.Clear();
    }

    internal static void Remember(AbstractPhysicalObject abstractPhysicalObject, OnlinePhysicalObject onlineObject)
    {
        Entity entity = _entities.GetValue(abstractPhysicalObject, _ => new Entity());

        if (entity.Owner == onlineObject.owner && entity.Id != null)
        {
            return;
        }

        entity.Owner = onlineObject.owner;
        entity.Id = onlineObject.id.ToString();
        entity.IsAvatar |= onlineObject is OnlineCreature { isAvatar: true };

        if (onlineObject.isMine)
        {
            _localEntities[entity.Id] = new WeakReference<AbstractPhysicalObject>(abstractPhysicalObject);
        }
    }

    internal static void RememberAvatar(AbstractCreature abstractCreature, OnlinePlayer owner, string id)
    {
        _avatars[id] = abstractCreature;

        Entity entity = _entities.GetValue(abstractCreature, _ => new Entity());

        entity.Owner = owner;
        entity.Id = id;
        entity.IsAvatar = true;
    }

    internal static bool TryGetOwner(AbstractPhysicalObject abstractPhysicalObject, out OnlinePlayer owner, out string id)
    {
        if (_entities.TryGetValue(abstractPhysicalObject, out Entity entity) && entity is { Owner: { } entityOwner, Id: { } entityId })
        {
            owner = entityOwner;
            id = entityId;

            return true;
        }

        owner = null!;
        id = "";

        return false;
    }

    internal static bool TryGetLostAvatar(AbstractPhysicalObject abstractPhysicalObject, out OnlinePlayer owner, out string id)
    {
        if (
            abstractPhysicalObject.GetOnlineObject() == null
            && _entities.TryGetValue(abstractPhysicalObject, out Entity entity)
            && entity is { IsAvatar: true, Owner: { } entityOwner, Id: { } entityId }
        )
        {
            owner = entityOwner;
            id = entityId;

            return true;
        }

        owner = null!;
        id = "";

        return false;
    }

    internal static bool IsRemote(AbstractPhysicalObject abstractPhysicalObject) => TryGetOwner(abstractPhysicalObject, out OnlinePlayer owner, out _) && !owner.isMe;

    internal static AbstractPhysicalObject? FindLocal(string id)
    {
        return _localEntities.TryGetValue(id, out WeakReference<AbstractPhysicalObject> reference) && reference.TryGetTarget(out AbstractPhysicalObject abstractPhysicalObject)
            ? abstractPhysicalObject
            : null;
    }

    internal static AbstractCreature? FindLostAvatar(string id, OnlinePlayer owner, RainWorldGame game)
    {
        if (!_avatars.TryGetValue(id, out AbstractCreature abstractCreature))
        {
            return null;
        }

        if (abstractCreature.world?.game != game)
        {
            _avatars.Remove(id);

            return null;
        }

        if (OnlineManager.lobby?.clientSettings.TryGetValue(owner, out ClientSettings settings) != true || !settings.TryGetData(out StoryClientSettingsData data) || !data.isDead)
        {
            return null;
        }

        if (abstractCreature.state is { alive: true } state)
        {
            state.alive = false;
        }

        return abstractCreature;
    }

    internal static void RecordOrigin(AbstractCreature abstractCreature) => _origins.GetValue(abstractCreature, creature => new(creature.ID));

    internal static EntityID? OriginId(AbstractCreature abstractCreature) => _origins.TryGetValue(abstractCreature, out StrongBox<EntityID> origin) ? origin.Value : null;

    internal static EntityID OriginOf(AbstractCreature abstractCreature) => OriginId(abstractCreature) ?? abstractCreature.ID;

    private class Entity
    {
        public OnlinePlayer? Owner;

        public string? Id;

        public bool IsAvatar;
    }
}
