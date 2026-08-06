using UnityEngine;
using UnityEngine.EventSystems;

// 미니맵을 좌클릭하면 클릭한 위치에 핑 아이콘을 새로 만들어 남긴다. 로컬 전용 — 다른 플레이어에겐 안 보인다.
// 클릭할 때마다 기존 핑은 그대로 두고 새 핑이 계속 추가된다(자동으로 안 없어짐).
// 미니맵 이미지(RawImage) 오브젝트에 붙여야 한다 (raycastTarget 켜져 있어야 클릭을 받음).
// 핑 종류가 여러 개면 왼쪽 선택 버튼들의 OnClick()에서 SelectType(인덱스)를 호출해 고르게 한다.
public class MinimapPing : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private RectTransform _mapRect;
    [SerializeField] private RectTransform[] _pingIconPrefabs; // 종류별 프리팹. 0번이 기본 선택값

    private int _selectedType;

    // 선택 버튼의 OnClick()에 연결 — 인덱스는 인스펙터에서 직접 지정
    public void SelectType(int index)
    {
        if (_pingIconPrefabs == null || index < 0 || index >= _pingIconPrefabs.Length) return;
        _selectedType = index;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;
        if (_mapRect == null || _pingIconPrefabs == null || _pingIconPrefabs.Length == 0) return;

        RectTransform prefab = _pingIconPrefabs[_selectedType];
        if (prefab == null) return;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_mapRect, eventData.position, eventData.pressEventCamera, out Vector2 localPoint))
            return;

        RectTransform ping = Instantiate(prefab, _mapRect);
        ping.anchoredPosition = localPoint;
    }
}
