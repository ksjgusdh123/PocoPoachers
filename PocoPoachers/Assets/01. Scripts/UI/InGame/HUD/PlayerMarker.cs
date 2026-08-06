using TMPro;
using UnityEngine;

// 미니맵 위에 표시되는 플레이어 한 명의 마커. MinimapMarkerSpawner가 플레이어 수만큼 인스턴스화한다.
public class PlayerMarker : MonoBehaviour
{
    // 닉네임 시스템이 아직 없어 대신 WorldObject.Id를 표시한다. 닉네임이 생기면 여기만 바꾸면 된다.
    [SerializeField] private TextMeshProUGUI _nameText;

    private RectTransform _rect;
    private RectTransform _mapRect;
    private MinimapCaptureData _mapData;
    private Transform _target;
    private int _lastId = int.MinValue;

    public void Init(MinimapCaptureData mapData, RectTransform mapRect, Transform target)
    {
        _rect = (RectTransform)transform;
        _mapData = mapData;
        _mapRect = mapRect;
        _target = target;
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

        UpdateNameText();
    }

    private void UpdateNameText()
    {
        if (_nameText == null || _target == null) return;

        var worldObject = _target.GetComponent<WorldObject>();
        if (worldObject == null || worldObject.Id == _lastId) return;

        _lastId = worldObject.Id;
        _nameText.text = _lastId.ToString();
    }
}
