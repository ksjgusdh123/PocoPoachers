using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ItemQuantityRange
{
    public ItemType Type;
    public int Min = 1;
    public int Max = 1;
}

[System.Serializable]
public class BoxSpawnPoint
{
    public Transform point;
    public GameObject boxPrefab; // ItemBox + BoxLootTable가 붙은 프리팹. 인스턴스화하지 않고 BoxLootTable 설정값만 읽어온다.
}

// 호스트에서 호출하기

public class ItemSpawner : MonoBehaviour
{
    [Header("Box Spawn Points")]
    [SerializeField] private BoxSpawnPoint[] _boxSpawnPoints;

    [Header("Ground Placement")]
    [SerializeField] private LayerMask _groundLayer;

    static Dictionary<ItemType, List<int>> _itemIdsByType;

    int _nextUid = 1000;
    static int _nextItemUid = 1;
    const int BOX_TYPE_ID = 301;

    // 새로 발급되는 무기/방어구의 초기 내구도를 최대치의 이 비율 범위 안에서 랜덤으로 정한다
    private const float MinInitialDurabilityRatio = 0.5f;
    private const float DefaultMaxDurability = 100f; // GunBase/ArmorBase 기본 최대 내구도와 동일

    // 스택 불가 아이템(무기/방어구 등)에만 고유 uid 발급, 소모품류는 0
    // 모든 스포너(필드 박스/적 드롭 등)가 공유하는 카운터라 호스트 전역에서 충돌 없음
    public static int AssignItemUid(int itemId)
    {
        var data = ItemTable.Instance.Get(itemId);
        if (data == null || data.MaxStack > 1) return 0;

        int uid = _nextItemUid++;

        if (HasDurability(data.Type))
        {
            float current = DefaultMaxDurability * Random.Range(MinInitialDurabilityRatio, 1f);
            WorldEquipmentManager.SetInitialDurability(uid, itemId, current, DefaultMaxDurability);
        }

        return uid;
    }

    private static bool HasDurability(ItemType type) =>
        type == ItemType.Weapon || type == ItemType.Helmet || type == ItemType.Armor;

    // 타입별 아이템 id 목록. ItemTable 기반이라 스포너 인스턴스와 무관하게 공유되는 정적 캐시.
    public static List<int> GetItemIds(ItemType type)
    {
        if (_itemIdsByType == null) BuildItemIdCache();
        return _itemIdsByType.TryGetValue(type, out var ids) ? ids : new List<int>();
    }

    static void BuildItemIdCache()
    {
        _itemIdsByType = new Dictionary<ItemType, List<int>>();

        var table = ItemTable.Instance;
        if (table == null) return;

        foreach (ItemType type in System.Enum.GetValues(typeof(ItemType)))
            _itemIdsByType[type] = new List<int>();
        foreach (var item in table.All)
        {
            if (_itemIdsByType.ContainsKey(item.Type))
                _itemIdsByType[item.Type].Add(item.Id);
        }
    }

    // 하위 호환: EnemySpawner 등에서 인스턴스로 접근하던 코드가 그대로 동작하도록 유지
    public List<int> GetIds(ItemType type) => GetItemIds(type);

    void Start()
    {
        if (RoomManager.IsHost)
            SpawnInitBoxes();
    }

    public void SpawnInitBoxes()
    {
        if (!RoomManager.IsHost) return;
        if (_boxSpawnPoints == null) return;

        var omgr = ObjectManager.Instance;

        foreach (var sp in _boxSpawnPoints)
        {
            if (sp.point == null || sp.boxPrefab == null) continue;

            var lootTable = sp.boxPrefab.GetComponent<BoxLootTable>();
            if (lootTable == null) continue;

            int uid = _nextUid++;
            lootTable.Roll(out var itemIds, out var itemCounts, out var itemUids);

            Vector3 pos = GetGroundPosition(sp.point.position, _groundLayer);
            float rot = sp.point.eulerAngles.y;

            var data = new H_ItemSpawnT
            {
                Uid = uid,
                TypeId = BOX_TYPE_ID,
                Pos = new Vec3T { X = pos.x, Y = pos.y, Z = pos.z },
                Rotation = rot,
                ItemIds = itemIds,
                ItemCount = itemCounts,
                ItemUids = itemUids,
            };

            omgr?.RegisterSpawnedBox(data);
            omgr?.SpawnItemBox(uid, BOX_TYPE_ID, pos, rot)
                ?.Initialize(itemIds.ToArray(), itemCounts.ToArray(), itemUids.ToArray());
        }
    }

    public void ResetSpawnState()
    {
        _nextUid = 1000;
        _nextItemUid = 1;
    }

    // 저장에서 복원한 아이템 uid와 겹치지 않도록 카운터를 최댓값 다음으로 밀어둔다 (게임 로드 시 호출)
    public static void SeedItemUid(int maxUsedUid)
    {
        if (maxUsedUid >= _nextItemUid)
            _nextItemUid = maxUsedUid + 1;
    }

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
    private void OnDrawGizmosSelected()
    {
        if (_boxSpawnPoints == null) return;

        foreach (var sp in _boxSpawnPoints)
        {
            if (sp.point == null) continue;

            Gizmos.color = new Color(1f, 1f, 0f, 0.6f);
            Gizmos.DrawSphere(sp.point.position, 0.3f);
            Gizmos.DrawWireCube(sp.point.position, Vector3.one);
        }
    }
}
