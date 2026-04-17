using System.Collections.Generic;
using UnityEngine;

public class ResourceManager : Singleton<ResourceManager>
{
    // 한 번 로드한 리소스는 캐시에서 재사용
    private Dictionary<string, Object> _cache = new Dictionary<string, Object>();

    // 리소스 로드 (텍스쳐, 프리팹, SO 등 범용)
    public T Load<T>(string path) where T : Object
    {
        if (_cache.TryGetValue(path, out Object cached))
            return cached as T;

        T resource = Resources.Load<T>(path);
        if (resource == null)
        {
            Debug.LogWarning($"[ResourceManager] 리소스를 찾을 수 없습니다: {path}");
            return null;
        }

        _cache[path] = resource;
        return resource;
    }

    // 프리팹 인스턴스화
    public GameObject Instantiate(string path, Transform parent = null)
    {
        GameObject prefab = Load<GameObject>(path);
        if (prefab == null) return null;

        return Object.Instantiate(prefab, parent);
    }

    // 씬 전환 시 캐시 비우기
    public void Clear()
    {
        _cache.Clear();
    }
}
