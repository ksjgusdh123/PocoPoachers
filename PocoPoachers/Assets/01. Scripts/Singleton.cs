using UnityEngine;

public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    protected static T instance;
    private static bool bIsDestroy = false;
    public static T GetInstance()
    {
        if (bIsDestroy) return null;

        if (instance == null)
        {
            GameObject go = GameObject.Find("[Managers]");
            if (null == go) go = new GameObject("[Managers]");
            instance = go.AddComponent<T>();
            DontDestroyOnLoad(go);
        }
        return instance;
    }

    protected virtual void Awake()
    {
    }

    private void OnDestroy()
    {
        bIsDestroy = true;
    }
}