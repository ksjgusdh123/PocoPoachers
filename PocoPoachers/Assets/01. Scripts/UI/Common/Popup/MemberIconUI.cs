using UnityEngine;
using UnityEngine.UI;

// 팀 인원 표시용 아이콘 하나 (머리 + 몸통). 조건을 만족한 인원은 초록으로 칠한다.
// 이동 투표에서는 "수락함", 탈출 구역에서는 "구역 안에 있음"을 뜻한다.
public class MemberIconUI : MonoBehaviour
{
    static readonly Color IdleColor = new Color(0.42f, 0.47f, 0.55f, 1f);
    static readonly Color DoneColor = new Color(0.29f, 0.85f, 0.45f, 1f);

    [SerializeField] private Image _head;
    [SerializeField] private Image _body;

    public void SetHighlighted(bool highlighted)
    {
        Color color = highlighted ? DoneColor : IdleColor;
        if (_head != null) _head.color = color;
        if (_body != null) _body.color = color;
    }
}
