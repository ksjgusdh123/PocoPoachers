using System;
using System.Collections.Generic;
using UnityEngine;

public class WorldUIManager : Singleton<WorldUIManager>
{
    [Serializable]
    private struct WorldUIEntry
    {
        public WorldUIType type;
        public WorldUIElement prefab;
        public Vector3 offset;
        [Min(0)] public int initialPoolSize;
    }

    [SerializeField] private Canvas _worldCanvas;
    [SerializeField] private WorldUIEntry[] _entries;

    private Dictionary<WorldUIType, Queue<WorldUIElement>> _pools;
    private Dictionary<WorldUIType, WorldUIElement> _prefabMap;
    private Dictionary<WorldUIType, Vector3> _offsetMap;

    protected override void Awake()
    {
        base.Awake();
        InitPools();
    }

    private void InitPools()
    {
        _pools = new Dictionary<WorldUIType, Queue<WorldUIElement>>();
        _prefabMap = new Dictionary<WorldUIType, WorldUIElement>();
        _offsetMap = new Dictionary<WorldUIType, Vector3>();

        foreach (WorldUIEntry entry in _entries)
        {
            _pools[entry.type] = new Queue<WorldUIElement>();
            _prefabMap[entry.type] = entry.prefab;
            _offsetMap[entry.type] = entry.offset;

            for (int i = 0; i < entry.initialPoolSize; i++)
            {
                WorldUIElement element = Instantiate(entry.prefab, _worldCanvas.transform);
                element.gameObject.SetActive(false);
                _pools[entry.type].Enqueue(element);
            }
        }
    }

    public T Create<T>(WorldUIType type, Transform target) where T : WorldUIElement
    {
        return Create<T>(type, target, _offsetMap.TryGetValue(type, out var offset) ? offset : Vector3.zero);
    }

    public T Create<T>(WorldUIType type, Transform target, Vector3 offset) where T : WorldUIElement
    {
        T element = Get<T>(type);
        if (element == null) return null;

        element.Init(target, offset, type);
        element.gameObject.SetActive(true);
        return element;
    }

    public void Return(WorldUIElement element)
    {
        element.gameObject.SetActive(false);
        _pools[element.UIType].Enqueue(element);
    }

    private T Get<T>(WorldUIType type) where T : WorldUIElement
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
