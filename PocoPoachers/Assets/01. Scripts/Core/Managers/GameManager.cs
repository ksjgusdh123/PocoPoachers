using UnityEngine;
using UnityEngine.UI;

public class GameManager : Singleton<GameManager>
{
    [SerializeField] private GameObject _playerPrefab;
    public GameObject PlayerPrefab => _playerPrefab;

    public Inventory GainedInventory { get; private set; }
    public Inventory GiveInventory { get; private set; }

    public bool ShouldLoadPlayerInventory { get; private set; }
    public SpawnId PendingSpawnId { get; private set; }

    public void SetLoadPlayerInventory(bool load) => ShouldLoadPlayerInventory = load;
    public void SetSpawnId(SpawnId id) => PendingSpawnId = id;

    public void SaveChangeInventorys(Inventory give, Inventory gained)
    {
        GiveInventory = give;
        GainedInventory = gained;
    }
}
