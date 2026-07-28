using UnityEngine;

public class PlayerSpawner : MonoBehaviour
{
    [SerializeField] private SpawnId _spawnId = SpawnId.None;
    [SerializeField] private GameObject _playerPrefab;

    private void Start()
    {
        if (GameManager.Instance.PendingSpawnId != _spawnId) return;

        // 씬 전환 경로(포탈 중복 호출, 잔류 PendingSpawnId 등)에 따라 스폰 조건이 두 번 성립하는 경우를
        // 대비한 안전장치. 이미 로컬 플레이어가 있으면 중복 스폰하지 않는다.
        if (FindAnyObjectByType<PlayerController>() != null)
        {
            Debug.LogWarning($"[PlayerSpawner] 이미 존재하는 플레이어가 있어 스폰을 건너뜁니다 (spawner={name}, spawnId={_spawnId}).");
            return;
        }

        var prefab = _playerPrefab != null ? _playerPrefab : GameManager.Instance.PlayerPrefab;
        if (prefab == null) return;
        Instantiate(prefab, transform.position, transform.rotation);
        GameManager.Instance.SetSpawnId(SpawnId.None);
    }
}
