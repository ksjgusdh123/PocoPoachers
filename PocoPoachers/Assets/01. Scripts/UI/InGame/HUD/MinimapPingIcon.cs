using UnityEngine;
using UnityEngine.EventSystems;

// 핑 아이콘 프리팹에 붙인다. 아이콘을 클릭하면 스스로 지워져서,
// 빈 곳 클릭(MinimapPing)은 새 핑을 남기고 이미 있는 핑을 클릭하면 지우는 토글처럼 동작한다.
public class MinimapPingIcon : MonoBehaviour, IPointerClickHandler
{
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;
        Destroy(gameObject);
    }
}
