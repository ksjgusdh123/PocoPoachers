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

    public Sprite LoadSprite(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return null;
        if (_cache.TryGetValue(path, out Object cached)) return cached as Sprite;

        var sprite = Resources.Load<Sprite>(path);
        if (sprite != null)
        {
            _cache[path] = sprite;
            return sprite;
        }

        var tex = Resources.Load<Texture2D>(path);
        if (tex == null) return null;

        sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
        _cache[path] = sprite;
        return sprite;
    }

    public T Spawn<T>(string path, Transform parent = null) where T : Component
    {
        var prefab = Load<GameObject>(path);
        if (prefab == null) return null;
        var go = Object.Instantiate(prefab, parent);
        go.transform.localPosition = Vector3.zero;
        return go.GetComponent<T>();
    }

    // 씬 전환 시 캐시 비우기
    public void Clear()
    {
        _cache.Clear();
    }
}
