using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : Singleton<SceneLoader>
{
    public void LoadTitleScene()
    {
        RoomManager.Instance?.LeaveRoom();
        NetworkManager.Instance?.LeaveGame();
        Cursor.visible   = true;
        Cursor.lockState = CursorLockMode.None;
        SceneManager.LoadScene(SceneName.Title);
    }

    public void LoadShelterScene() => SceneManager.LoadScene(SceneName.Shelter);
    public void LoadRaidTestScene() => SceneManager.LoadScene(SceneName.RaidTest);
}
