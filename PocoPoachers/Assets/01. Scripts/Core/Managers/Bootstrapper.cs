using UnityEngine;

public class Bootstrapper : MonoBehaviour
{
    private void Awake()
    {
        MainThreadDispatcher.GetInstance();
        RoomManager.GetInstance();
        GameManager.GetInstance();
        SceneLoader.GetInstance();
        SaveManager.GetInstance();
        ShelterManager.GetInstance();
        SoundManager.GetInstance();
        LocalizationManager.GetInstance();
        UISoundManager.GetInstance();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        CheatConsole.GetInstance();
#endif
    }
}
