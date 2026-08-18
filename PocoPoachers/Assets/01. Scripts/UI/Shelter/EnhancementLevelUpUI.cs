using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 기체 레벨업 전용 패널 — 레벨, 비용, 레벨업 버튼만 다룬다. 스탯 강화는 EnhancementStatUI가 담당.
public class EnhancementLevelUpUI : MonoBehaviour
{
    private const float LevelUpPowerCost = 50f;

    [SerializeField] private TextMeshProUGUI _characterLevelText;
    [SerializeField] private TextMeshProUGUI _levelUpCostText;
    [SerializeField] private TextMeshProUGUI _levelUpPowerCostText;
    [SerializeField] private Button _levelUpButton;

    private PlayerEnhancement _playerEnhancement;

    private void Awake()
    {
        _levelUpButton?.onClick.AddListener(OnClickLevelUp);
    }

    private void OnEnable()
    {
        if (Generator.Instance != null)
            Generator.Instance.OnPowerChanged += HandlePowerChanged;
    }

    private void OnDisable()
    {
        if (Generator.Instance != null)
            Generator.Instance.OnPowerChanged -= HandlePowerChanged;
    }

    private void HandlePowerChanged(float current, float max) => Refresh();

    public void Open(PlayerEnhancement playerEnhancement)
    {
        _playerEnhancement = playerEnhancement;
        Refresh();
    }

    public void Refresh()
    {
        if (_playerEnhancement == null)
        {
            if (_characterLevelText != null) _characterLevelText.text = "-";
            if (_levelUpCostText != null) _levelUpCostText.text = "-";
            if (_levelUpButton != null) _levelUpButton.interactable = false;
            return;
        }

        if (_characterLevelText != null)
            _characterLevelText.text = $"Lv. {_playerEnhancement.CharacterLevel} / {_playerEnhancement.MaxCharacterLevel}";

        if (_levelUpCostText != null)
            _levelUpCostText.text = _playerEnhancement.GetCharacterLevelCostText();

        bool canAffordPower = Generator.Instance != null && Generator.Instance.CurrentPower >= LevelUpPowerCost;

        if (_levelUpPowerCostText != null)
        {
            _levelUpPowerCostText.text = string.Format(LocalizationManager.GetInstance().GetString("generator.power_cost_format"), LevelUpPowerCost.ToString("0"));
            _levelUpPowerCostText.color = canAffordPower ? UITheme.InkPositive : UITheme.InkNegative;
        }

        if (_levelUpButton != null)
            _levelUpButton.interactable = !_playerEnhancement.IsCharacterMaxLevel() && canAffordPower;
    }

    private void OnClickLevelUp()
    {
        if (_playerEnhancement == null) return;

        if (Generator.Instance == null || Generator.Instance.CurrentPower < LevelUpPowerCost)
        {
            var loc = LocalizationManager.GetInstance();
            UIManager.GetInstance().ShowNotice(loc.GetString("generator.title"), loc.GetString("generator.power_insufficient_message"));
            return;
        }

        if (!_playerEnhancement.TryLevelUpCharacter()) return;

        Generator.Instance.TryConsume(LevelUpPowerCost);
        Refresh();
    }
}
