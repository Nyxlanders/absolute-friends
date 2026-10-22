using MonoMod.RuntimeDetour;
using RainMeadow;

namespace AbsoluteFriends.Meadow;

internal static class AvatarIds
{
    private const uint FirstStableNumber = 1_000_000_000;

    private const uint StableNumberRange = 1_000_000_000;

    private static Hook? _createHook;

    internal static void Register()
    {
        _createHook ??= new Hook(
            typeof(OnlineCreature).GetConstructor([typeof(OnlineCreature.OnlineCreatureDefinition), typeof(OnlineResource), typeof(AbstractCreatureState)]),
            On_OnlineCreature_ctor
        );
    }

    internal static void Unregister()
    {
        _createHook?.Dispose();
        _createHook = null;
    }

    private static void On_OnlineCreature_ctor(
        Action<OnlineCreature, OnlineCreature.OnlineCreatureDefinition, OnlineResource, AbstractCreatureState> orig,
        OnlineCreature self,
        OnlineCreature.OnlineCreatureDefinition definition,
        OnlineResource inResource,
        AbstractCreatureState initialState
    )
    {
        orig(self, definition, inResource, initialState);

        if (!self.isAvatar || self.owner?.id is not { } ownerId || self.abstractCreature is not { } abstractCreature)
        {
            return;
        }

        uint seed = unchecked((uint)ownerId.GetHashCode() * 31u + (uint)self.id.id);
        EntityID stableId = new(-1, (int)(FirstStableNumber + seed % StableNumberRange))
        {
            altSeed = abstractCreature.ID.altSeed
        };

        abstractCreature.ID = stableId;
    }
}
