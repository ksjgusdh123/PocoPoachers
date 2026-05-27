using UnityEngine;

public class Bootstrapper : MonoBehaviour
{
    private void Awake()
    {
        GameManager.GetInstance();
        SceneLoader.GetInstance();
    }
}
