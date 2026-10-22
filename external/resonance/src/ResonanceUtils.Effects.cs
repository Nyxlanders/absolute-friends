using AbsoluteFriends.Options;
using RWCustom;
using UnityEngine;
using Random = UnityEngine.Random;

namespace AbsoluteFriends.Resonance;

internal static partial class ResonanceUtils
{
    private static float AftershockRatio => Config.ResonanceAftershock.IsActive ? Config.ResonanceAftershockRatio.Value : 0f;

    extension(AbstractCreature abstractCreature)
    {
        private void Shake(int duration)
        {
            if (duration < 1)
            {
                return;
            }

            ResonanceTracker tracker = abstractCreature.Tracker;

            tracker.Duration = duration;
            tracker.Reflection = duration;
        }

        private void Reflect(int duration, int cost)
        {
            if (duration < 1)
            {
                return;
            }

            abstractCreature.Shake(duration);
            abstractCreature.Tracker.Aftershock += (int)(cost * AftershockRatio);

            if (abstractCreature.realizedCreature is not { } creature)
            {
                return;
            }

            creature.Stun(duration);

            if (creature is Player slugcat)
            {
                slugcat.Blink(duration);
                slugcat.airInLungs *= 0.1f;
                slugcat.exhausted = true;
                slugcat.aerobicLevel = Mathf.Max(slugcat.aerobicLevel, 1.5f);
            }
        }
    }

    extension(Creature creature)
    {
        public void Vibrate(float progress)
        {
            Vector2 vector = Custom.RNV();

            foreach (var bodyChunk in creature.bodyChunks ?? [])
            {
                vector = Vector3.Slerp(-vector.normalized, Custom.RNV(), Random.value);
                vector *= Mathf.Min(3f, Random.value * 3f / Mathf.Lerp(bodyChunk.mass, 1f, 0.5f)) * progress;
                bodyChunk.pos += vector;
                bodyChunk.vel += vector * 0.5f;
            }

            foreach (var bodyPart in creature.graphicsModule?.bodyParts ?? [])
            {
                vector = Vector3.Slerp(-vector.normalized, Custom.RNV(), Random.value);
                vector *= Random.value * 2f * progress;
                bodyPart.pos += vector;
                bodyPart.vel += vector;

                if (bodyPart is Limb limb)
                {
                    limb.mode = Limb.Mode.Dangle;
                }
            }

            if (creature is Player slugcat)
            {
                slugcat.Blink(5);
            }
        }

        public void Stagger(float severity)
        {
            if (severity <= 0f || !creature.Consious || Random.value >= severity)
            {
                return;
            }

            creature.Stun(Random.Range(20, 80));

            if (creature is Player slugcat)
            {
                slugcat.Blink(100);
                slugcat.exhausted = true;
            }
        }

    }

}
