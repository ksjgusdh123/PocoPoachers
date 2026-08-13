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

    public void LoadTutorialScene() => LoadViaLoadingScreen(SceneName.Tutorial);

    // 결과 화면은 가벼운 씬이라 로딩 화면을 거치지 않는다 (한 프레임 스치듯 지나가면 오히려 거슬린다).
    public void LoadResultScene() => LoadSceneDirect(SceneName.Result);

    // 로딩 화면을 건너뛰고 곧바로 대상 씬을 연다.
    // 게스트에 전파하지 않는 로컬 이동이므로, 팀이 함께 움직여야 하면 SceneTransition.Go를 쓸 것.
    public void LoadSceneDirect(string targetSceneName)
    {
        if (string.IsNullOrEmpty(targetSceneName)) return;

        TargetSceneName = targetSceneName;
        ObjectManager.Instance?.Clear();
        SceneManager.LoadScene(targetSceneName);
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
