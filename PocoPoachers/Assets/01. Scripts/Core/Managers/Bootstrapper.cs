using UnityEngine;

public class Bootstrapper : MonoBehaviour
{
    private void Awake()
    {
        MainThreadDispatcher.GetInstance();
        RoomManager.GetInstance();
        GameManager.GetInstance();
        SceneLoader.GetInstance();
    }
}
