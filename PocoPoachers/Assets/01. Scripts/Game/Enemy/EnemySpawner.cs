using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[System.Serializable]
public class EnemySpawnEntry
{
    public GameObject prefab;
    public int count;
    public int[] gunItemIds;    // 랜덤으로 장착될 무기 ID 목록 (비어 있으면 기본값 사용)
    public int[] helmetItemIds; // 랜덤으로 장착될 헬멧 ID 목록 (비어 있으면 기본값 사용)
    [Range(0f, 1f)] public float helmetSpawnChance = 0.5f;
}

[System.Serializable]
public class EnemySpawnPoint
{
    public Transform centerPoint;
    public float radius = 5f;
    public float patrolRadius = 10f; // 타깃이 없을 때 적이 정찰 기준점에서 벗어날 수 있는 최대 거리
    public Vector3 patrolOffset = Vector3.zero; // 정찰 기준점을 centerPoint에서 이 만큼 옮긴 위치로 사용
    public EnemySpawnEntry[] enemies;

    public Vector3 PatrolOrigin => centerPoint.position + patrolOffset;
}

public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private EnemySpawnPoint[] spawnPoints;

    private List<int> _gunIds;
    private List<int> _helmetIds;
    private Transform _enemiesParent;

    private void Start()
    {
        if (RoomManager.IsHost)
        {
            _gunIds = GetComponent<ItemSpawner>().GetIds(ItemType.Weapon);
            _helmetIds = GetComponent<ItemSpawner>().GetIds(ItemType.Helmet);
            SpawnAll();
        }
    }

    private void SpawnAll()
    {
        foreach (var point in spawnPoints)
        {
            foreach (var entry in point.enemies)
            {
                for (int i = 0; i < entry.count; i++)
                {
                    Vector3 spawnPos = GetRandomNavMeshPosition(point.centerPoint.position, point.radius);
                    var enemy = Instantiate(entry.prefab, spawnPos, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f), GetEnemiesParent());

                    SetPatrolBounds(enemy, point.PatrolOrigin, point.patrolRadius);
                    EquipWeapon(enemy, entry);
                    EquipHelmet(enemy, entry);
                }
            }
        }
    }

    // enemyTypeId(EnemyStat.EnemyId)에 해당하는 프리팹을 찾아 스폰 (게스트가 호스트로부터 받은 적을 생성할 때 사용)
    public GameObject SpawnEnemyByTypeId(int enemyTypeId, Vector3 pos, Quaternion rotation)
    {
        foreach (var point in spawnPoints)
        {
            foreach (var entry in point.enemies)
            {
                if (entry.prefab == null) continue;

                var stat = entry.prefab.GetComponent<EnemyStat>();
                if (stat == null || stat.EnemyId != enemyTypeId) continue;

                var enemy = Instantiate(entry.prefab, pos, rotation, GetEnemiesParent());
                SetPatrolBounds(enemy, point.PatrolOrigin, point.patrolRadius);
                return enemy;
            }
        }
        return null;
    }

    private void SetPatrolBounds(GameObject enemy, Vector3 origin, float patrolRadius)
    {
        var bounds = enemy.GetComponent<EnemyPatrolBounds>();
        if (bounds == null) bounds = enemy.AddComponent<EnemyPatrolBounds>();
        bounds.SetBounds(origin, patrolRadius);
    }

    private Transform GetEnemiesParent()
    {
        if (_enemiesParent == null)
        {
            var existing = GameObject.Find("Enemies");
            _enemiesParent = existing != null ? existing.transform : new GameObject("Enemies").transform;
        }
        return _enemiesParent;
    }

    private void EquipWeapon(GameObject enemy, EnemySpawnEntry entry)
    {
        var weaponCtrl = enemy.GetComponent<AIWeaponController>();
        if (weaponCtrl == null) return;

        if (entry.gunItemIds != null && entry.gunItemIds.Length > 0)
            weaponCtrl.EquipGun(entry.gunItemIds[Random.Range(0, entry.gunItemIds.Length)]);
        else if (_gunIds != null && _gunIds.Count > 0)
            weaponCtrl.EquipGun(_gunIds[Random.Range(0, _gunIds.Count)]);
    }

    private void EquipHelmet(GameObject enemy, EnemySpawnEntry entry)
    {
        if (Random.value > entry.helmetSpawnChance) return;

        var armorCtrl = enemy.GetComponent<ArmorController>();
        if (armorCtrl == null) return;

        int[] pool = (entry.helmetItemIds != null && entry.helmetItemIds.Length > 0)
            ? entry.helmetItemIds
            : (_helmetIds != null && _helmetIds.Count > 0 ? _helmetIds.ToArray() : null);

        if (pool == null || pool.Length == 0) return;

        var itemData = ItemTable.Instance.Get(pool[Random.Range(0, pool.Length)]);
        if (itemData != null)
            armorCtrl.Equip(itemData, 0, ItemSpawner.AssignItemUid(itemData.id));
    }

    private Vector3 GetRandomNavMeshPosition(Vector3 origin, float radius)
    {
        for (int i = 0; i < 10; i++)
        {
            Vector3 randomDir = origin + Random.insideUnitSphere * radius;
            randomDir.y = origin.y;
            if (NavMesh.SamplePosition(randomDir, out NavMeshHit hit, radius, NavMesh.AllAreas))
                return hit.position;
        }
        return origin;
    }

    private void OnDrawGizmosSelected()
    {
        if (spawnPoints == null) return;

        foreach (var point in spawnPoints)
        {
            if (point.centerPoint == null) continue;

            // 모든 적이 patrolOffset만큼 옮겨진 기준점을 기준으로 정찰하므로, 벗어날 수 있는 최대 범위는 patrolRadius 그 자체 — 겹치지 않도록 먼저 채운다
            Vector3 patrolOrigin = point.PatrolOrigin;
            Gizmos.color = new Color(1f, 0.9f, 0.1f, 0.3f);
            Gizmos.DrawSphere(patrolOrigin, point.patrolRadius);
            Gizmos.color = new Color(1f, 0.9f, 0.1f, 1f);
            Gizmos.DrawWireSphere(patrolOrigin, point.patrolRadius);

            // centerPoint에서 정찰 기준점까지의 오프셋을 선으로 표시
            if (point.patrolOffset != Vector3.zero)
                Gizmos.DrawLine(point.centerPoint.position, patrolOrigin);

            // 스폰 위치가 흩뿌려지는 범위
            Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.3f);
            Gizmos.DrawSphere(point.centerPoint.position, point.radius);

            Gizmos.color = new Color(1f, 0.3f, 0.3f, 1f);
            Gizmos.DrawWireSphere(point.centerPoint.position, point.radius);
        }
    }
}
