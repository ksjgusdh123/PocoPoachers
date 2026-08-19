using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SkillSlotUI : MonoBehaviour
{
    [SerializeField] private Image _icon;
    [SerializeField] private Image _cooldownOverlay;
    [SerializeField] private TextMeshProUGUI _cooldownText;
    [SerializeField] private TextMeshProUGUI _keyLabel;

    [SerializeField, Tooltip("스킬이 장착되지 않은 슬롯에 표시할 이미지")]
    private Sprite _emptyIcon;

    [SerializeField, Tooltip("빈 슬롯 아이콘에 곱할 색. RGB를 낮추면 어두워진다.")]
    private Color _emptyIconTint = new Color(0.45f, 0.45f, 0.45f, 0.7f);

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

    public void SetSkill(IPlayerSkill skill)
    {
        bool hasSkill = skill != null;

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
