using TMPro;
using UnityEngine;

// 미니맵 위에 표시되는 플레이어 한 명의 마커. MinimapMarkerSpawner가 플레이어 수만큼 인스턴스화한다.
public class PlayerMarker : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _nameText;

    private RectTransform _rect;
    private RectTransform _mapRect;
    private MinimapCaptureData _mapData;
    private Transform _target;
    private int _playerId;

    public void Init(MinimapCaptureData mapData, RectTransform mapRect, Transform target, int playerId)
    {
        _rect = (RectTransform)transform;
        _mapData = mapData;
        _mapRect = mapRect;
        _target = target;
        _playerId = playerId;

        RefreshName();
    }

    // 명부는 마커보다 늦게 도착할 수 있다 (팀원 캐릭터가 이동 패킷으로 먼저 생김)
    private void OnEnable() => PlayerNameRegistry.OnChanged += RefreshName;
    private void OnDisable() => PlayerNameRegistry.OnChanged -= RefreshName;

    private void RefreshName()
    {
        if (_nameText != null)
            _nameText.text = PlayerNameRegistry.Get(_playerId);
    }

    private void LateUpdate()
    {
        if (_mapData == null || _mapRect == null || _target == null) return;
        if (_mapData.WorldSize <= 0f) return;

        float half = _mapData.WorldSize * 0.5f;
        float u = (_target.position.x - (_mapData.WorldCenter.x - half)) / _mapData.WorldSize;
        float v = (_target.position.z - (_mapData.WorldCenter.z - half)) / _mapData.WorldSize;

        Rect rect = _mapRect.rect;
        _rect.anchoredPosition = new Vector2((u - 0.5f) * rect.width, (v - 0.5f) * rect.height);
    }
}
