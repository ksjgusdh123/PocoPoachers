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
    public EnemySpawnEntry[] enemies;
}

public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private EnemySpawnPoint[] spawnPoints;

    private void Start()
    {
        SpawnAll();
    }

    private void SpawnAll()
    {
        Transform enemiesParent = new GameObject("Enemies").transform;

        foreach (var point in spawnPoints)
        {
            foreach (var entry in point.enemies)
            {
                for (int i = 0; i < entry.count; i++)
                {
                    Vector3 spawnPos = GetRandomNavMeshPosition(point.centerPoint.position, point.radius);
                    Instantiate(entry.prefab, spawnPos, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f), enemiesParent);
                }
            }
        }
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

            Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.3f);
            Gizmos.DrawSphere(point.centerPoint.position, point.radius);

            Gizmos.color = new Color(1f, 0.3f, 0.3f, 1f);
            Gizmos.DrawWireSphere(point.centerPoint.position, point.radius);
        }
    }
}
