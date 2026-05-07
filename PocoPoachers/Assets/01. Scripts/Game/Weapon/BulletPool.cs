using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class BulletPool : Singleton<BulletPool>
{
    private const int DefaultCapacity = 20;
    private const int MaxSize = 100;

    [SerializeField] private GameObject _networkBulletPrefab;
    public GameObject NetworkBulletPrefab => _networkBulletPrefab;

    private readonly Dictionary<GameObject, ObjectPool<Bullet>> _pools = new();

    public Bullet Get(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        var pool = GetOrCreatePool(prefab);
        Bullet bullet = pool.Get();
        bullet.transform.SetPositionAndRotation(position, rotation);
        return bullet;
    }

    public void Release(GameObject prefab, Bullet bullet)
    {
        if (!_pools.TryGetValue(prefab, out var pool)) return;
        pool.Release(bullet);
    }

    private ObjectPool<Bullet> GetOrCreatePool(GameObject prefab)
    {
        if (_pools.TryGetValue(prefab, out var pool)) return pool;

        ObjectPool<Bullet> newPool = new ObjectPool<Bullet>(
            createFunc: () =>
            {
                Bullet bullet = Instantiate(prefab).GetComponent<Bullet>();
                return bullet;
            },
            actionOnGet: bullet => bullet.gameObject.SetActive(true),
            actionOnRelease: bullet => bullet.gameObject.SetActive(false),
            actionOnDestroy: bullet => Destroy(bullet.gameObject),
            defaultCapacity: DefaultCapacity,
            maxSize: MaxSize
        );

        _pools.Add(prefab, newPool);
        return newPool;
    }
}
