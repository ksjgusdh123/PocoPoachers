using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 스킬 슬롯(SkillSlotUI) 호버 툴팁. 슬롯이 켜고 끄며, 내용만 채운다.
// 위치는 에디터에서 RectTransform으로 잡는다 (DescriptionUI와 같은 방식).
// 표시 항목이 아이템과 전혀 겹치지 않아 ItemInfoPanel을 상속하지 않는다.
// 슬롯이 FindAnyObjectByType<SkillDescriptionUI>로 찾으므로 씬에 하나만 둘 것.
public class SkillDescriptionUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _name;
    [SerializeField] private TextMeshProUGUI _description;
    [SerializeField] private TextMeshProUGUI _cooldown;
    [SerializeField] private Image _icon;

    // 비활성으로 배치해 두면 Awake는 첫 Show의 SetActive(true) 때 처음 돈다.
    // 그때 무조건 끄면 방금 켠 것을 도로 꺼버리므로, 표시 요청이 없었을 때만 끈다.
    private bool _shown;

    private void Awake()
    {
        ApplyAlwaysOnTopSorting();

        if (!_shown) gameObject.SetActive(false);
    }

    public void Show(PlayerSkillData data)
    {
        if (data == null) return;

        _shown = true;
        gameObject.SetActive(true);

        var localization = LocalizationManager.GetInstance();
        if (_name != null) _name.text = localization.GetString(data.name);
        if (_description != null) _description.text = FormatDescription(localization.GetString(data.description), data);
        if (_cooldown != null) _cooldown.text = $"{data.cooldown:0.#}s";

        if (_icon != null)
        {
            _icon.sprite = ResourceManager.Instance.LoadSprite(data.icon);
            _icon.enabled = _icon.sprite != null;   // 아이콘이 없는 스킬은 빈 흰 사각형이 남지 않게 끈다
        }
    }

    // 설명 문장에 스킬 수치를 끼워 넣는다. localization.csv의 desc에서 {0}~{5}로 참조한다.
    // 인자 순서 고정: 0=지속시간 1=위력/배율 2=속도 3=거리 4=반경 5=쿨타임.
    // 문장마다 필요한 번호만 쓰면 되고, 자리표시자가 없는 문장은 원문 그대로 나온다.
    // 한국어와 영어는 어순이 달라 값을 코드에서 이어붙이면 한쪽이 어색해진다 — 문장이 자리를 정하게 둔다.
    private static string FormatDescription(string text, PlayerSkillData data)
    {
        if (string.IsNullOrEmpty(text) || !text.Contains('{')) return text;

        try
        {
            return string.Format(text,
                Number(data.duration), Number(data.power), Number(data.speed),
                Number(data.distance), Number(data.radius), Number(data.cooldown));
        }
        catch (FormatException)
        {
            // 번역문에 짝이 안 맞는 중괄호가 섞이면 예외가 난다 — 툴팁을 비우지 말고 원문을 보여준다
            Debug.LogWarning($"[SkillDescriptionUI] 설명 서식이 잘못되었습니다: {text}");
            return text;
        }
    }

    // 8.0 → "8", 0.25 → "0.25" (정수는 소수점을 붙이지 않는다)
    private static string Number(float value) => value.ToString("0.##");

    public void Hide()
    {
        _shown = false;
        gameObject.SetActive(false);
    }

    // 툴팁은 형제 순서만으로는 열린 패널을 못 이긴다 — UIManager가 패널 Canvas의 sortingOrder를 올려 잡기 때문.
    // (ItemInfoPanel.ApplyAlwaysOnTopSorting과 같은 이유·같은 방식)
    private void ApplyAlwaysOnTopSorting()
    {
        if (!TryGetComponent(out Canvas canvas))
            canvas = gameObject.AddComponent<Canvas>();

        canvas.overrideSorting = true;
        canvas.sortingOrder = UIManager.TooltipSortingOrder;
    }
}
