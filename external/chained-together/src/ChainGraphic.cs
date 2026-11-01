using RWCustom;
using UnityEngine;

namespace AbsoluteFriends.ChainedTogether;

internal class ChainGraphic(Chain chain)
{
    private const float Tint = 0.2f;

    private const float StrainJitter = 1.5f;

    public virtual string Container => "Foreground";

    public virtual SoundID Sound => SoundID.SS_AI_Give_The_Mark_Boom;

    protected Chain Chain => chain;

    protected Vector2[] Points { get; } = new Vector2[chain.LastIndex + 1];

    public virtual void InitiateSprites(FSprite[] sprites, RoomCamera rCam) => sprites[0] = TriangleMesh.MakeLongMeshAtlased(Chain.LastIndex + 1, false, true);

    public virtual void DrawSprites(FSprite[] sprites, float timeStacker, Vector2 camPos)
    {
        TriangleMesh mesh = (TriangleMesh)sprites[0];
        int last = Chain.LastIndex;

        MoveMesh(mesh, timeStacker, camPos, 0.5f);
        ColorMesh(mesh, last, i =>
        {
            Color color = Color.Lerp(Color.white, Chain.ColorAt((float)i / last), Tint);

            color.a = Chain.Visibility * Chain.EndFadeOf(i, last);

            return color;
        });
    }

    protected static void ColorMesh(TriangleMesh mesh, int last, Func<int, Color> colorAt)
    {
        Color previous = colorAt(0);

        for (int i = 0; i <= last; ++i)
        {
            Color current = colorAt(i);

            mesh.verticeColors[i * 4] = previous;
            mesh.verticeColors[i * 4 + 1] = previous;
            mesh.verticeColors[i * 4 + 2] = current;
            mesh.verticeColors[i * 4 + 3] = current;

            previous = current;
        }
    }

    protected static void SetLengthUV(TriangleMesh mesh, int last)
    {
        for (int i = 0; i <= last; ++i)
        {
            float start = (float)i / (last + 2);
            float end = (float)(i + 1) / (last + 2);

            mesh.UVvertices[i * 4] = new Vector2(start, 0f);
            mesh.UVvertices[i * 4 + 1] = new Vector2(start, 1f);
            mesh.UVvertices[i * 4 + 2] = new Vector2(end, 0f);
            mesh.UVvertices[i * 4 + 3] = new Vector2(end, 1f);
        }
    }

    protected void MoveMesh(TriangleMesh mesh, float timeStacker, Vector2 camPos, float width)
    {
        int last = Chain.LastIndex;
        float jitter = Chain.Strain * StrainJitter;

        for (int i = 0; i <= last; ++i)
        {
            Points[i] = Chain.PositionAt(i, timeStacker);

            if (i > 0 && i < last && jitter > 0f)
            {
                Points[i] += Custom.RNV() * (UnityEngine.Random.value * jitter);
            }
        }

        Vector2 previous = Points[0];

        for (int i = 0; i <= last; ++i)
        {
            Vector2 current = Points[i];
            Vector2 along = (previous - current).normalized;
            Vector2 normal = new Vector2(-along.y, along.x) * width;

            mesh.MoveVertice(i * 4, previous - normal - camPos);
            mesh.MoveVertice(i * 4 + 1, previous + normal - camPos);
            mesh.MoveVertice(i * 4 + 2, current - normal - camPos);
            mesh.MoveVertice(i * 4 + 3, current + normal - camPos);

            previous = current;
        }
    }
}
