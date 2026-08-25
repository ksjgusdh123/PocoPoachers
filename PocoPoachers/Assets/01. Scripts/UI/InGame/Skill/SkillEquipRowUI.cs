using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 좌측 목록의 한 줄 — 아이콘과 이름만 보여주고, 클릭하면 우측 상세를 갱신하도록 알린다.
public class SkillEquipRowUI : MonoBehaviour
{
    [SerializeField] private Image _icon;
    [SerializeField] private TextMeshProUGUI _nameText;
    [SerializeField] private Image _background;

    [SerializeField, Tooltip("장착 중 표시. 없으면 표시하지 않는다.")]
    private GameObject _equippedMark;

    [SerializeField] private Color _normalColor = new(0.055f, 0.259f, 0.341f, 0.2f);
    [SerializeField] private Color _selectedColor = new(0.176f, 0.541f, 0.639f, 0.45f);

    private PlayerSkillData _data;
    private Action<PlayerSkillData> _onSelected;

    private void Awake()
    {
        GetComponent<Button>()?.onClick.AddListener(OnClick);
        if (_background == null) _background = GetComponent<Image>();
    }

    public void Setup(PlayerSkillData data, Action<PlayerSkillData> onSelected)
    {
        _data = data;
        _onSelected = onSelected;

        if (_icon != null)
        {
            _icon.sprite = ResourceManager.Instance.LoadSprite(data.icon);
            _icon.enabled = _icon.sprite != null;
        }

        if (_nameText != null)
            _nameText.text = LocalizationManager.GetInstance().GetString(data.name);
    }

    public void SetEquipped(bool equipped)
    {
        if (_equippedMark != null) _equippedMark.SetActive(equipped);
    }

    public void SetSelected(bool selected)
    {
        if (_background != null)
            _background.color = selected ? _selectedColor : _normalColor;
    }

    private void OnClick() => _onSelected?.Invoke(_data);
}
