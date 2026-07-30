using System;
using System.Collections.Generic;
using UnityEngine;

public class MainThreadDispatcher : Singleton<MainThreadDispatcher>
{
    static readonly Queue<Action> _queue = new Queue<Action>();

    // lock 밖에서 실행하기 위한 버퍼. Update는 메인 스레드에서만 돌므로 재사용해도 안전하다.
    static readonly List<Action> _drain = new List<Action>();

    // 도메인 리로드를 끈 상태에서 Play를 반복하면 이전 세션의 액션(이미 파괴된 오브젝트 참조)이
    // 남아 MissingReferenceException을 일으키므로 진입 시 비운다.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        lock (_queue) _queue.Clear();
        _drain.Clear();
    }

    public static void Enqueue(Action action)
    {
        if (action == null) return;
        lock (_queue) _queue.Enqueue(action);
    }

    protected override void Awake()
    {
        base.Awake();
        if (_instance != this) return;

        // Singleton.GetInstance()로 생성된 경우 이미 DontDestroyOnLoad 상태다.
        // 씬에 배치된 인스턴스만 루트로 올려 부모 계층과 함께 파괴되지 않게 한다.
        if (transform.parent != null) transform.SetParent(null);
        DontDestroyOnLoad(gameObject);
    }

    void Update()
    {
        // lock을 잡은 채 Action을 실행하면, Action이 다른 스레드의 완료를 기다리는 동안
        // 그 스레드의 Enqueue가 차단되어 데드락이 된다. 큐를 비워 옮긴 뒤 lock 밖에서 실행한다.
        lock (_queue)
        {
            if (_queue.Count == 0) return;
            while (_queue.Count > 0)
                _drain.Add(_queue.Dequeue());
        }

        for (int i = 0; i < _drain.Count; i++)
        {
            try { _drain[i]?.Invoke(); }
            catch (Exception e) { Debug.LogException(e); }
        }
        _drain.Clear();
    }
}
