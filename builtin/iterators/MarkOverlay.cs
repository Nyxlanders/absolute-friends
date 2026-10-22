using AbsoluteFriends.Core;
using AbsoluteFriends.Diagnostics;
using AbsoluteFriends.Options;
using AbsoluteFriends.Utils;
using UnityEngine;

namespace AbsoluteFriends.Iterators;

internal class MarkOverlay(Creature friend) : CosmeticSprite
{
    private const float Height = 30f;

    private const float MarkSize = 5f;

    private static readonly AddonLogger _logger = Reporter.GetLogger(Plugin.Name);

    private float _alpha;

    private float _lastAlpha;

    public static bool Failed { get; private set; }

    public static bool CanBearMark(Creature creature)
    {
        return creature is { dead: false, slatedForDeletetion: false, room: not null }
            && creature.room.game.session is StoryGameSession { saveState.deathPersistentSaveData.theMark: true }
            && creature.IsTracked;
    }

    public static float MarkAlpha(Creature friend)
    {
        float alpha = 0f;

        foreach (var player in friend.room.game.RealizedPlayers)
        {
            if (player.room == friend.room && player.graphicsModule is PlayerGraphics graphics && friend.abstractCreature.IsTrackedFor(player.abstractCreature))
            {
                alpha = Mathf.Max(alpha, graphics.markAlpha);
            }
        }

        return alpha;
    }

    public override void Update(bool eu)
    {
        base.Update(eu);

        try
        {
            if (!Config.Mark.IsActive || friend.room != room || !CanBearMark(friend))
            {
                Destroy();

                return;
            }

            BodyChunk head = friend.bodyChunks[0];

            lastPos = pos;
            pos = head.pos + new Vector2(0f, head.rad + Height);
            _lastAlpha = _alpha;
            _alpha = MarkAlpha(friend);
        }
        catch (Exception exception)
        {
            Fail(exception);
        }
    }

    public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
    {
        sLeaser.sprites =
        [
            new FSprite("Futile_White", true)
            {
                shader = RainWorldUtils.Shader("FlatLight") ?? FShader.defaultShader,
                alpha = 0f
            },
            new FSprite("pixel", true)
            {
                scale = MarkSize,
                alpha = 0f
            }
        ];

        AddToContainer(sLeaser, rCam, rCam.ReturnFContainer("Foreground"));
    }

    public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
    {
        base.DrawSprites(sLeaser, rCam, timeStacker, camPos);

        try
        {
            Vector2 position = Vector2.Lerp(lastPos, pos, timeStacker) - camPos;
            float alpha = Mathf.Lerp(_lastAlpha, _alpha, timeStacker);
            FSprite glow = sLeaser.sprites[0];
            FSprite mark = sLeaser.sprites[1];
            Color color = friend.abstractCreature.ShortcutColor;

            glow.SetPosition(position);
            glow.color = color;
            glow.alpha = 0.2f * alpha;
            glow.scale = 1f + alpha;
            mark.SetPosition(position);
            mark.color = color;
            mark.alpha = alpha;
        }
        catch (Exception exception)
        {
            Fail(exception);
        }
    }

    private void Fail(Exception exception)
    {
        Destroy();

        if (Failed)
        {
            return;
        }

        Failed = true;

        _logger.LogError("The mark overlay threw an exception and was disabled", exception);
    }
}
