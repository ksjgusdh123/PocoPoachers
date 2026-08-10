using UnityEngine;
using UnityEngine.UI;

// 팀 이동 투표 팝업의 인원 아이콘 하나 (머리 + 몸통). 수락하면 초록으로 칠한다.
public class VoteMemberIconUI : MonoBehaviour
{
    static readonly Color PendingColor  = new Color(0.42f, 0.47f, 0.55f, 1f);
    static readonly Color AcceptedColor = new Color(0.29f, 0.85f, 0.45f, 1f);

    [SerializeField] private Image _head;
    [SerializeField] private Image _body;

    public void SetAccepted(bool accepted)
    {
        Color color = accepted ? AcceptedColor : PendingColor;
        if (_head != null) _head.color = color;
        if (_body != null) _body.color = color;
    }
}
