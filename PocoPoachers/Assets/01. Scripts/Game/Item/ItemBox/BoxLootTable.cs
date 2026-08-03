using System.Collections.Generic;
using UnityEngine;

// 상자 프리팹에 붙여서 그 상자가 뽑을 아이템 확률/개수를 개별로 설정한다.
// (예: 무기 위주 상자는 weaponChance를 높게, 방어구 위주 상자는 armorChance를 높게)
// ItemSpawner는 이 컴포넌트가 붙은 프리팹을 인스턴스화하지 않고 설정값만 읽어 Roll()을 호출한다.
public class BoxLootTable : MonoBehaviour
{
    [SerializeField, Range(0f, 1f)] private float _weaponChance = 0.7f;
    [SerializeField, Range(0f, 1f)] private float _armorChance = 0.3f;
    [SerializeField] private int _minItemCount = 1;
    [SerializeField] private int _maxItemCount = 3;
    [SerializeField] private List<ItemQuantityRange> _quantityRanges = new List<ItemQuantityRange>();
    [SerializeField] private int _defaultMinQuantity = 1;
    [SerializeField] private int _defaultMaxQuantity = 1;

    [Header("Weapon Ammo")]
    [SerializeField] private int _minAmmoCount = 20;
    [SerializeField] private int _maxAmmoCount = 30;

    private Dictionary<ItemType, (int min, int max)> _quantityMap;

    public void Roll(out List<int> itemIds, out List<int> itemCounts, out List<int> itemUids)
    {
        BuildQuantityMapIfNeeded();

        itemIds = new List<int>();
        itemCounts = new List<int>();
        itemUids = new List<int>();

        int itemCount = Random.Range(_minItemCount, _maxItemCount + 1);
        for (int i = 0; i < itemCount; i++)
        {
            var (id, type) = GetRandomItemId();
            if (id == -1) continue;

            var (min, max) = GetQuantityRange(type);
            itemIds.Add(id);
            itemCounts.Add(Random.Range(min, max + 1));
            itemUids.Add(ItemSpawner.AssignItemUid(id));

            if (type == ItemType.Weapon)
                AddMatchingAmmo(id, itemIds, itemCounts, itemUids);
        }
    }

    // 뽑힌 무기(itemId)에 맞는 탄약을 GunStatTable에서 찾아 함께 담는다
    private void AddMatchingAmmo(int weaponItemId, List<int> itemIds, List<int> itemCounts, List<int> itemUids)
    {
        int ammoItemId = GunStatTable.Instance.Get(weaponItemId)?.AmmoItemId ?? 0;
        if (ammoItemId <= 0) return;

        itemIds.Add(ammoItemId);
        itemCounts.Add(Random.Range(_minAmmoCount, _maxAmmoCount + 1));
        itemUids.Add(ItemSpawner.AssignItemUid(ammoItemId));
    }

    private void BuildQuantityMapIfNeeded()
    {
        if (_quantityMap != null) return;

        _quantityMap = new Dictionary<ItemType, (int, int)>();
        foreach (var range in _quantityRanges)
            _quantityMap[range.Type] = (range.Min, range.Max);
    }

    private (int min, int max) GetQuantityRange(ItemType type)
    {
        if (_quantityMap.TryGetValue(type, out var range)) return range;
        return (_defaultMinQuantity, _defaultMaxQuantity);
    }

    private (int id, ItemType type) GetRandomItemId()
    {
        float value = Random.value;
        var helmets = ItemSpawner.GetItemIds(ItemType.Helmet);
        var weapons = ItemSpawner.GetItemIds(ItemType.Weapon);
        var consumables = ItemSpawner.GetItemIds(ItemType.Consumable);

        if (value < _armorChance && helmets.Count > 0)
            return (helmets[Random.Range(0, helmets.Count)], ItemType.Helmet);
        else if (value < _weaponChance && weapons.Count > 0)
            return (weapons[Random.Range(0, weapons.Count)], ItemType.Weapon);

        return consumables.Count > 0 ? (consumables[Random.Range(0, consumables.Count)], ItemType.Consumable) : (-1, ItemType.None);
    }
}
