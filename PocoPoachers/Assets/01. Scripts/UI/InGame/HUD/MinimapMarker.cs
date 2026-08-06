using UnityEngine;

// 미니맵 RawImage 위에 대상(보통 로컬 플레이어)의 현재 위치를 마커로 표시한다.
// mapRect은 미니맵 이미지의 RectTransform, markerRect은 그 자식으로 둔 마커 아이콘의 RectTransform이어야 한다.
public class MinimapMarker : MonoBehaviour
{
    [SerializeField] private MinimapCaptureData _mapData;
    [SerializeField] private RectTransform _mapRect;
    [SerializeField] private RectTransform _markerRect;
    [SerializeField] private Transform _target;

    private void LateUpdate()
    {
        if (_mapData == null || _mapRect == null || _markerRect == null || _target == null) return;
        if (_mapData.WorldSize <= 0f) return;

        float half = _mapData.WorldSize * 0.5f;
        float u = (_target.position.x - (_mapData.WorldCenter.x - half)) / _mapData.WorldSize;
        float v = (_target.position.z - (_mapData.WorldCenter.z - half)) / _mapData.WorldSize;

        Rect rect = _mapRect.rect;
        _markerRect.anchoredPosition = new Vector2((u - 0.5f) * rect.width, (v - 0.5f) * rect.height);
    }
}
