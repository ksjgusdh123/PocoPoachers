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

    // 결과 화면은 가벼운 씬이라 로딩 화면을 거치지 않는다 (한 프레임 스치듯 지나가면 오히려 거슬린다).
    public void LoadResultScene()
    {
        TargetSceneName = SceneName.Result;
        ObjectManager.Instance?.Clear();
        SceneManager.LoadScene(SceneName.Result);
    }

    public void LoadRaidTestScene() => LoadViaLoadingScreen(SceneName.RaidTest);
    public void LoadPlanetScene(int planetId) => LoadViaLoadingScreen($"SC_Raid_{planetId}");

    private void LoadViaLoadingScreen(string targetSceneName)
    {
        TargetSceneName = targetSceneName;
        ObjectManager.Instance?.Clear();
        SceneManager.LoadScene(SceneName.Loading);
    }
}
