using System;
using System.Collections.Generic;
using UnityEngine;

public class WorldUIManager : Singleton<WorldUIManager>
{
    [Serializable]
    private struct WorldUIEntry
    {
        public WorldUIType type;
        public WorldUIBase prefab;
        public Vector3 offset;
        [Min(0)] public int initialPoolSize;
    }

    [SerializeField] private Canvas _worldCanvas;
    [SerializeField] private WorldUIEntry[] _entries;

    private Dictionary<WorldUIType, Queue<WorldUIBase>> _pools;
    private Dictionary<WorldUIType, WorldUIBase> _prefabMap;
    private Dictionary<WorldUIType, Vector3> _offsetMap;

    protected override void Awake()
    {
        base.Awake();
        InitPools();
    }

    private void InitPools()
    {
        _pools = new Dictionary<WorldUIType, Queue<WorldUIBase>>();
        _prefabMap = new Dictionary<WorldUIType, WorldUIBase>();
        _offsetMap = new Dictionary<WorldUIType, Vector3>();

        foreach (WorldUIEntry entry in _entries)
        {
            _pools[entry.type] = new Queue<WorldUIBase>();
            _prefabMap[entry.type] = entry.prefab;
            _offsetMap[entry.type] = entry.offset;

            for (int i = 0; i < entry.initialPoolSize; i++)
            {
                WorldUIBase element = Instantiate(entry.prefab, _worldCanvas.transform);
                element.gameObject.SetActive(false);
                _pools[entry.type].Enqueue(element);
            }
        }
    }

    public T Create<T>(WorldUIType type, Transform target) where T : WorldUIBase
    {
        return Create<T>(type, target, _offsetMap.TryGetValue(type, out var offset) ? offset : Vector3.zero);
    }

    public T Create<T>(WorldUIType type, Transform target, Vector3 offset) where T : WorldUIBase
    {
        T element = Get<T>(type);
        if (element == null) return null;

        element.Init(target, offset, type);
        element.gameObject.SetActive(true);
        return element;
    }

    public void Return(WorldUIBase element)
    {
        if (element == null) return;

        if (!_pools.TryGetValue(element.UIType, out var pool))
        {
            // Init을 거치지 않았거나 등록되지 않은 타입 — 풀에 넣을 수 없으므로 파괴한다.
            Debug.LogWarning($"[WorldUIManager] {element.UIType} 풀이 없어 '{element.name}'을 반환할 수 없습니다.", element);
            Destroy(element.gameObject);
            return;
        }

        // 같은 요소를 두 번 반환하면 풀에 중복으로 들어가 두 곳에서 동시에 쓰이게 된다.
        if (pool.Contains(element)) return;

        element.gameObject.SetActive(false);
        pool.Enqueue(element);
    }

    private T Get<T>(WorldUIType type) where T : WorldUIBase
    {
        if (!_prefabMap.ContainsKey(type))
        {
            Debug.LogWarning($"[WorldUIManager] {type} 프리팹이 등록되지 않았습니다.");
            return null;
        }

        if (_pools[type].Count > 0)
            return _pools[type].Dequeue() as T;

        return Instantiate(_prefabMap[type], _worldCanvas.transform) as T;
    }
}
