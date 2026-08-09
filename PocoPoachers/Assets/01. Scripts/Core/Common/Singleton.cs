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

    // 이미 살아있는 인스턴스만 돌려주고, 없으면 만들지 않는다.
    // OnDisable/OnDestroy에서 구독을 해제할 때는 반드시 이쪽을 써야 한다. 정리 단계에서 GetInstance를
    // 부르면 씬이 닫히는 도중에 새 오브젝트가 생겨 "Some objects were not cleaned up" 경고가 난다.
    public static T ExistingInstance => SingletonRuntimeState.IsQuitting || _instance == null ? null : _instance;

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
