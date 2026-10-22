using System.Runtime.CompilerServices;
using UnityEngine;

namespace AbsoluteFriends.Visualizer;

internal readonly struct OverlayPosition(Vector2 screen, Vector2 actual, bool isOtherRoom)
{
    public Vector2 Screen { get; } = screen;

    public Vector2 Actual { get; } = actual;

    public bool IsOtherRoom { get; } = isOtherRoom;

    public bool IsClamped => Screen != Actual;

    public bool IsDistant => IsOtherRoom || IsClamped;
}

internal class OverlayPositions
{
    private ConditionalWeakTable<AbstractPhysicalObject, StrongBox<Vector2>> _lastPlaces = new();

    private World? _world;

    public bool TryGet(AbstractPhysicalObject abstractPhysicalObject, RoomCamera camera, Vector2 camPos, float timeStacker, out OverlayPosition position)
    {
        position = default;

        if (abstractPhysicalObject.slatedForDeletion || camera.room?.world is not { } world || camera.room.abstractRoom is not { } viewed || abstractPhysicalObject.world != world)
        {
            return false;
        }

        if (_world != world)
        {
            _world = world;
            _lastPlaces = new();
        }

        bool isOtherRoom = true;

        if (TryGetWorldPosition(abstractPhysicalObject, world, timeStacker, out Vector2 inWorld, out int roomIndex))
        {
            _lastPlaces.GetOrCreateValue(abstractPhysicalObject).Value = inWorld;

            isOtherRoom = roomIndex != viewed.index;
        }
        else if (_lastPlaces.TryGetValue(abstractPhysicalObject, out var last))
        {
            inWorld = last.Value;
        }
        else
        {
            return false;
        }

        Vector2 actual = inWorld - world.RoomToWorldPos(camPos, viewed.index);

        position = new(actual.ClampToScreen(camera), actual, isOtherRoom);

        return true;
    }

    private static bool TryGetWorldPosition(AbstractPhysicalObject abstractPhysicalObject, World world, float timeStacker, out Vector2 position, out int roomIndex)
    {
        if (
            abstractPhysicalObject.realizedObject is { room.abstractRoom: { } current } realizedObject
            && world.GetAbstractRoom(current.index) != null
            && realizedObject.TryGetPosition(timeStacker, out Vector2 inRoom)
        )
        {
            roomIndex = current.index;
            position = world.RoomToWorldPos(inRoom, roomIndex);

            return true;
        }

        if (abstractPhysicalObject.realizedObject?.ShortcutVessel(world.game) is { room: { } shortcutRoom } vessel && world.GetAbstractRoom(shortcutRoom.index) != null)
        {
            roomIndex = shortcutRoom.index;
            position = world.RoomToWorldPos(new Vector2(vessel.pos.x + 0.5f, vessel.pos.y + 0.5f) * 20f, roomIndex);

            return true;
        }

        if (world.GetAbstractRoom(abstractPhysicalObject.pos.room) is { } abstractRoom)
        {
            roomIndex = abstractRoom.index;
            position = world.RoomToWorldPos(abstractPhysicalObject.pos.PositionInRoom(abstractRoom), roomIndex);

            return true;
        }

        position = default;
        roomIndex = -1;

        return false;
    }
}
