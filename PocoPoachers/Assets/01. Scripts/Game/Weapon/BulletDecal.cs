using System;
using UnityEngine;

public class BulletDecal : MonoBehaviour
{
    private float _lifeTimer;
    private Action _onRelease;

    public bool IsSpawned { get; private set; }

    public void Place(Vector3 position, Quaternion rotation, Transform parent, float size, float lifetime, Action onRelease)
    {
        transform.SetParent(parent, true);
        transform.SetPositionAndRotation(position, rotation);
        transform.localScale = Vector3.one * size;

        _lifeTimer = lifetime;
        _onRelease = onRelease;
        IsSpawned = true;
    }

    private void Update()
    {
        if (!IsSpawned) return;

        _lifeTimer -= Time.deltaTime;
        if (_lifeTimer <= 0f)
            Release();
    }

    public void Release()
    {
        if (!IsSpawned) return;

        IsSpawned = false;
        transform.SetParent(null, true);
        _onRelease?.Invoke();
        _onRelease = null;
    }
}
