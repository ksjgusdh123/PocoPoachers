using System.Collections.Generic;
using UnityEngine;

// 미니맵 위에 접속한 플레이어 수만큼 PlayerMarker를 만들어 관리한다.
// 로컬 플레이어는 ObjectManager에 없으므로(ApplyMove의 IsLocalPlayer) 따로 찾아서 추가한다.
public class MinimapMarkerSpawner : MonoBehaviour
{
    [SerializeField] private MinimapCaptureData _mapData;
    [SerializeField] private RectTransform _mapRect;
    [SerializeField] private PlayerMarker _markerPrefab;

    private readonly Dictionary<int, PlayerMarker> _markers = new();

    // 매 프레임 검사하지 않고, 미니맵이 열릴 때(MinimapUI.OnShow)만 한 번 호출된다
    public void Refresh()
    {
        RefreshLocalPlayer();
        RefreshRemotePlayers();
    }

    private void RefreshLocalPlayer()
    {
        NetworkManager nm = NetworkManager.Instance;
        if (nm == null) return;

        PlayerController local = FindFirstObjectByType<PlayerController>();
        if (local == null) return;

        EnsureMarker(nm.MyPlayerId, local.transform);
    }

    private void RefreshRemotePlayers()
    {
        if (ObjectManager.Instance == null) return;

        foreach (WorldObject obj in ObjectManager.Instance.GetAllByKind(ObjectKind.Player))
            EnsureMarker(obj.Id, obj.transform);
    }

    private void EnsureMarker(int id, Transform target)
    {
        if (_markers.ContainsKey(id)) return;
        if (_markerPrefab == null || _mapRect == null) return;

        PlayerMarker marker = Instantiate(_markerPrefab, _mapRect);
        marker.Init(_mapData, _mapRect, target, id);
        _markers[id] = marker;
    }

    private void OnDisable()
    {
        foreach (PlayerMarker marker in _markers.Values)
            if (marker != null) Destroy(marker.gameObject);
        _markers.Clear();
    }
}
