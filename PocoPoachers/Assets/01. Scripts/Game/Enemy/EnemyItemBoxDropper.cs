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

    private void Awake()
    {
        _stat = GetComponent<StatBase>();
        _weaponController = GetComponent<AIWeaponController>();
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

        // 장착 중인 총과 탄환 먼저 추가 (flip 제외)
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

        // 랜덤 아이템 목록 추가
        int itemCount = Random.Range(_minItemCount, _maxItemCount + 1);
        var rolledIds = ItemSpawner.Roll(itemCount);
        foreach (var id in rolledIds) { itemIds.Add(id); itemCounts.Add(1); }

        // 호스트: 박스 스폰
        omgr.SpawnItemBox(uid, BOX_TYPE_ID, spawnPos, spawnRot)
            ?.Initialize(itemIds.ToArray(), itemCounts.ToArray(), noRevealIds);

        // 나중에 접속한 게스트를 위해 등록
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
