using System.Collections.Generic;
using UnityEngine;

// 상자 하나가 뽑을 후보 항목.
// id 범위를 비워두면 type 전체에서, 범위를 채우면 그 구간의 아이템에서 랜덤으로 하나 고른다.
[System.Serializable]
public class ItemBoxRandomEntry
{
    [Tooltip("id 범위를 비워뒀을 때 뽑을 대상 타입")]
    public ItemType type = ItemType.Consumable;

    [Tooltip("뽑을 아이템 id 범위 (item.csv 참고). 둘 다 0이면 위 타입 전체에서 뽑는다")]
    public int minItemId;
    public int maxItemId;

    [Tooltip("추첨 가중치. 다른 항목과의 비율로만 계산되므로 합을 1로 맞출 필요는 없다. 0이면 제외")]
    [Min(0f)] public float weight = 1f;

    [Tooltip("이 항목이 뽑혔을 때 들어갈 개수 범위")]
    [Min(1)] public int minQuantity = 1;
    [Min(1)] public int maxQuantity = 1;

    // 아이템 테이블은 런타임 중 바뀌지 않아 후보 목록을 한 번만 만들어 재사용한다
    [System.NonSerialized] private List<int> _candidates;

    public void ClearCache() => _candidates = null;

    // 이 항목이 뽑을 수 있는 아이템 id 목록. 범위 안에 실제로 존재하는 id만 담긴다.
    public List<int> GetCandidates()
    {
        if (_candidates != null) return _candidates;

        if (minItemId <= 0 && maxItemId <= 0)
        {
            _candidates = ItemSpawner.GetItemIds(type);
            return _candidates;
        }

        // 한쪽만 채우면 그 id 하나만 지정한 것으로 취급한다
        int low = minItemId > 0 ? minItemId : maxItemId;
        int high = maxItemId > 0 ? maxItemId : minItemId;
        if (high < low) (low, high) = (high, low);

        _candidates = new List<int>();
        var table = ItemTable.Instance;
        if (table == null) return _candidates;

        // id는 대역 사이가 비어 있어(801~806 다음이 811) 범위를 다 훑지 않고 실재하는 것만 고른다
        foreach (var item in table.All)
        {
            if (item.Id >= low && item.Id <= high)
                _candidates.Add(item.Id);
        }

        return _candidates;
    }
}

// 씬에 배치할 상자에 붙여 뽑을 항목과 개수를 설정한다.
// ItemSpawner가 호스트에서 씬 박스의 AppendContents()를 호출하고 결과를 게스트에 동기화한다.
[DisallowMultipleComponent]
[RequireComponent(typeof(ItemBox))]
[AddComponentMenu("Item Box/Random Contents (랜덤 내용물)")]
[UnityEngine.Scripting.APIUpdating.MovedFrom(true, null, null, "BoxLootTable")]
public class ItemBoxRandomContents : ItemBoxContents
{
    [Header("한 상자에서 뽑을 항목 수")]
    [SerializeField, Min(0)] private int _minItemCount = 1;
    [SerializeField, Min(0)] private int _maxItemCount = 3;

    [Header("뽑을 후보")]
    [SerializeField] private List<ItemBoxRandomEntry> _entries = new List<ItemBoxRandomEntry>();

    [Header("무기에 딸려오는 탄약")]
    [Tooltip("무기가 뽑히면 그 무기에 맞는 탄약을 자동으로 같이 넣는다. 후보에 직접 넣은 탄약과는 별개다")]
    [SerializeField] private bool _includeWeaponAmmo = true;
    [SerializeField, Min(1)] private int _minAmmoCount = 20;
    [SerializeField, Min(1)] private int _maxAmmoCount = 30;

    private bool _warnedEmpty;
    private readonly HashSet<int> _warnedEmptyRanges = new HashSet<int>();

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (_maxItemCount < _minItemCount) _maxItemCount = _minItemCount;
        if (_maxAmmoCount < _minAmmoCount) _maxAmmoCount = _minAmmoCount;

        foreach (var entry in _entries)
        {
            if (entry == null) continue;
            if (entry.maxQuantity < entry.minQuantity) entry.maxQuantity = entry.minQuantity;
            entry.ClearCache();
        }
    }
