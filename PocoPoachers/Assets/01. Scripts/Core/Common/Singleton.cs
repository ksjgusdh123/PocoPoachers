using UnityEngine;

// Singleton<T>의 종료 플래그 보관용 비제네릭 클래스.
// [RuntimeInitializeOnLoadMethod]는 열린 제네릭 타입의 정적 메서드를 호출하지 못하므로 분리했다.
// "앱 종료 중"은 타입별이 아니라 프로세스 전체 상태이므로 공용으로 두는 것이 의미상으로도 맞다.
internal static class SingletonRuntimeState
{
    public static bool IsQuitting;

    // Enter Play Mode Options에서 도메인 리로드를 끄면 정적 필드가 유지된다.
    // 리셋하지 않으면 Play→Stop→Play 시 IsQuitting이 true로 남아 모든 싱글톤이 null을 반환한다.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reset() => IsQuitting = false;
}

public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    protected static T _instance;

    public static T Instance => GetInstance();

    public static T GetInstance()
    {
        if (SingletonRuntimeState.IsQuitting)
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
        if (SingletonRuntimeState.IsQuitting)
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
        SingletonRuntimeState.IsQuitting = true;
    }

    protected virtual void OnDestroy()
    {
        if (_instance == this)
            _instance = null;
    }
}
