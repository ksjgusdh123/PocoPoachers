using UnityEngine.SceneManagement;

public class SceneLoader : Singleton<SceneLoader>
{
    public void LoadLobbyScene() => SceneManager.LoadScene(SceneName.LobbyScene);
    public void LoadGameScene()  => SceneManager.LoadScene(SceneName.GameScene);
}
