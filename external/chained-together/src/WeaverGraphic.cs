using AbsoluteFriends.Utils;
using UnityEngine;
using Watcher;

namespace AbsoluteFriends.ChainedTogether;

internal class WeaverGraphic(Chain chain) : ChainGraphic(chain)
{
    private const float Width = 1.5f;

    private readonly float _randomOffset = UnityEngine.Random.value;

    public override string Container => "GrabShaders";

    public override SoundID Sound => WatcherEnums.WatcherSoundID.Void_Weaver_Jangle;

    public override void InitiateSprites(FSprite[] sprites, RoomCamera rCam)
    {
        base.InitiateSprites(sprites, rCam);

        sprites[0].shader = RainWorldUtils.Shader("WeaverThreadBoth") ?? FShader.defaultShader;

        SetLengthUV((TriangleMesh)sprites[0], Chain.LastIndex);
    }

    public override void DrawSprites(FSprite[] sprites, float timeStacker, Vector2 camPos)
    {
        TriangleMesh mesh = (TriangleMesh)sprites[0];
        Color parameters = new(Chain.Visibility, _randomOffset, 0f, 0f);

        MoveMesh(mesh, timeStacker, camPos, Width);
        ColorMesh(mesh, Chain.LastIndex, _ => parameters);
    }
}
