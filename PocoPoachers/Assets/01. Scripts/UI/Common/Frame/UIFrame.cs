using TMPro;
using UnityEngine;
using UnityEngine.UI;

// ── Inspector 연결 구조 ──────────────────────────────────────────────────
//  UIFrame
//  ├─ Dimmer        (Image)
//  └─ Panel         (Image)
//      ├─ Txt_Title (TextMeshProUGUI)
//      ├─ Btn_Close (Button)
//      └─ Content   (RectTransform)  ← 패널별 콘텐츠 배치 영역
// ────────────────────────────────────────────────────────────────────────

public class UIFrame : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _txtTitle;
    [SerializeField] private Button _btnClose;
    [SerializeField] private RectTransform _content;

    public Button BtnClose => _btnClose;
    public RectTransform Content => _content;

    public void SetTitle(string title) => _txtTitle.text = title;
}
