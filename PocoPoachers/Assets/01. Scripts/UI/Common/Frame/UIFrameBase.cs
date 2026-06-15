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

    public void SetTitle(string title) => _txtTitle.text = title;
}
