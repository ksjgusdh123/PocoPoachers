using System.Collections.Generic;
using UnityEngine;

struct PendingRemoteMove
{
    public int PlayerId;
    public Vector3 Pos;
    public float Rotation;
    public sbyte MoveType;
}

public class NetObjectManager : Singleton<NetObjectManager>
{
    [SerializeField] NetObject _playerRemotePrefab;

    readonly Dictionary<(NetObjectKind kind, int id), NetObject> _netObjects = new Dictionary<(NetObjectKind, int), NetObject>();

    readonly object _pendingMovesLock = new object();
    readonly List<PendingRemoteMove> _pendingMoves = new List<PendingRemoteMove>();
    readonly List<PendingRemoteMove> _drainBuffer = new List<PendingRemoteMove>();

    public bool TryGet(NetObjectKind kind, int netId, out NetObject netObject)
    {
        return _netObjects.TryGetValue((kind, netId), out netObject);
    }

    public void Despawn(NetObjectKind kind, int netId)
    {
        var key = (kind, netId);
        if (!_netObjects.TryGetValue(key, out var netObj) || netObj == null)
            return;
        Destroy(netObj.gameObject);
        _netObjects.Remove(key);
    }

    void Update()
    {
        _drainBuffer.Clear();
        lock (_pendingMovesLock)
        {
            if (_pendingMoves.Count == 0)
                return;
            _drainBuffer.AddRange(_pendingMoves);
            _pendingMoves.Clear();
        }

        for (int i = 0; i < _drainBuffer.Count; i++)
        {
            PendingRemoteMove m = _drainBuffer[i];
            ApplyRemotePlayerMove(m.PlayerId, m.Pos, m.Rotation, m.MoveType);
        }
    }

    /// <summary>Recv 스레드에서도 호출 가능. 실제 반영은 <see cref="Update"/>에서 수행.</summary>
    public void QueueRemotePlayerMove(int playerId, Vector3 pos, float rotation, sbyte moveType)
    {
        lock (_pendingMovesLock)
        {
            _pendingMoves.Add(new PendingRemoteMove
            {
                PlayerId = playerId,
                Pos = pos,
                Rotation = rotation,
                MoveType = moveType,
            });
        }
    }

    void ApplyRemotePlayerMove(int playerId, Vector3 pos, float rotation, sbyte moveType)
    {
        var nm = NetworkManager.Instance;
        if (nm != null && playerId == nm.MyPlayerId)
            return;

        var key = (NetObjectKind.Player, playerId);
        if (!_netObjects.TryGetValue(key, out var netObj))
        {
            netObj = SpawnRemotePlayer(playerId);
            netObj.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, rotation, 0f));
            _netObjects.Add(key, netObj);
        }

        netObj.SetMoveTarget(pos, rotation);
    }

    public void ClearRemotePlayers()
    {
        lock (_pendingMovesLock)
            _pendingMoves.Clear();

        foreach (var kv in _netObjects)
        {
            if (kv.Value != null)
                Destroy(kv.Value.gameObject);
        }
        _netObjects.Clear();
    }

    NetObject SpawnRemotePlayer(int playerId)
    {
        GameObject go;
        NetObject netObj;

        if (_playerRemotePrefab != null)
        {
            go = Instantiate(_playerRemotePrefab.gameObject);
            netObj = go.GetComponent<NetObject>();
            if (netObj == null)
                netObj = go.AddComponent<NetObject>();
        }
        else
        {
            go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            var col = go.GetComponent<Collider>();
            if (col != null)
                Destroy(col);
            netObj = go.AddComponent<NetObject>();
        }

        go.name = $"Net_{NetObjectKind.Player}_{playerId}";
        netObj.Initialize(NetObjectKind.Player, playerId);
        return netObj;
    }
}
