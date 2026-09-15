using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[System.Serializable]
public class EnemySpawnEntry
{
    public GameObject prefab;
    public int count;
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

    private Transform _enemiesParent;

    private void Start()
    {
        if (RoomManager.IsHost)
        {
            SpawnAll();
        }
    }

    private void SpawnAll()
    {
        foreach (var point in spawnPoints)
        {
            foreach (var entry in point.enemies)
            {
                if (entry.prefab == null || entry.prefab.GetComponent<EnemyStat>() == null
                    || EnemyTable.Instance.Get(entry.prefab.GetComponent<EnemyStat>().EnemyId) == null)
                {
                    Debug.LogWarning("[EnemySpawner] 적 프리팹 또는 CSV ID가 유효하지 않아 스폰을 건너뜁니다.");
                    continue;
                }
                for (int i = 0; i < entry.count; i++)
                {
                    Vector3 spawnPos = GetRandomNavMeshPosition(point.centerPoint.position, point.radius);
                    var enemy = Instantiate(entry.prefab, spawnPos, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f), GetEnemiesParent());

                    SetPatrolBounds(enemy, point.PatrolOrigin, point.patrolRadius);
                    var equipment = enemy.GetComponent<EnemyStartEquipment>();
                    if (equipment == null) equipment = enemy.AddComponent<EnemyStartEquipment>();
                    equipment.EquipFromTable();
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
