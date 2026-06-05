using UnityEngine;

public class PlayerSpawner : MonoBehaviour
{
    [SerializeField] private SpawnId _spawnId = SpawnId.None;

    private void Start()
    {
        if (GameManager.Instance.PendingSpawnId != _spawnId) return;
        var prefab = GameManager.Instance.PlayerPrefab;
        if (prefab == null) return;
        Instantiate(prefab, transform.position, transform.rotation);
    }
}
