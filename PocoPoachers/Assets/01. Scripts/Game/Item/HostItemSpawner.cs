using System.Collections.Generic;
using UnityEngine;

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

    void Start()
    {
        var rmgr = RoomManager.Instance;
        if (rmgr != null)
            rmgr.OnGameStarted += SpawnInitBoxes;
    }

    void OnDestroy()
    {
        var rmgr = RoomManager.Instance;
        if (rmgr != null)
            rmgr.OnGameStarted -= SpawnInitBoxes;
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

            omgr?.RegisterSpawnedBox(data);
            omgr?.SpawnItemBox(uid, boxTypeId, cfg.Position, cfg.Rotation)
              ?.Initialize(cfg.ItemIds);
        }
    }

    public void ResetSpawnState()
    {
        _nextUid = 1000;
    }
}
