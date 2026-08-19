using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// EnhancementStatUI가 스탯 개수만큼 Instantiate하는 한 행 — (이름 / -버튼 / 10칸 블록바 / ＋버튼).
// 프리팹 자체에는 NameText/MinusButton/PlusButton/Blocks만 한 번 연결해두면 되고, 스탯별 내용은 Setup()으로 주입한다.
public class EnhancementStatRowUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _nameText;
    [SerializeField] private Button _minusButton;
    [SerializeField] private Button _plusButton;

    [Tooltip("왼쪽부터 채워지는 10칸 블록. 반드시 10개를 순서대로 연결한다.")]
    [SerializeField] private Image[] _blocks;

    public EnhancementStatType StatType { get; private set; }

    public void Setup(EnhancementStatType statType, string displayName, Action onPlusClicked, Action onMinusClicked)
    {
        StatType = statType;

        if (_nameText != null)
            _nameText.text = displayName;

        if (_plusButton != null)
        {
            _plusButton.onClick.RemoveAllListeners();
            _plusButton.onClick.AddListener(() => onPlusClicked?.Invoke());
        }

        if (_minusButton != null)
        {
            _minusButton.onClick.RemoveAllListeners();
            _minusButton.onClick.AddListener(() => onMinusClicked?.Invoke());
        }
    }

    public void SetFilled(int filledCount)
    {
        if (_blocks == null) return;

        Color filledColor = UITheme.AccentColor;
        Color emptyColor = UITheme.InkMuted;

        for (int i = 0; i < _blocks.Length; i++)
        {
            if (_blocks[i] == null) continue;
            _blocks[i].color = i < filledCount ? filledColor : emptyColor;
        }
    }

    public void SetInteractable(bool canPlus, bool canMinus)
    {
        if (_plusButton != null)
            _plusButton.interactable = canPlus;

        if (_minusButton != null)
            _minusButton.interactable = canMinus;
    }
}
