using UnityEngine;

namespace AbsoluteFriends.ChainedTogether;

internal partial class Chain
{
    private const int TautCooldown = 20;

    private const float TautSpeed = 2f;

    private const float MaxTautSpeed = 15f;

    private int _tautTimer;

    private float _lastPathLength;

    private bool _wasTaut;

    private Vector2 SoundPosition => Vector2.Lerp(_first.mainBodyChunk.pos, _second.mainBodyChunk.pos, 0.5f);

    private void UpdateSounds(bool wasActive)
    {
        if (_tautTimer > 0)
        {
            --_tautTimer;
        }

        if (!_isActive)
        {
            _wasTaut = false;

            return;
        }

        float pathLength = _rope!.totalLength;
        float speed = pathLength - _lastPathLength;
        bool isTaut = pathLength > _length;

        if (!wasActive)
        {
            PlaySound(1f);
        }
        else if (isTaut && !_wasTaut && _tautTimer == 0 && speed > TautSpeed)
        {
            _tautTimer = TautCooldown;

            PlaySound(Mathf.InverseLerp(TautSpeed, MaxTautSpeed, speed));
        }

        _wasTaut = isTaut;
        _lastPathLength = pathLength;
    }

    private void PlaySound(float volume)
    {
        volume *= Config.SoundVolume.Value;

        if (volume > 0f)
        {
            room.PlaySound(_graphic.Sound, SoundPosition, volume, 1f);
        }
    }
}
