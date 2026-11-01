using MoreSlugcats;
using UnityEngine;

namespace AbsoluteFriends.ChainedTogether;

internal class RubiconGraphic(Chain chain) : ChainGraphic(chain)
{
    public override SoundID Sound => MoreSlugcatsEnums.MSCSoundID.Chain_Lock;

    public override void InitiateSprites(FSprite[] sprites, RoomCamera rCam)
    {
        LoadTexture();

        sprites[0] = TriangleMesh.MakeLongMesh(Chain.LastIndex + 1, false, true, "chain");
    }

    public override void DrawSprites(FSprite[] sprites, float timeStacker, Vector2 camPos)
    {
        TriangleMesh mesh = (TriangleMesh)sprites[0];
        int last = Chain.LastIndex;
        float travelled = 0f;

        MoveMesh(mesh, timeStacker, camPos, 4f);

        for (int i = 0; i <= last; ++i)
        {
            float start = travelled / 1024f;

            if (i > 0)
            {
                travelled += Vector2.Distance(Points[i - 1], Points[i]);
            }

            float end = travelled / 1024f;

            mesh.UVvertices[i * 4] = new Vector2(0f, start);
            mesh.UVvertices[i * 4 + 1] = new Vector2(1f, start);
            mesh.UVvertices[i * 4 + 2] = new Vector2(0f, end);
            mesh.UVvertices[i * 4 + 3] = new Vector2(1f, end);
        }

        Color color = Color.Lerp(new Color(0.01f, 0.01f, 0.01f), RainWorld.SaturatedGold, Chain.Strain);

        ColorMesh(mesh, last, i => new Color(color.r, color.g, color.b, Chain.Visibility * Chain.EndFadeOf(i, last)));
    }

    private static void LoadTexture()
    {
        if (Futile.atlasManager.GetAtlasWithName("chain") != null)
        {
            return;
        }

        Texture2D texture = new(1, 1, TextureFormat.ARGB32, false);
        string path = AssetManager.ResolveFilePath(Path.Combine("Illustrations", "chain.png"));

        AssetManager.SafeWWWLoadTexture(ref texture, "file:///" + path, false, false);
        HeavyTexturesCache.LoadAndCacheAtlasFromTexture("chain", texture, false);
    }
}
