using System.Collections;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.VFX;

public class DeathVFXPool : Singleton<DeathVFXPool>
{
    [SerializeField] private VisualEffectAsset _deathVfxAsset;
    [SerializeField] private int _defaultCapacity = 8;
    [SerializeField] private int _maxSize = 16;
    [SerializeField] private float _vfxLifetime = 3f;

    private ObjectPool<VisualEffect> _pool;

    public void Spawn(Vector3 position)
    {
        if (_deathVfxAsset == null) return;

        EnsurePool();

        VisualEffect vfx = _pool.Get();
        vfx.transform.SetPositionAndRotation(position, Quaternion.identity);

        vfx.Reinit();
        vfx.SendEvent("OnPlay");
        StartCoroutine(ReleaseAfterDelay(vfx));
    }

    private void EnsurePool()
    {
        if (_pool != null) return;

        _pool = new ObjectPool<VisualEffect>(
            createFunc: CreateVfx,
            actionOnGet: vfx => vfx.gameObject.SetActive(true),
            actionOnRelease: vfx =>
            {
                vfx.Stop();
                vfx.transform.SetParent(transform, true);
                vfx.gameObject.SetActive(false);
            },
            actionOnDestroy: vfx => Destroy(vfx.gameObject),
            defaultCapacity: _defaultCapacity,
            maxSize: _maxSize
        );
    }

    private VisualEffect CreateVfx()
    {
        var go = new GameObject("DeathVFX");
        go.transform.SetParent(transform, false);
        var vfx = go.AddComponent<VisualEffect>();
        vfx.visualEffectAsset = _deathVfxAsset;
        return vfx;
    }

    private IEnumerator ReleaseAfterDelay(VisualEffect vfx)
    {
        yield return new WaitForSeconds(_vfxLifetime);
        if (vfx != null)
            _pool.Release(vfx);
    }
}
