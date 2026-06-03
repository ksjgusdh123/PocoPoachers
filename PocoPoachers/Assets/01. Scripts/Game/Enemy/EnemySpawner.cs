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
    public EnemySpawnEntry[] enemies;
}

public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private EnemySpawnPoint[] spawnPoints;

    private List<int> _gunIds;
    private List<int> _helmetIds;

    private void Start()
    {
        _gunIds = GetComponent<ItemSpawner>().GetIds(ItemType.Weapon);
        _helmetIds = GetComponent<ItemSpawner>().GetIds(ItemType.Helmet);
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
                    var enemy = Instantiate(entry.prefab, spawnPos, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f), enemiesParent);

                    EquipWeapon(enemy, entry);
                    EquipHelmet(enemy, entry);
                }
            }
        }
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
            armorCtrl.Equip(itemData, 0);
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
