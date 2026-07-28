using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DescriptionUI : MonoBehaviour
{
    // 장착 이력이 없어 WorldEquipmentManager에 기록이 없는 아이템의 기본 최대 내구도 (GunBase/ArmorBase 기본값과 동일)
    private const float DefaultMaxDurability = 100f;

    [SerializeField] private TextMeshProUGUI _name;
    [SerializeField] private TextMeshProUGUI _description;
    [SerializeField] private Image _icon;

    // 내구도가 있는 장비(무기/방어구)를 호버했을 때만 활성화되는 영역
    [SerializeField] private GameObject _durabilityRoot;
    [SerializeField] private Slider _durabilitySlider;
    [SerializeField] private TextMeshProUGUI _durabilityText;

    // 타입별 스탯 표기 영역 — 표기할 스탯이 있는 아이템(무기/파츠/방어구)에서만 활성화된다
    [SerializeField] private GameObject _statRoot;
    [SerializeField] private TextMeshProUGUI _statText;

    // 강화 단계 표기 영역 — 강화 가능 아이템(무기/파츠/방어구)에서만 활성화된다
    [SerializeField] private GameObject _enhancementRoot;
    [SerializeField] private TextMeshProUGUI _enhancementText;

    // 호버 중 실시간 갱신을 위해 구독해둔 대상 (다른 슬롯으로 옮기거나 닫을 때 해제)
    private EquippableItemBase _durabilityTarget;

    public void ShowDescription(ItemSlotUI slot)
    {
        if (!slot.IsSettedItem) return;
        // 아직 공개되지 않은(리빌 진행 중) 박스 슬롯은 설명 미표시
        if (slot.InventoryUI != null && slot.InventoryUI.IsSlotUnrevealed(slot.SlotIndex)) return;
        Show(slot.SlotItemData, slot.SlotUid);
    }

    // 장비 슬롯(EquipDropHandler) 등 ItemSlotUI가 아닌 곳에서 호버할 때 사용
    public void ShowDescription(ItemData data, int uid)
    {
        if (data == null) return;
        Show(data, uid);
    }

    // 고정 위치 툴팁 — 위치는 에디터에서 RectTransform으로 잡고, 여기선 내용만 채우고 켠다.
    private void Show(ItemData data, int uid)
    {
        if (!gameObject.activeSelf) gameObject.SetActive(true);
        _name.text = LocalizationManager.GetInstance().GetString(data.ItemName);
        _description.text = LocalizationManager.GetInstance().GetString(data.Description);
        if (_icon != null) _icon.sprite = ResourceManager.Instance.LoadSprite(data.icon);
        BindDurability(data, uid);
        BindStats(data, uid);
        BindEnhancement(data, uid);
    }

    public void HideDescription()
    {
        _name.text = "";
        _description.text = "";
        if (_icon != null) _icon.sprite = null;
        UnbindDurability();
        _statRoot?.SetActive(false);
        _enhancementRoot?.SetActive(false);
        gameObject.SetActive(false);
    }

    private void OnDestroy() => UnbindDurability();

    // 1) 장착 중이면 실제 인스턴스를 구독해 실시간 갱신
    // 2) 장착 해제 상태(인벤토리)면 WorldEquipmentManager에 저장된 값을 1회성으로 표시
    // 3) 한 번도 장착된 적 없으면(기록 없음) 기본 최대치를 풀로 표시
    private void BindDurability(ItemData data, int uid)
    {
        UnbindDurability();

        var target = EquippableItemBase.FindByUid(uid);
        if (target != null)
        {
            _durabilityTarget = target;
            _durabilityTarget.OnDurabilityChanged += RefreshDurability;
            RefreshDurability(target.CurrentDurability, target.MaxDurability);
            return;
        }

        if (!HasDurability(data)) return;

        if (!WorldEquipmentManager.TryGetDurability(uid, out float current, out float max))
        {
            current = DefaultMaxDurability;
            max = DefaultMaxDurability;
        }
        RefreshDurability(current, max);
    }

    private static bool HasDurability(ItemData data) =>
        data != null && data.ItemType is ItemType.Weapon or ItemType.Helmet or ItemType.Armor;

    private void UnbindDurability()
    {
        if (_durabilityTarget != null)
            _durabilityTarget.OnDurabilityChanged -= RefreshDurability;
        _durabilityTarget = null;
        _durabilityRoot?.SetActive(false);
    }

    private void RefreshDurability(float current, float max)
    {
        _durabilityRoot?.SetActive(true);
        if (_durabilitySlider != null)
            _durabilitySlider.value = max > 0f ? current / max : 0f;
        if (_durabilityText != null)
            _durabilityText.text = $"{Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";
    }

    // 표기할 스탯이 없는 타입(음식/재료 등)이면 영역을 통째로 숨겨 설명만 남긴다
    private void BindStats(ItemData data, int uid)
    {
        if (_statText == null) return;

        string text = BuildStatText(data, uid);
        _statText.text = text;
        _statRoot?.SetActive(!string.IsNullOrEmpty(text));
    }

    private static string BuildStatText(ItemData data, int uid) => data.ItemType switch
    {
        ItemType.Weapon => BuildGunStatText(data),
        ItemType.GunPart => BuildGunPartStatText(data, uid),
        ItemType.Helmet or ItemType.Armor => BuildArmorStatText(data, uid),
        _ => string.Empty,
    };

    private static bool IsEnhanceable(ItemType type) =>
        type is ItemType.Armor or ItemType.Helmet or ItemType.Weapon or ItemType.GunPart;

    // 강화 가능 아이템이면 전용 텍스트에 "강화: +N"을 표시(+0 포함), 그 외엔 영역을 숨긴다
    private void BindEnhancement(ItemData data, int uid)
    {
        if (_enhancementText == null) return;

        if (!IsEnhanceable(data.ItemType))
        {
            _enhancementText.text = string.Empty;
            _enhancementRoot?.SetActive(false);
            return;
        }

        int level = WorldEquipmentManager.GetEnhancementLevel(uid, data.Id);
        _enhancementText.text = $"+{level}";
        _enhancementRoot?.SetActive(true);
    }

    private static string BuildGunStatText(ItemData data)
    {
        var stat = GunStatTable.Instance.Get(data.Id);
        if (stat == null) return string.Empty;

        var sb = new StringBuilder();
        sb.AppendLine($"데미지: {stat.Damage:F0}");
        if (stat.PelletCount > 1) sb.AppendLine($"탄자 수: {stat.PelletCount}");
        sb.AppendLine($"연사: {stat.Rpm:F0} RPM");
        sb.AppendLine($"탄창: {stat.MaxMagazine}");
        sb.AppendLine($"장전: {stat.ReloadTime:F1}초");
        sb.AppendLine($"사거리: {stat.BulletRange:F0}");
        return sb.ToString().TrimEnd();
    }

    // 강화 수치는 WorldEquipmentManager가 실제 적용하는 값을 그대로 조회한다 (공식 중복 방지)
    private static string BuildGunPartStatText(ItemData data, int uid)
    {
        var basePart = GunPartTable.Instance.Get(data.Id);
        if (basePart == null) return string.Empty;

        var part = WorldEquipmentManager.GetEnhancedGunPart(basePart, uid);
        var sb = new StringBuilder();
        AppendMultiplier(sb, "산탄 확산", part.SpreadMultiplier);
        AppendMultiplier(sb, "조준 속도", part.AimFovMultiplier);
        AppendMultiplier(sb, "재장전 속도", part.ReloadTimeMultiplier);
        AppendMultiplier(sb, "반동", part.RecoilMultiplier);
        AppendMultiplier(sb, "소음 범위", part.SoundRangeMultiplier);
        if (part.MaxMagazineBonus != 0) sb.AppendLine($"탄창 용량: +{part.MaxMagazineBonus}");
        return sb.ToString().TrimEnd();
    }

    private static string BuildArmorStatText(ItemData data, int uid)
    {
        var baseStat = ArmorStatTable.Instance.Get(data.Id);
        if (baseStat == null) return string.Empty;

        var stat = WorldEquipmentManager.GetEnhancedArmorStat(uid, baseStat);
        var sb = new StringBuilder();
        if (stat.DefenseRate > 0f) sb.AppendLine($"방어율: {stat.DefenseRate * 100f:F0}%");
        if (stat.MaxHpBonus > 0f) sb.AppendLine($"HP 보너스: +{stat.MaxHpBonus:F0}");
        AppendMultiplier(sb, "이동속도", stat.MoveSpeedMultiplier);
        return sb.ToString().TrimEnd();
    }

    // 효과가 없는(배율 1) 스탯은 줄을 만들지 않는다
    private static void AppendMultiplier(StringBuilder sb, string label, float value)
    {
        if (Mathf.Approximately(value, 1f)) return;

        int pct = Mathf.RoundToInt((value - 1f) * 100f);
        sb.AppendLine($"{label}: {(pct >= 0 ? "+" : "")}{pct}%");
    }
}
