using System;
using UnityEngine;

public static class SoundEvent
{
    public static event Action<Vector3, float, GameObject> OnSoundEmitted;

    public static void Emit(Vector3 position, float range, GameObject source)
    {
        OnSoundEmitted?.Invoke(position, range, source);
    }
}