#endif

    public void AppendContents(List<int> itemIds, List<int> itemCounts, List<int> itemUids)
    {
        if (!RoomManager.IsHost) return;

        if (_entries.Count == 0)
        {
            if (!_warnedEmpty)
            {
                _warnedEmpty = true;
                Debug.LogWarning($"[ItemBoxRandomContents] 뽑을 후보가 비어 있어 빈 상자가 됩니다. ({name})");
            }
            return;
        }

        // 같은 박스의 추첨 동안 변하지 않는 가중치 합은 한 번만 계산한다.
        float totalWeight = 0f;
        ItemBoxRandomEntry last = null;
        foreach (var entry in _entries)
        {
            if (entry == null || entry.weight <= 0f || float.IsNaN(entry.weight) || float.IsInfinity(entry.weight)) continue;
            totalWeight += entry.weight;
            last = entry;
        }
        if (last == null) return;

        int itemCount = Random.Range(_minItemCount, _maxItemCount + 1);
        for (int i = 0; i < itemCount; i++)
        {
            var entry = PickEntry(totalWeight, last);
            if (entry == null) continue;

            int id = ResolveItemId(entry);
            if (id <= 0) continue;

            var data = ItemTable.Instance.Get(id);
            if (data == null) continue;

            itemIds.Add(id);
            itemCounts.Add(ClampToStack(Random.Range(entry.minQuantity, entry.maxQuantity + 1), data));
            itemUids.Add(ItemSpawner.AssignItemUid(id));

            if (_includeWeaponAmmo && data.Type == ItemType.Weapon)
                AddMatchingAmmo(id, itemIds, itemCounts, itemUids);
        }
    }

    // 가중치 비율로 후보 하나를 고른다.
    private ItemBoxRandomEntry PickEntry(float totalWeight, ItemBoxRandomEntry last)
    {
        float pick = Random.value * totalWeight;
        foreach (var entry in _entries)
        {
            if (entry == null || entry.weight <= 0f || float.IsNaN(entry.weight) || float.IsInfinity(entry.weight)) continue;
            pick -= entry.weight;
            if (pick <= 0f) return entry;
        }

        // 부동소수 누적 오차로 끝까지 빠졌을 때
        return last;
    }

    private int ResolveItemId(ItemBoxRandomEntry entry)
    {
        var candidates = entry.GetCandidates();
        if (candidates.Count > 0) return candidates[Random.Range(0, candidates.Count)];

        WarnEmptyRange(entry);
        return -1;
    }

    // 범위 안에 아이템이 하나도 없으면 그 항목은 조용히 빠지므로, 항목당 한 번만 알려준다.
    private void WarnEmptyRange(ItemBoxRandomEntry entry)
    {
        int index = _entries.IndexOf(entry);
        if (!_warnedEmptyRanges.Add(index)) return;

        if (entry.minItemId > 0 || entry.maxItemId > 0)
            Debug.LogWarning($"[ItemBoxRandomContents] id {entry.minItemId}~{entry.maxItemId} 범위에 아이템이 없습니다. ({name})");
        else
            Debug.LogWarning($"[ItemBoxRandomContents] {entry.type} 타입 아이템이 테이블에 없습니다. ({name})");
    }

    // 인스펙터에 스택 상한보다 큰 값을 넣어도 한 슬롯에 들어갈 수 있는 만큼으로 줄인다.
    private static int ClampToStack(int count, ItemData data) =>
        Mathf.Clamp(count, 1, Mathf.Max(1, data.MaxStack));

    // 뽑힌 무기(itemId)에 맞는 탄약을 GunStatTable에서 찾아 함께 담는다
    private void AddMatchingAmmo(int weaponItemId, List<int> itemIds, List<int> itemCounts, List<int> itemUids)
    {
        int ammoItemId = GunStatTable.Instance.Get(weaponItemId)?.AmmoItemId ?? 0;
        if (ammoItemId <= 0) return;

        var ammoData = ItemTable.Instance.Get(ammoItemId);
        if (ammoData == null) return;

        itemIds.Add(ammoItemId);
        itemCounts.Add(ClampToStack(Random.Range(_minAmmoCount, _maxAmmoCount + 1), ammoData));
        itemUids.Add(ItemSpawner.AssignItemUid(ammoItemId));
    }
}
