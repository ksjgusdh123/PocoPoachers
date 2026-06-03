using UnityEngine;
using UnityEngine.UI;

// ── Inspector 연결 구조 ──────────────────────────────────────────────────
//  PanelSaveSlots [SaveSlotPanelUI]
//  ├── ScrollRect
//  │   └── Content  (Vertical Layout Group)  ← _content
//  └── Btn_Close    [Button]
// ────────────────────────────────────────────────────────────────────────

public class SaveSlotPanelUI : MonoBehaviour
{
    [SerializeField] private Transform          _content;
    [SerializeField] private SaveSlotButtonUI   _slotPrefab;
    [SerializeField] private Button             _btnClose;

    private void Awake()
    {
        _btnClose?.onClick.AddListener(() => gameObject.SetActive(false));
    }

    private void OnEnable() => Rebuild();

    private void Rebuild()
    {
        foreach (Transform child in _content)
            Destroy(child.gameObject);

        foreach (int idx in SaveManager.GetInstance().GetAllSlotIndices())
        {
            var slot = Instantiate(_slotPrefab, _content);
            slot.Init(idx);
        }
    }
}
