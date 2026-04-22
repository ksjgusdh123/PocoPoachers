using System.Collections.Generic;
using UnityEngine;

public class NetObjectManager : MonoBehaviour
{
    [SerializeField] NetObject _playerRemotePrefab;

    readonly Dictionary<(NetObjectKind kind, int id), NetObject> _netObjects = new Dictionary<(NetObjectKind, int), NetObject>();

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

    public void ApplyRemotePlayerMove(int playerId, Vector3 pos, float rotation, sbyte moveType)
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
