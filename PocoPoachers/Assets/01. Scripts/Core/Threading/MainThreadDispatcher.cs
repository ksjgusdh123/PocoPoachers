using System;
using System.Collections.Generic;
using UnityEngine;

public class MainThreadDispatcher : Singleton<MainThreadDispatcher>
{
    static readonly Queue<Action> _queue = new Queue<Action>();

    public static void Enqueue(Action action)
    {
        if (action == null) return;
        lock (_queue) _queue.Enqueue(action);
    }

    protected override void Awake()
    {
        base.Awake();
        DontDestroyOnLoad(gameObject);
    }

    void Update()
    {
        lock (_queue)
        {
            while (_queue.Count > 0)
            {
                Action action = _queue.Dequeue();
                try { action?.Invoke(); }
                catch (Exception e) { Debug.LogException(e); }
            }
        }
    }
}
