using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SkillEquipRowUI : MonoBehaviour
{
    [SerializeField] private Image _icon;
    [SerializeField] private TextMeshProUGUI _nameText;
    [SerializeField] private TextMeshProUGUI _descriptionText;
    [SerializeField] private TextMeshProUGUI _cooldownText;
    [SerializeField] private Button _equipButton;
    [SerializeField] private TextMeshProUGUI _equipButtonText;

    private PlayerSkillData _data;
    private Action<PlayerSkillData> _onEquip;

    private void Awake()
    {
        if (_equipButton != null)
            _equipButton.onClick.AddListener(OnClickEquip);
    }

    public void Setup(PlayerSkillData data, Action<PlayerSkillData> onEquip)
    {
        _data = data;
        _onEquip = onEquip;

        if (_icon != null)
        {
            _icon.sprite = ResourceManager.Instance.LoadSprite(data.icon);
            _icon.enabled = _icon.sprite != null;
        }

        LocalizationManager localization = LocalizationManager.GetInstance();

        if (_nameText != null)
            _nameText.text = localization.GetString(data.name);
        if (_descriptionText != null)
            _descriptionText.text = localization.GetString(data.description);
        if (_cooldownText != null)
            _cooldownText.text = $"{data.cooldown:0.#}s";
    }

    public void SetEquipped(bool equipped)
    {
        if (_equipButton != null)
            _equipButton.interactable = !equipped;

        if (_equipButtonText != null)
            _equipButtonText.text = LocalizationManager.GetInstance()
                .GetString(equipped ? "skill.equipped" : "skill.equip");
    }

    private void OnClickEquip() => _onEquip?.Invoke(_data);
}
