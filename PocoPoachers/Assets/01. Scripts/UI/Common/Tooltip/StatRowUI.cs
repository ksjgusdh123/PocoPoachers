using TMPro;
using UnityEngine;

// DescriptionUI의 스탯 한 줄 — 라벨/값 텍스트를 가진 프리팹에 붙인다.
// 스탯 한 개마다 이 프리팹을 생성해 VerticalLayoutGroup 등으로 여러 줄을 만든다.
public class StatRowUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _label;
    [SerializeField] private TextMeshProUGUI _value;

    public void Set(string label, string value)
    {
        if (_label != null) _label.text = label;
        if (_value != null) _value.text = value;
    }
}
