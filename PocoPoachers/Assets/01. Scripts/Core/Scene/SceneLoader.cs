using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : Singleton<SceneLoader>
{
    public string TargetSceneName { get; private set; }

    public void LoadTitleScene()
    {
        RoomManager.Instance?.LeaveRoom();
        NetworkManager.Instance?.LeaveGame();
        Cursor.visible   = true;
        Cursor.lockState = CursorLockMode.None;
        LoadViaLoadingScreen(SceneName.Title);
    }

    public void LoadShelterScene() => LoadViaLoadingScreen(SceneName.Shelter);
    public void LoadRaidTestScene() => LoadViaLoadingScreen(SceneName.RaidTest);
    public void LoadPlanetScene(int planetId) => LoadViaLoadingScreen($"SC_Raid_{planetId}");

    private void LoadViaLoadingScreen(string targetSceneName)
    {
        TargetSceneName = targetSceneName;
        SceneManager.LoadScene(SceneName.Loading);
    }
}
