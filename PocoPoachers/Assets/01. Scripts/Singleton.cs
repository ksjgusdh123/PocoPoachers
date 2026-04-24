using UnityEngine;

public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    protected static T _instance;
    private static bool _isQuitting;

    public static T Instance => GetInstance();

    public static T GetInstance()
    {
        if (_isQuitting)
            return null;

        if (_instance == null)
            _instance = FindAnyObjectByType<T>(FindObjectsInactive.Exclude);

        if (_instance == null)
        {
            GameObject go = new GameObject($"[{typeof(T).Name}]");
            _instance = go.AddComponent<T>();
            DontDestroyOnLoad(go);
        }
        return _instance;
    }

    protected virtual void Awake()
    {
        if (_isQuitting)
            return;

        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this as T;
    }

    protected virtual void OnApplicationQuit()
    {
        _isQuitting = true;
    }

    protected virtual void OnDestroy()
    {
        if (_instance == this)
            _instance = null;
    }
}
