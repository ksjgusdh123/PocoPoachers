using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ItemQuantityRange
{
    public ItemType Type;
    public int Min = 1;
    public int Max = 1;
}

// 호스트에서 호출하기

public class ItemSpawner : MonoBehaviour
{
    [Header("Simple Spawn Area")]
    [SerializeField] private Vector3 _spawnAreaSize = new Vector3(10f, 0f, 10f); // 가로, 높이, 세로 범위
    [SerializeField] private int _totalBoxCount = 5;

    [Header("Ground Placement")]
    [SerializeField] private LayerMask _groundLayer;

    [Header("Random Settings")]
    [SerializeField, Range(0f, 1f)] private float _weaponChance = 0.7f;
    [SerializeField, Range(0f, 1f)] private float _armorChance = 0.3f;
    [SerializeField] private int _minItemPerBox = 1;
    [SerializeField] private int _maxItemPerBox = 3;
    [SerializeField] private List<ItemQuantityRange> _quantityRanges = new List<ItemQuantityRange>();
    [SerializeField] private int _defaultMinQuantity = 1;
    [SerializeField] private int _defaultMaxQuantity = 1;

    private Dictionary<ItemType, List<int>> _cachedItemIds = new Dictionary<ItemType, List<int>>();
    private Dictionary<ItemType, (int min, int max)> _quantityMap = new Dictionary<ItemType, (int, int)>();

    int _nextUid = 1000;
    const int BOX_TYPE_ID = 301;

    public List<int> GunIds => _cachedItemIds[ItemType.Weapon];

    void Start()
    {
        CacheItemIds();

        if (RoomManager.IsHost)
            SpawnInitBoxes();
    }

    void CacheItemIds()
    {
        var table = ItemTable.Instance;
        if (table == null) return;
        foreach (ItemType type in System.Enum.GetValues(typeof(ItemType)))
            _cachedItemIds[type] = new List<int>();
        foreach (var item in table.All)
        {
            if (_cachedItemIds.ContainsKey(item.Type))
                _cachedItemIds[item.Type].Add(item.Id);
        }

        foreach (var range in _quantityRanges)
            _quantityMap[range.Type] = (range.Min, range.Max);
    }

    (int min, int max) GetQuantityRange(ItemType type)
    {
        if (_quantityMap.TryGetValue(type, out var range)) return range;
        return (_defaultMinQuantity, _defaultMaxQuantity);
    }

    public void SpawnInitBoxes()
    {
        if (!RoomManager.IsHost) return;

        var omgr = ObjectManager.Instance;
        Vector3 origin = transform.position; // 스포너의 현재 위치를 중심점으로 사용

        for (int i = 0; i < _totalBoxCount; i++)
        {
            int uid = _nextUid++;

            // 1. 인스펙터 설정값 기반 랜덤 좌표 계산
            float rx = Random.Range(-_spawnAreaSize.x * 0.5f, _spawnAreaSize.x * 0.5f);
            float rz = Random.Range(-_spawnAreaSize.z * 0.5f, _spawnAreaSize.z * 0.5f);
            Vector3 randomPoint = origin + new Vector3(rx, _spawnAreaSize.y, rz);

            // 2. 바닥 체크 (SpawnUtility 활용)
            Vector3 randomPos = GetGroundPosition(randomPoint, _groundLayer);
            float randomRot = Random.Range(0f, 360f);

            // 아이템 구성
            int itemCount = Random.Range(_minItemPerBox, _maxItemPerBox + 1);
            List<int> randomItems = new List<int>();
            List<int> randomCounts = new List<int>();
            for (int j = 0; j < itemCount; j++)
            {
                var (pickedId, pickedType) = GetRandomItemId();
                if (pickedId == -1) continue;
                var (min, max) = GetQuantityRange(pickedType);
                randomItems.Add(pickedId);
                randomCounts.Add(Random.Range(min, max + 1));
            }

            // temp
            randomItems.Add(601);
            randomCounts.Add(30);

            var data = new H_ItemSpawnT
            {
                Uid = uid,
                TypeId = BOX_TYPE_ID,
                Pos = new Vec3T { X = randomPos.x, Y = randomPos.y, Z = randomPos.z },
                Rotation = randomRot,
                ItemIds = randomItems,
                ItemCount = randomCounts,
            };

            omgr?.RegisterSpawnedBox(data);
            omgr?.SpawnItemBox(uid, BOX_TYPE_ID, randomPos, randomRot)
                ?.Initialize(randomItems.ToArray(), randomCounts.ToArray());
        }

        // temp
        int iuid = _nextUid++;
        var idata = new H_ItemSpawnT
        {
            Uid = iuid,
            TypeId = 302,
            Pos = new Vec3T { X = 4.5f, Y = 0f, Z = 2f },
            Rotation = 0,
        };
        omgr.RegisterSpawnedBox(idata);
        omgr.SpawnItemBox(iuid, 302, new Vector3(4.5f, 0f, 2f), 0);
    }

    (int id, ItemType type) GetRandomItemId()
    {
        float value = Random.value;
        var helmets = _cachedItemIds[ItemType.Helmet];
        var weapons = _cachedItemIds[ItemType.Weapon];
        var consumables = _cachedItemIds[ItemType.Consumable];

        if (value < _armorChance && helmets.Count > 0)
            return (helmets[Random.Range(0, helmets.Count)], ItemType.Helmet);
        else if (value < _weaponChance && weapons.Count > 0)
            return (weapons[Random.Range(0, weapons.Count)], ItemType.Weapon);

        return consumables.Count > 0 ? (consumables[Random.Range(0, consumables.Count)], ItemType.Consumable) : (-1, ItemType.None);
    }

    public void ResetSpawnState() => _nextUid = 1000;

    static List<int> _dropIds;

    public static List<int> Roll(int count)
    {
        if (_dropIds == null)
        {
            _dropIds = new List<int>();
            foreach (var item in ItemTable.Instance.All)
                if (item.id < 300) _dropIds.Add(item.id);
        }
        count = Mathf.Clamp(count, 1, 8);
        var result = new List<int>(count);
        for (int i = 0; i < count && _dropIds.Count > 0; i++)
            result.Add(_dropIds[Random.Range(0, _dropIds.Count)]);
        return result;
    }

    public static Vector3 GetGroundPosition(Vector3 origin, LayerMask layerMask, float maxDistance = 100f, float offsetY = 50f)
    {
        Vector3 rayStart = new Vector3(origin.x, origin.y + offsetY, origin.z);

        if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, maxDistance, layerMask))
        {
            return hit.point;
        }

        return origin;
    }

    public static Vector3 GetRandomPointInVolume(BoxCollider volume, LayerMask groundLayer)
    {
        Bounds bounds = volume.bounds;

        float randomX = Random.Range(bounds.min.x, bounds.max.x);
        float randomZ = Random.Range(bounds.min.z, bounds.max.z);

        Vector3 randomPos = new Vector3(randomX, bounds.center.y, randomZ);
        return GetGroundPosition(randomPos, groundLayer);
    }

    // --- 에디터 시각화 (Gizmos) ---
    private void OnDrawGizmosSelected() // 선택했을 때만 영역이 보이도록 변경
    {
        Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
        Gizmos.DrawCube(transform.position, new Vector3(_spawnAreaSize.x, 1f, _spawnAreaSize.z));

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position, new Vector3(_spawnAreaSize.x, 1f, _spawnAreaSize.z));
    }
}