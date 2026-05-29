using UnityEngine;

public class EnemyItemBoxDropper : MonoBehaviour
{
    [SerializeField, Range(1, 8)] private int _minItemCount = 1;
    [SerializeField, Range(1, 8)] private int _maxItemCount = 4;
    [SerializeField] private LayerMask _groundLayer;

    private static int _nextUid = 5000;
    private const int BOX_TYPE_ID = 301;

    private StatBase _stat;

    private void Awake()
    {
        _stat = GetComponent<StatBase>();
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

        // 아이템 목록 생성
        int itemCount = Random.Range(_minItemCount, _maxItemCount + 1);
        var itemIds = ItemSpawner.Roll(itemCount);

        // 호스트: 박스 스폰
        omgr.SpawnItemBox(uid, BOX_TYPE_ID, spawnPos, spawnRot)
            ?.Initialize(itemIds.ToArray());

        // 나중에 접속한 게스트를 위해 등록
        var spawnData = new H_ItemSpawnT
        {
            Uid = uid,
            TypeId = BOX_TYPE_ID,
            Pos = new Vec3T { X = spawnPos.x, Y = spawnPos.y, Z = spawnPos.z },
            Rotation = spawnRot,
            ItemIds = itemIds,
        };
        omgr.RegisterSpawnedBox(spawnData);

        RoomSync.ItemSpawn(spawnData.Uid, spawnData.TypeId, spawnPos, spawnRot, itemIds);
    }

}
