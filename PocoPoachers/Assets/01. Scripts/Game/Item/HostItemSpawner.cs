using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 호스트가 게임 시작 시 아이템 박스를 스폰하고 게스트에게 H_ItemSpawn을 전송합니다.
/// Inspector에서 박스 위치·회전·아이템 ID를 설정하세요.
/// </summary>
public class HostItemSpawner : MonoBehaviour
{
    [System.Serializable]
    public class BoxConfig
    {
        public Vector3 Position;
        public float Rotation;
        public int[] ItemIds;
    }

    [SerializeField] BoxConfig[] _boxes;

    int _nextUid = 1000;
    readonly List<H_ItemSpawnT> _spawnedBoxes = new();
    bool _spawned;

    void Start()
    {
        var rmgr = RoomManager.Instance;
        if (rmgr != null)
        {
            rmgr.OnGameStarted += HandleGameStarted;
            rmgr.OnRoomJoined  += HandleRoomJoined;
        }
    }

    void OnDestroy()
    {
        var rmgr = RoomManager.Instance;
        if (rmgr != null)
        {
            rmgr.OnGameStarted -= HandleGameStarted;
            rmgr.OnRoomJoined  -= HandleRoomJoined;
        }
    }

    // 방 생성 즉시 (호스트) 또는 방장 연결 완료 시 (게스트) 발동
    void HandleGameStarted()
    {
        if (!RoomManager.IsHost || _spawned) return;
        SpawnInitBoxes();
        _spawned = true;
    }

    // 새 게스트가 참여할 때마다 현재 박스 상태 동기화
    void HandleRoomJoined()
    {
        if (!RoomManager.IsHost) return;

        int newGuestId = RoomManager.LastGuestId;
        foreach (var data in _spawnedBoxes)
        {
            if (newGuestId != 0)
                PacketBuilder.SendToGuest(newGuestId, data, H_ItemSpawn.Pack, PacketType.H_ItemSpawn);
            else
                PacketBuilder.BroadcastToGuests(data, H_ItemSpawn.Pack, PacketType.H_ItemSpawn);
        }
    }
    public void SpawnInitBoxes()
    {
        if (!RoomManager.IsHost || _boxes == null) return;

        var omgr = ObjectManager.Instance;

        foreach (var cfg in _boxes)
        {
            int uid = _nextUid++;
            var boxTypeId = 301;

            var data = new H_ItemSpawnT
            {
                Uid = uid,
                TypeId = boxTypeId,
                Pos = new Vec3T { X = cfg.Position.x, Y = cfg.Position.y, Z = cfg.Position.z },
                Rotation = cfg.Rotation,
                ItemIds = new List<int>(cfg.ItemIds),
            };
            _spawnedBoxes.Add(data);

            omgr?.SpawnItemBox(uid, boxTypeId, cfg.Position, cfg.Rotation)
              ?.Initialize(cfg.ItemIds);

            PacketBuilder.BroadcastToGuests(data, H_ItemSpawn.Pack, PacketType.H_ItemSpawn);
        }
        Debug.Log($"[HostItemSpawner] 아이템 박스 스폰 완료!");
    }

    public void ResetSpawnState()
    {
        _spawnedBoxes.Clear();
        _spawned = false;
        _nextUid = 1000;
    }
}