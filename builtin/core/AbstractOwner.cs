namespace AbsoluteFriends.Core;

public class AbstractOwner : AbstractCreature
{
    public readonly PhysicalObject PhysicalObject;

    public AbstractOwner(PhysicalObject physicalObject) : base(null, StaticWorld.GetCreatureTemplate(CreatureTemplate.Type.StandardGroundCreature), null, new(), new())
    {
        PhysicalObject = physicalObject;
        creatureTemplate = null;
    }

    public virtual string DisplayName => PhysicalObject.GetType().Name;
}
