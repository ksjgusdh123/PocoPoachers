using System.Collections;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.VFX;

public class BloodVFXPool : Singleton<BloodVFXPool>
{
    private const float SurfaceOffset = 0.01f;

    [SerializeField] private VisualEffectAsset _bloodVfxAsset;
    [SerializeField] private int  _defaultCapacity = 16;
    [SerializeField] private int  _maxSize         = 32;
    [SerializeField] private float _vfxLifetime    = 1f;

    private ObjectPool<VisualEffect> _pool;

    public void Spawn(RaycastHit hit)
    {
        if (hit.collider == null) return;
        if (_bloodVfxAsset == null) return;

        EnsurePool();

        Vector3    position = hit.point + hit.normal * SurfaceOffset;
        Quaternion rotation = Quaternion.FromToRotation(Vector3.up, hit.normal);

        VisualEffect vfx = _pool.Get();
        Transform vfxTransform = vfx.transform;

        // 피격 대상(적)의 자식으로 붙이면 적이 사망/디스폰될 때 풀 오브젝트까지 함께 파괴되어
        // Release되지 못하고 풀이 고갈된다. 풀 루트 아래에 두고 월드 좌표만 맞춘다.
        vfxTransform.SetParent(transform, true);
        vfxTransform.SetPositionAndRotation(position, rotation);
        vfxTransform.localScale = Vector3.one;

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
        var go  = new GameObject("BloodVFX");
        go.transform.SetParent(transform, false);
        var vfx = go.AddComponent<VisualEffect>();
        vfx.visualEffectAsset = _bloodVfxAsset;
        return vfx;
    }

    private IEnumerator ReleaseAfterDelay(VisualEffect vfx)
    {
        yield return new WaitForSeconds(_vfxLifetime);
        if (vfx != null)
            _pool.Release(vfx);
    }
}
