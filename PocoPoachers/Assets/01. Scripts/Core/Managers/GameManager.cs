using UnityEngine;
using UnityEngine.UI;

public class GameManager : Singleton<GameManager>
{
    [SerializeField] private GameObject _playerPrefab;
    public GameObject PlayerPrefab => _playerPrefab;

    public Inventory GainedInventory { get; private set; }
    public Inventory GiveInventory { get; private set; }

    public SpawnId PendingSpawnId { get; private set; }
    public int SelectedPlanetId { get; private set; }

    public void SetSpawnId(SpawnId id) => PendingSpawnId = id;
    public void SetSelectedPlanet(int id) => SelectedPlanetId = id;

    public void SaveChangeInventorys(Inventory give, Inventory gained)
    {
        GiveInventory = give;
        GainedInventory = gained;
    }
}
