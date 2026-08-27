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

    [SerializeField, Tooltip("해금 전 자물쇠 표시. 없으면 표시하지 않는다.")]
    private GameObject _lockedMark;

    [SerializeField, Tooltip("해금 전 아이콘/이름에 씌울 색")]
    private Color _lockedTint = new(0.45f, 0.45f, 0.45f, 1f);

    [SerializeField] private Color _normalColor = new(0.055f, 0.259f, 0.341f, 0.2f);
    [SerializeField] private Color _selectedColor = new(0.176f, 0.541f, 0.639f, 0.45f);

    private PlayerSkillData _data;
    private Action<PlayerSkillData> _onSelected;
    private Color _iconColor = Color.white;
    private Color _nameColor = Color.white;
    private bool _tintCached;
    private bool _locked;
    private ThemedTextUI _nameTheme;

    private void Awake()
    {
        GetComponent<Button>()?.onClick.AddListener(OnClick);
        if (_background == null) _background = GetComponent<Image>();
    }

    // 목록은 창이 닫힌 상태에서 만들어진다 — 비활성일 때 칠한 색은 첫 활성화에서 유실되므로 여기서 다시 칠한다.
    private void OnEnable() => ApplyLockTint();

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

    // 잠긴 스킬도 목록에는 남겨 해금 조건을 볼 수 있게 하고, 대신 흐리게 표시한다.
    public void SetLocked(bool locked)
    {
        _locked = locked;
        ApplyLockTint();
    }

    private void ApplyLockTint()
    {
        // 원래 색은 잠금 색을 덮어쓰기 전에 잡아야 한다. Awake는 창이 처음 열릴 때까지 돌지 않아
        // 그 시점엔 이미 잠금 색이 칠해져 있을 수 있으므로 첫 호출에서 캐시한다.
        if (!_tintCached)
        {
            _tintCached = true;
            if (_icon != null) _iconColor = _icon.color;
            if (_nameText != null)
            {
                _nameColor = _nameText.color;
                _nameTheme = _nameText.GetComponent<ThemedTextUI>();
            }
        }

        if (_lockedMark != null) _lockedMark.SetActive(_locked);
        if (_icon != null) _icon.color = _locked ? _lockedTint : _iconColor;
        if (_nameText == null) return;

        // 이름 텍스트는 ThemedTextUI가 OnEnable마다 테마 색을 다시 칠한다 — 잠긴 동안에는
        // 색 적용을 넘겨받고, 해제할 때 테마가 원래 색을 되돌리게 한다.
        bool themeOwnsColor = _nameTheme != null && _nameTheme.ColorRole != UITheme.TextColorRole.None;
        if (_nameTheme != null) _nameTheme.SetColorOverride(_locked);

        if (_locked) _nameText.color = _lockedTint;
        else if (!themeOwnsColor) _nameText.color = _nameColor;
    }

    public void SetSelected(bool selected)
    {
        if (_background != null)
            _background.color = selected ? _selectedColor : _normalColor;
    }

    private void OnClick() => _onSelected?.Invoke(_data);
}
