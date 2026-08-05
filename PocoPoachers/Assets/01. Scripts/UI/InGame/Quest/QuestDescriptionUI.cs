using TMPro;
using UnityEngine;

// 퀘스트 상세 표시 패널. QuestListUI가 목록에서 항목을 고르면 SetQuest로 내용을 채운다.
public class QuestDescriptionUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _questNameText;
    [SerializeField] private TextMeshProUGUI _questNpcNameText;
    [SerializeField] private TextMeshProUGUI _questDescriptionText;
    [SerializeField] private TextMeshProUGUI _questGoalText;
    [SerializeField] private TextMeshProUGUI _questRewardText;

    public void SetQuest(QuestData data)
    {
        if (data == null)
        {
            Clear();
            return;
        }

        if (_questNameText != null) _questNameText.text = data.QuestName;
        if (_questNpcNameText != null) _questNpcNameText.text = data.NpcName;
        if (_questDescriptionText != null) _questDescriptionText.text = data.Description;
        if (_questGoalText != null) _questGoalText.text = data.Goal;
        if (_questRewardText != null) _questRewardText.text = data.Reward;
    }

    public void Clear()
    {
        if (_questNameText != null) _questNameText.text = "-";
        if (_questNpcNameText != null) _questNpcNameText.text = "-";
        if (_questDescriptionText != null) _questDescriptionText.text = "-";
        if (_questGoalText != null) _questGoalText.text = "-";
        if (_questRewardText != null) _questRewardText.text = "-";
    }
}
