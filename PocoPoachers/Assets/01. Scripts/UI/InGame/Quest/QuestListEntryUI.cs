using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 퀘스트 목록 항목 하나(버튼). QuestListUI가 템플릿으로 Instantiate해 재사용한다.
public class QuestListEntryUI : MonoBehaviour
{
    [SerializeField] private Button _button;
    [SerializeField] private TextMeshProUGUI _nameText;

    private QuestData _data;

    private void Awake()
    {
        if (_button == null) _button = GetComponent<Button>();
        if (_nameText == null) _nameText = GetComponentInChildren<TextMeshProUGUI>(true);
    }

    public void Setup(QuestData data, Action<QuestData, QuestListEntryUI> onClick)
    {
        _data = data;
        if (_nameText != null) _nameText.text = data.QuestName;

        _button.onClick.RemoveAllListeners();
        _button.onClick.AddListener(() => onClick?.Invoke(_data, this));
    }

    public void SetSelected(bool selected)
    {
        if (_nameText != null)
            _nameText.color = selected ? UITheme.InkPrimary : UITheme.InkSecondary;
    }
}
