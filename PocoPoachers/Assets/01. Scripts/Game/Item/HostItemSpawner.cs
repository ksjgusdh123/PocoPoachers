using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 호스트가 게임 시작 시 더미 아이템 박스를 스폰하고 게스트에게 H_ItemSpawn을 전송합니다.
/// Inspector에서 박스 위치·회전·아이템 ID를 설정하세요.
/// </summary>
public class HostItemSpawner : MonoBehaviour
{
    [System.Serializable]
    public class BoxConfig
    {
        public Vector3 Position;
        public float   Rotation;
        public int[]   ItemIds;
    }

    [SerializeField] BoxConfig[] _boxes;

    // 박스 UID는 1000번부터 할당
    int _nextUid = 1000;

    // 스폰된 박스 데이터 캐시 (나중에 연결되는 게스트에게 재전송)
    readonly List<H_ItemSpawnT> _spawnedBoxes = new();
    bool _spawned;

    void Start()
    {
        var nmgr = NetworkManager.Instance;
        if (nmgr != null) nmgr.OnSinglePlayerStarted += OnSinglePlayerStarted;

        var rmgr = RoomManager.Instance;
        if (rmgr != null) rmgr.OnRoomJoined += OnJoin;
    }

    void OnDestroy()
    {
        var nmgr = NetworkManager.Instance;
        if (nmgr != null) nmgr.OnSinglePlayerStarted -= OnSinglePlayerStarted;

        var rmgr = RoomManager.Instance;
        if (rmgr != null) rmgr.OnRoomJoined -= OnJoin;
    }

    void OnSinglePlayerStarted()
    {
        if (!_spawned)
        {
            SpawnAll();
            _spawned = true;
        }
    }

    void OnJoin()
    {
        var mgr = RoomManager.Instance;
        if (mgr == null || !mgr.IsHost) return;

        if (!_spawned)
        {
            SpawnAll();
            _spawned = true;
        }
        else
        {
            foreach (var data in _spawnedBoxes)
                mgr.SendToAllGuests(data, H_ItemSpawn.Pack, PacketType.H_ItemSpawn);
        }
    }

    public void SpawnAll()
    {
        if (_boxes == null) return;
        var rmgr = RoomManager.Instance;
        var omgr  = ObjectManager.Instance;

        foreach (var cfg in _boxes)
        {
            int uid = _nextUid++;

            var data = new H_ItemSpawnT
            {
                Uid      = uid,
                TypeId   = 301, // 상자 타입
                Pos      = new Vec3T { X = cfg.Position.x, Y = cfg.Position.y, Z = cfg.Position.z },
                Rotation = cfg.Rotation,
                ItemIds  = new List<int>(cfg.ItemIds),
            };
            _spawnedBoxes.Add(data);

            omgr?.SpawnItemBox(uid, 301, cfg.Position, cfg.Rotation)
              ?.Initialize(cfg.ItemIds);

            rmgr?.SendToAllGuests(data, H_ItemSpawn.Pack, PacketType.H_ItemSpawn);
        }
    }

    public void ResetSpawnState()
    {
        _spawnedBoxes.Clear();
        _spawned  = false;
        _nextUid  = 1000;
    }
}
