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
        var noRevealIds = new HashSet<int>();

        CollectEquippedItems(itemIds, itemCounts, noRevealIds);
        CollectRandomItems(itemIds, itemCounts);
        SpawnBox(omgr, uid, spawnPos, spawnRot, itemIds, itemCounts, noRevealIds);
    }

    private void CollectEquippedItems(List<int> itemIds, List<int> itemCounts, HashSet<int> noRevealIds)
    {
        var gun = _weaponController?.Gun;
        if (gun != null)
        {
            itemIds.Add(gun.GunData.itemId);
            itemCounts.Add(1);
            noRevealIds.Add(gun.GunData.itemId);

            if (gun.GunData.ammoItemId > 0)
            {
                itemIds.Add(gun.GunData.ammoItemId);
                itemCounts.Add(Random.Range(_minAmmoCount, _maxAmmoCount + 1));
                noRevealIds.Add(gun.GunData.ammoItemId);
            }
        }

        var armorMount = _armorController?.GetComponent<ArmorMount>();
        if (armorMount != null)
        {
            int helmetId = armorMount.GetEquippedItemId();
            if (helmetId > 0)
            {
                itemIds.Add(helmetId);
                itemCounts.Add(1);
                noRevealIds.Add(helmetId);
            }
        }
    }

    private void CollectRandomItems(List<int> itemIds, List<int> itemCounts)
    {
        int itemCount = Random.Range(_minItemCount, _maxItemCount + 1);
        var rolledIds = ItemSpawner.Roll(itemCount);
        foreach (var id in rolledIds) { itemIds.Add(id); itemCounts.Add(1); }
    }

    private void SpawnBox(ObjectManager omgr, int uid, Vector3 spawnPos, float spawnRot,
        List<int> itemIds, List<int> itemCounts, HashSet<int> noRevealIds)
    {
        omgr.SpawnItemBox(uid, BOX_TYPE_ID, spawnPos, spawnRot)
            ?.Initialize(itemIds.ToArray(), itemCounts.ToArray(), noRevealIds);

        var spawnData = new H_ItemSpawnT
        {
            Uid = uid,
            TypeId = BOX_TYPE_ID,
            Pos = new Vec3T { X = spawnPos.x, Y = spawnPos.y, Z = spawnPos.z },
            Rotation = spawnRot,
            ItemIds = itemIds,
            ItemCount = itemCounts,
        };
        omgr.RegisterSpawnedBox(spawnData);
        RoomSync.ItemSpawn(spawnData.Uid, spawnData.TypeId, spawnPos, spawnRot, itemIds);
    }
}
