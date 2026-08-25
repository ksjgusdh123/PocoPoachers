using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 지속 중인 스킬 하나를 나타내는 칸. 남은 시간이 줄어들수록 12시부터 시계방향으로 어두워진다.
public class SkillBuffEntryUI : MonoBehaviour
{
    [SerializeField] private Image _icon;
    [SerializeField] private Image _durationOverlay;
    [SerializeField] private TextMeshProUGUI _timeText;

    // 매 프레임 스프라이트를 다시 로드하지 않기 위한 캐시
    private int _shownSkillId = -1;

    private void Awake()
    {
        if (_durationOverlay == null) return;

        // 어두운 부분이 12시부터 시계방향으로 늘어난다.
        // 쿨다운 오버레이(SkillSlotUI)는 반대로 걷히는 방향이라 fillClockwise가 다르다.
        _durationOverlay.type = Image.Type.Filled;
        _durationOverlay.fillMethod = Image.FillMethod.Radial360;
        _durationOverlay.fillOrigin = (int)Image.Origin360.Top;
        _durationOverlay.fillClockwise = true;
        _durationOverlay.raycastTarget = false;
    }

    public void Show(IPlayerSkill skill, float remaining, float duration)
    {
        if (!gameObject.activeSelf) gameObject.SetActive(true);

        if (_icon != null && _shownSkillId != skill.Data.id)
        {
            _icon.sprite = ResourceManager.Instance.LoadSprite(skill.Data.icon);
            _icon.enabled = _icon.sprite != null;
        }
        _shownSkillId = skill.Data.id;

        if (_durationOverlay != null)
            _durationOverlay.fillAmount = duration > 0f ? Mathf.Clamp01(1f - remaining / duration) : 1f;

        if (_timeText != null)
            _timeText.text = Mathf.CeilToInt(remaining).ToString();
    }

    public void Hide()
    {
        _shownSkillId = -1;

        if (gameObject.activeSelf) gameObject.SetActive(false);
    }
}
