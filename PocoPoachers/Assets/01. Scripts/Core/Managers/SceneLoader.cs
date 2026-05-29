using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : Singleton<SceneLoader>
{
    public void LoadLobbyScene()
    {
        NetworkManager.Instance?.LeaveGame();
        Cursor.visible   = true;
        Cursor.lockState = CursorLockMode.None;
        SceneManager.LoadScene(SceneName.LobbyScene);
    }

    public void LoadGameScene() => SceneManager.LoadScene(SceneName.GameScene);
}
