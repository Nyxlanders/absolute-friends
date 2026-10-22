using System.Runtime.CompilerServices;
using AbsoluteFriends.Core;
using AbsoluteFriends.Options;
using AbsoluteFriends.Utils;
using UnityEngine;

namespace AbsoluteFriends.Visualizer;

internal class FriendHudOverlay : HudOverlay
{
    private static readonly ConditionalWeakTable<Room, FriendHudOverlay> _overlays = new();

    private readonly Dictionary<AbstractCreature, FriendMarker> _markers = [];

    private readonly HashSet<AbstractCreature> _sources = [];

    private readonly List<AbstractCreature> _removed = [];

    private readonly OverlayPositions _positions = new();

    private readonly Dictionary<AbstractCreature, bool> _deaths = [];

    private readonly UpdateTimer _timer = new();

    private readonly FriendMeter _meter;

    public FriendHudOverlay(Room room)
    {
        _meter = new(Container);
        _overlays.Remove(room);
        _overlays.Add(room, this);
    }

    private static bool IsEnabled => Config.FriendMarker.IsActive || Config.FriendMeter.IsActive || Config.BumpEffect.IsActive;

    public static bool IsShowing(AbstractPhysicalObject source)
    {
        return Config.FriendMarker.IsActive
            && source is AbstractCreature creature
            && RainWorldUtils.MainCamera(source.world?.game)?.room is { } viewed
            && _overlays.TryGetValue(viewed, out var overlay)
            && !overlay.slatedForDeletetion
            && overlay._markers.TryGetValue(creature, out var marker)
            && marker.IsVisible;
    }

    public override void Update(bool eu)
    {
        base.Update(eu);

        try
        {
            RoomCamera? camera = RainWorldUtils.MainCamera(room?.game);

            if (!IsEnabled || camera == null || camera.room != room)
            {
                Clear();

                return;
            }

            if (_timer.Elapse())
            {
                Refresh();
            }

            UpdateMarkers(camera);

            if (Config.FriendMeter.IsActive)
            {
                _meter.Update(camera.hud, camera.sSize, _sources);
            }
            else
            {
                _meter.Clear();
            }

            UpdateDeaths();
        }
        catch (Exception exception)
        {
            Clear();
            this.Destroy(exception);
        }
    }

    public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
    {
        base.DrawSprites(sLeaser, rCam, timeStacker, camPos);

        if (sLeaser.deleteMeNextFrame || !OverlayUtils.ShouldDraw(rCam, room))
        {
            return;
        }

        Container.isVisible = IsEnabled;

        if (!Container.isVisible)
        {
            return;
        }

        try
        {
            _meter.Draw(timeStacker);

            foreach (var entry in _markers)
            {
                entry.Value.Draw(rCam, _positions.TryGet(entry.Key, rCam, camPos, timeStacker, out OverlayPosition place) ? place : null, timeStacker);
            }
        }
        catch (Exception exception)
        {
            Clear();
            this.Destroy(exception);
        }
    }

    private static bool IsTaggedByMeadow(AbstractCreature creature) => creature.IsPlayer && !creature.HasAuthority && MeadowOptions.IsInLobby && MeadowTags.IsShown;

    private static bool IsHidden(RoomCamera camera)
    {
        RoomCamera.CameraCutsceneType type = camera.cutsceneType;

        return type == RoomCamera.CameraCutsceneType.HideHudAndFollowNoone
            || (
                camera.InCutscene
                && (
                    type == RoomCamera.CameraCutsceneType.VoidSea
                    || type == RoomCamera.CameraCutsceneType.EndingOE
                    || type == RoomCamera.CameraCutsceneType.HunterStart
                    || type == RoomCamera.CameraCutsceneType.HideJollyHud
                    || type == RoomCamera.CameraCutsceneType.HideJollyHudAndArrows
                )
            );
    }

    private void Refresh()
    {
        _sources.Clear();

        foreach (var creature in FriendUtils.TrackedFriendsIncludingPlayers)
        {
            if ((creature.IsPlayer && creature.HasAuthority) || creature.slatedForDeletion || creature.world != room.world || !_sources.Add(creature))
            {
                continue;
            }

            if (!Config.FriendMarker.IsActive)
            {
                continue;
            }

            if (!_markers.TryGetValue(creature, out var marker))
            {
                marker = new(creature);

                AddNodes(marker.Nodes);

                _markers.Add(creature, marker);
            }

            marker.Refresh();
        }
    }

    private void UpdateMarkers(RoomCamera camera)
    {
        bool isActive = Config.FriendMarker.IsActive;
        bool isShown = isActive && !IsHidden(camera);

        _removed.Clear();

        foreach (var entry in _markers)
        {
            AbstractCreature creature = entry.Key;
            FriendMarker marker = entry.Value;
            bool isSource = isActive && _sources.Contains(creature);
            bool positioned = _positions.TryGet(creature, camera, camera.pos, 1f, out OverlayPosition place);

            marker.Update(isShown && isSource && positioned && !IsTaggedByMeadow(creature) ? place : null);

            if (!isSource && !marker.IsVisible)
            {
                _removed.Add(creature);
            }
        }

        foreach (var creature in _removed)
        {
            _markers[creature].Remove();
            _markers.Remove(creature);
        }
    }

    private void UpdateDeaths()
    {
        foreach (var creature in _sources)
        {
            UpdateDeath(creature);
        }

        foreach (var creature in room.game.Players)
        {
            UpdateDeath(creature);
        }
    }

    private void UpdateDeath(AbstractCreature creature)
    {
        bool isDead = creature.IsDead;

        if (!_deaths.TryGetValue(creature, out bool wasDead))
        {
            _deaths[creature] = isDead;

            return;
        }

        if (isDead == wasDead)
        {
            return;
        }

        _deaths[creature] = isDead;

        if (!Config.BumpEffect.IsActive || creature.realizedCreature is not { } realized || realized.room != room)
        {
            return;
        }

        if (!isDead)
        {
            room.AddObject(new RevivalBump(realized));
        }
        else if (!creature.IsPlayer)
        {
            room.AddObject(new DeathBump(realized));
        }
    }

    private void Clear()
    {
        foreach (var marker in _markers.Values)
        {
            marker.Remove();
        }

        _markers.Clear();
        _sources.Clear();
        _deaths.Clear();
        _meter.Clear();
        _timer.Reset();
    }
}
