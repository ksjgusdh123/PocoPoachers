using System.Collections.Generic;
using UnityEngine;

public class DropItemSpawnManager : Singleton<DropItemSpawnManager>
{
    private readonly List<int> _cachedItemIds = new List<int>();

    protected override void Awake()
    {
        base.Awake();
        CacheItemIds();
    }

    private void CacheItemIds()
    {
        var table = ItemTable.Instance;
        if (table == null) return;

        foreach (var item in table.All)
            if (item.Id < 300) _cachedItemIds.Add(item.Id);
    }

    // count개의 랜덤 아이템 ID 반환 (최대 8개)
    public List<int> Roll(int count)
    {
        count = Mathf.Clamp(count, 1, 8);
        var result = new List<int>(count);

        for (int i = 0; i < count; i++)
        {
            if (_cachedItemIds.Count == 0) break;
            result.Add(_cachedItemIds[Random.Range(0, _cachedItemIds.Count)]);
        }

        return result;
    }
}
