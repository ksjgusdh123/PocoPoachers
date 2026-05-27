using UnityEngine.SceneManagement;

public class SceneLoader : Singleton<SceneLoader>
{
    protected override void Awake()
    {
        base.Awake();
        RoomManager.Instance.OnGameStarted += LoadGameScene;
    }

    protected override void OnDestroy()
    {
        if (RoomManager.Instance != null)
            RoomManager.Instance.OnGameStarted -= LoadGameScene;
        base.OnDestroy();
    }

    public void LoadLobbyScene() => SceneManager.LoadScene(SceneName.LobbyScene);
    public void LoadGameScene()  => SceneManager.LoadScene(SceneName.GameScene);
}
