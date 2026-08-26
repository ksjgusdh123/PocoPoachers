using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SkillSlotUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Image _icon;
    [SerializeField] private Image _cooldownOverlay;
    [SerializeField] private TextMeshProUGUI _cooldownText;
    [SerializeField] private TextMeshProUGUI _keyLabel;

    [SerializeField, Tooltip("스킬이 장착되지 않은 슬롯에 표시할 이미지")]
    private Sprite _emptyIcon;

    [SerializeField, Tooltip("빈 슬롯 아이콘에 곱할 색. RGB를 낮추면 어두워진다.")]
    private Color _emptyIconTint = new Color(0.45f, 0.45f, 0.45f, 0.7f);

    // 호버 툴팁에 넘길 현재 스킬 (빈 슬롯이면 null)
    private IPlayerSkill _skill;
    private SkillDescriptionUI _description;

    private void Awake()
    {
        if (_cooldownOverlay == null) return;

        // 어두운 오버레이가 12시에서 반시계로 남아야, 밝아지는 쪽이 시계방향으로 늘어난다
        _cooldownOverlay.type = Image.Type.Filled;
        _cooldownOverlay.fillMethod = Image.FillMethod.Radial360;
        _cooldownOverlay.fillOrigin = (int)Image.Origin360.Top;
        _cooldownOverlay.fillClockwise = false;
        _cooldownOverlay.raycastTarget = false;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_skill == null) return;   // 빈 슬롯은 설명할 게 없다

        Description()?.Show(_skill.Data);
    }

    public void OnPointerExit(PointerEventData eventData) => Description()?.Hide();

    private SkillDescriptionUI Description() =>
        _description ??= FindAnyObjectByType<SkillDescriptionUI>(FindObjectsInactive.Include);

    public void SetSkill(IPlayerSkill skill)
    {
        bool hasSkill = skill != null;
        _skill = skill;

        if (_icon != null)
        {
            _icon.sprite = hasSkill ? ResourceManager.Instance.LoadSprite(skill.Data.icon) : _emptyIcon;
            _icon.color = hasSkill ? Color.white : _emptyIconTint;
            _icon.enabled = _icon.sprite != null;
        }

        if (!hasSkill)
            SetCooldown(0f, 0f);
    }

    public void SetKeyLabel(string text)
    {
        if (_keyLabel != null)
            _keyLabel.text = text;
    }

    public void SetCooldown(float remaining, float total)
    {
        float ratio = total > 0f ? Mathf.Clamp01(remaining / total) : 0f;

        if (_cooldownOverlay != null)
        {
            _cooldownOverlay.fillAmount = ratio;
            _cooldownOverlay.enabled = ratio > 0f;
        }

        if (_cooldownText != null)
            _cooldownText.text = remaining > 0f ? Mathf.CeilToInt(remaining).ToString() : "";
    }
}
