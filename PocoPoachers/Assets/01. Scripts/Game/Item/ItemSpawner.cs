using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ItemQuantityRange
{
    public ItemType Type;
    public int Min = 1;
    public int Max = 1;
}

// 씬 배치 박스를 검색해 등록한다. 아이템 선정은 호스트에서만 수행한다.
public class ItemSpawner : MonoBehaviour
{
    static Dictionary<ItemType, List<int>> _itemIdsByType;
    static int _nextItemUid = 1;

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

    void Start() => SpawnInitBoxes();

    // 기존 호출부 호환용. 이제 새 박스를 만들지 않고 현재 씬의 박스를 등록한다.
    public void SpawnInitBoxes() => InitializeSceneBoxes(gameObject.scene);

    public static void InitializeSceneBoxes(UnityEngine.SceneManagement.Scene scene)
    {
        var manager = ObjectManager.Instance;
        if (manager == null || !scene.IsValid() || !scene.isLoaded) return;

        foreach (var root in scene.GetRootGameObjects())
        foreach (var box in root.GetComponentsInChildren<ItemBox>(true))
        {
            if (box is LootBox) continue;
            // 중도 입장 준비 중 재검색할 때 런타임 적 드롭 등은 건드리지 않는다.
            var worldObject = box.GetComponent<WorldObject>();
            if (worldObject != null && worldObject.Kind == ObjectKind.ItemBox && worldObject.Id > 0) continue;
            if (!box.HasValidSceneBoxId)
            {
                Debug.LogError($"[ItemSpawner] 박스 고정 ID가 없습니다. 에디터에서 씬을 저장해 주세요: {box.name}", box);
                continue;
            }
            int id = box.SceneBoxId;
            if (manager.TryGet(ObjectKind.ItemBox, id, out var existing) && existing != null)
            {
                if (existing.gameObject != box.gameObject)
                {
                    Debug.LogError($"[ItemSpawner] 박스 ID가 중복됩니다: {id}, {box.name}", box);
                    continue;
                }
            }
            else
            {
                manager.RegisterSceneObject(ObjectKind.ItemBox, id, box.gameObject);
            }

            var inventory = box.GetComponent<Inventory>();
            if (inventory == null) inventory = box.gameObject.AddComponent<Inventory>();
            inventory.EnsureInitialized();
            if (!RoomManager.IsHost || box.SceneContentsInitialized) continue;

            // 고정 내용을 먼저 담아 슬롯이 부족할 때 고정 지급을 우선한다.
            var ids = new List<int>();
            var counts = new List<int>();
            var uids = new List<int>();
            var noReveal = new HashSet<int>();
            var fixedContents = box.GetComponent<ItemBoxFixedContents>();
            if (fixedContents != null && fixedContents.enabled)
                fixedContents.AppendContents(ids, counts, uids, noReveal);
            var loot = box.GetComponent<ItemBoxRandomContents>();
            if (loot != null && loot.enabled)
                loot.AppendContents(ids, counts, uids);
            box.Initialize(ids.ToArray(), counts.ToArray(), uids.ToArray(), noReveal);
            box.SceneContentsInitialized = true;
            var position = box.transform.position;
            manager.RegisterSpawnedBox(new H_ItemSpawnT
            {
                Uid = id,
                TypeId = 0, // 게스트 씬에 이미 존재하므로 프리팹 생성은 하지 않는다.
                Pos = new Vec3T { X = position.x, Y = position.y, Z = position.z },
                Rotation = box.transform.eulerAngles.y,
            });
        }
    }

    public void ResetSpawnState() => _nextItemUid = 1;

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

}
