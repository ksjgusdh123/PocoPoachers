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
        LoadTargetScene();
    }

    public void OnInteractExit(PlayerController player) { }

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
