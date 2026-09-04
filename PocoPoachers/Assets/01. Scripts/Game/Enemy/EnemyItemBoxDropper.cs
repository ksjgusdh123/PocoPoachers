using System.Collections.Generic;
using UnityEngine;

public class EnemyItemBoxDropper : MonoBehaviour
{
    [SerializeField, Range(1, 8)] private int _minItemCount = 1;
    [SerializeField, Range(1, 8)] private int _maxItemCount = 4;
    [SerializeField, Range(1, 30)] private int _minAmmoCount = 20;
    [SerializeField, Range(1, 30)] private int _maxAmmoCount = 30;
    [SerializeField] private LayerMask _groundLayer;

    private static int _nextUid = 5000;
    private const int BOX_TYPE_ID = 301;

    private StatBase _stat;
    private AIWeaponController _weaponController;
    private ArmorController _armorController;

    private void Awake()
    {
        _stat = GetComponent<StatBase>();
        _weaponController = GetComponent<AIWeaponController>();
        _armorController = GetComponent<ArmorController>();
    }

    private void OnEnable()
    {
        if (_stat != null) _stat.OnDie += DropItems;
    }

    private void OnDisable()
    {
        if (_stat != null) _stat.OnDie -= DropItems;
    }

    private void DropItems()
    {
        if (!RoomManager.IsHost) return;

        var omgr = ObjectManager.Instance;
        if (omgr == null) return;

        int uid = _nextUid++;
        Vector3 spawnPos = ItemSpawner.GetGroundPosition(transform.position, _groundLayer);
        float spawnRot = transform.eulerAngles.y;

        var itemIds = new List<int>();
        var itemCounts = new List<int>();
        var itemUids = new List<int>();
        var noRevealIndices = new HashSet<int>();

        CollectEquippedItems(itemIds, itemCounts, itemUids, noRevealIndices);
        CollectRandomItems(itemIds, itemCounts, itemUids);
        SpawnBox(omgr, uid, spawnPos, spawnRot, itemIds, itemCounts, itemUids, noRevealIndices);
    }

    // 적이 몸에 걸치고 있어 이미 눈에 보였던 아이템은 리빌 대상에서 뺀다.
    // 제외 대상은 아이템 id가 아니라 방금 담은 위치(인덱스)로 기록해야 한다 — id로 기록하면
    // 뒤이어 굴러나온 같은 id의 랜덤 아이템까지 함께 공개돼버린다.
    private void CollectEquippedItems(List<int> itemIds, List<int> itemCounts, List<int> itemUids, HashSet<int> noRevealIndices)
    {
        var gun = _weaponController?.Gun;
        if (gun != null)
        {
            itemIds.Add(gun.ItemId);
            itemCounts.Add(1);
            // 장착 중이던 개체라 기존 uid가 있으면 그대로, 없으면 새로 발급
            itemUids.Add(gun.Uid != 0 ? gun.Uid : ItemSpawner.AssignItemUid(gun.ItemId));
            noRevealIndices.Add(itemIds.Count - 1);

            if (gun.Stat.AmmoItemId > 0)
            {
                itemIds.Add(gun.Stat.AmmoItemId);
                itemCounts.Add(Random.Range(_minAmmoCount, _maxAmmoCount + 1));
                itemUids.Add(0);
                noRevealIndices.Add(itemIds.Count - 1);
            }
        }

        var armorMount = _armorController?.GetComponent<ArmorMount>();
        if (armorMount != null)
        {
            int helmetId = armorMount.GetEquippedItemId();
            if (helmetId > 0)
            {
                var armor = armorMount.GetArmor();
                itemIds.Add(helmetId);
                itemCounts.Add(1);
                itemUids.Add(armor != null && armor.Uid != 0 ? armor.Uid : ItemSpawner.AssignItemUid(helmetId));
                noRevealIndices.Add(itemIds.Count - 1);
            }
        }
    }

    private void CollectRandomItems(List<int> itemIds, List<int> itemCounts, List<int> itemUids)
    {
        int itemCount = Random.Range(_minItemCount, _maxItemCount + 1);
        var rolledIds = ItemSpawner.Roll(itemCount);
        foreach (var id in rolledIds)
        {
            itemIds.Add(id);
            itemCounts.Add(1);
            itemUids.Add(ItemSpawner.AssignItemUid(id));
        }
    }

    private void SpawnBox(ObjectManager omgr, int uid, Vector3 spawnPos, float spawnRot,
        List<int> itemIds, List<int> itemCounts, List<int> itemUids, HashSet<int> noRevealIndices)
    {
        omgr.SpawnItemBox(uid, BOX_TYPE_ID, spawnPos, spawnRot)
            ?.Initialize(itemIds.ToArray(), itemCounts.ToArray(), itemUids.ToArray(), noRevealIndices);

        var spawnData = new H_ItemSpawnT
        {
            Uid = uid,
            TypeId = BOX_TYPE_ID,
            Pos = new Vec3T { X = spawnPos.x, Y = spawnPos.y, Z = spawnPos.z },
            Rotation = spawnRot,
            ItemIds = itemIds,
            ItemCount = itemCounts,
            ItemUids = itemUids,
            NoRevealIndices = new List<int>(noRevealIndices),
        };
        omgr.RegisterSpawnedBox(spawnData);
        RoomSync.ItemSpawn(spawnData.Uid, spawnData.TypeId, spawnPos, spawnRot, itemIds, itemCounts, itemUids, spawnData.NoRevealIndices);
    }
}
