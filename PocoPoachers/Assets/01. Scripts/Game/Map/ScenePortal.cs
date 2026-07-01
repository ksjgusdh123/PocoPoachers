using UnityEngine;

public class ScenePortal : MonoBehaviour, IInteractable
{
    public enum TargetScene { Shelter, RaidTest, Title }

    [SerializeField] private TargetScene _targetScene = TargetScene.Shelter;
    [SerializeField] private SpawnId _spawnId = SpawnId.None;

    public void OnInteract(PlayerController player)
    {
        SoundManager.GetInstance().PlaySfx("sfx_portal");

        GameManager.Instance.SetSpawnId(_spawnId);
        BroadcastSceneIfHost();
        LoadTargetScene();
    }

    public void OnInteractExit(PlayerController player) { }

    private void BroadcastSceneIfHost()
    {
        if (!RoomManager.IsHost || !RoomManager.HasGuests) return;

        string sceneName = _targetScene switch
        {
            TargetScene.Shelter  => SceneName.Shelter,
            TargetScene.RaidTest => SceneName.RaidTest,
            _                    => null,
        };
        if (sceneName == null) return;

        PacketBuilder.BroadcastReliableToGuests(
            new H_LoadSceneT { SceneName = sceneName, SpawnId = (int)_spawnId },
            H_LoadScene.Pack, PacketType.H_LoadScene);
    }

    private void LoadTargetScene()
    {
        switch (_targetScene)
        {
            case TargetScene.Shelter:  SceneLoader.Instance.LoadShelterScene();  break;
            case TargetScene.RaidTest: SceneLoader.Instance.LoadRaidTestScene(); break;
            case TargetScene.Title:    SceneLoader.Instance.LoadTitleScene();    break;
        }
    }
}
