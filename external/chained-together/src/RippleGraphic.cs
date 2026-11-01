using AbsoluteFriends.Utils;
using RWCustom;
using UnityEngine;
using Watcher;

namespace AbsoluteFriends.ChainedTogether;

internal class RippleGraphic(Chain chain) : ChainGraphic(chain)
{
    private const float Width = 1.5f;

    public override string Container => "GrabShaders";

    public override SoundID Sound => WatcherEnums.WatcherSoundID.Void_Weaver_Pluck_Thread;

    public override void InitiateSprites(FSprite[] sprites, RoomCamera rCam)
    {
        base.InitiateSprites(sprites, rCam);

        sprites[0].shader = RainWorldUtils.Shader("RippleSpawnCreature") ?? FShader.defaultShader;

        SetLengthUV((TriangleMesh)sprites[0], Chain.LastIndex);
    }

    public override void DrawSprites(FSprite[] sprites, float timeStacker, Vector2 camPos)
    {
        TriangleMesh mesh = (TriangleMesh)sprites[0];
        float darkness = RainWorldUtils.MainCamera(Chain.room.game)?.currentPalette.darkness ?? 0f;
        Color parameters = new(0.7f + 0.3f * Mathf.InverseLerp(0.1f, 0.9f, darkness), Custom.LerpMap(darkness, 0.1f, 0.9f, 0.1f, 0.075f, 0.5f), 0f);

        MoveMesh(mesh, timeStacker, camPos, Width * Chain.Visibility);
        ColorMesh(mesh, Chain.LastIndex, _ => parameters);
    }
}
