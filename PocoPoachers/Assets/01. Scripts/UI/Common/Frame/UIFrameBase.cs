using TMPro;
using UnityEngine;

// ── Inspector 연결 구조 (UIFrame / UIPopupFrame 공통) ──────────────────────
//  └─ Panel         (Image)
//      ├─ Txt_Title (TextMeshProUGUI)
//      └─ Content   (RectTransform)  ← 패널별 콘텐츠 배치 영역
// ────────────────────────────────────────────────────────────────────────

public abstract class UIFrameBase : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _txtTitle;
    [SerializeField] private RectTransform _content;

    public RectTransform Content => _content;

    public void SetTitle(string title)
    {
        if (_txtTitle == null)
        {
            Debug.LogWarning($"[{GetType().Name}] Txt_Title이 연결되지 않아 제목을 설정할 수 없습니다.", this);
            return;
        }

        _txtTitle.text = title;
    }
}
