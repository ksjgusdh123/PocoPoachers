using System.Collections.Generic;
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

    // 타입별 스탯 표기 영역 — 표기할 스탯이 있는 아이템(무기/파츠/방어구)에서만 활성화된다.
    // 스탯 한 개마다 _statRowPrefab을 _statContainer 아래에 생성한다.
    // 정렬·높이는 Content의 VerticalLayoutGroup + ContentSizeFitter가 자동 처리한다.
    [SerializeField] private GameObject _statRoot;
    [SerializeField] private Transform _statContainer;   // ScrollView의 Content (VerticalLayoutGroup + ContentSizeFitter)
    [SerializeField] private StatRowUI _statRowPrefab;

    // 강화 단계 표기 영역 — 강화 가능 아이템(무기/파츠/방어구)에서만 활성화된다
    [SerializeField] private GameObject _enhancementRoot;
    [SerializeField] private TextMeshProUGUI _enhancementText;

    // 호버 중 실시간 갱신을 위해 구독해둔 대상 (다른 슬롯으로 옮기거나 닫을 때 해제)
    private EquippableItemBase _durabilityTarget;

    // 현재 표시 중인 스탯 행들 — 다음 호버/숨김 때 파괴한다
    private readonly List<StatRowUI> _statRows = new();

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
        ClearStatRows();
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

    // 스탯 한 개마다 행 프리팹을 생성해 채운다. 정렬·높이는 Content의 레이아웃 그룹이 처리한다.
    // 표기할 스탯이 없으면(음식/재료 등) 영역을 숨긴다.
    private void BindStats(ItemData data, int uid)
    {
        ClearStatRows();

        var stats = BuildStats(data, uid);
        if (_statRowPrefab != null && _statContainer != null)
        {
            foreach (var (label, value) in stats)
            {
                var row = Instantiate(_statRowPrefab, _statContainer);
                row.Set(label, value);
                _statRows.Add(row);
            }
        }

        _statRoot?.SetActive(stats.Count > 0);
    }

    private void ClearStatRows()
    {
        foreach (var row in _statRows)
            if (row != null) Destroy(row.gameObject);
        _statRows.Clear();
    }

    private static List<(string label, string value)> BuildStats(ItemData data, int uid) => data.ItemType switch
    {
        ItemType.Weapon => BuildGunStats(data),
        ItemType.GunPart => BuildGunPartStats(data, uid),
        ItemType.Helmet or ItemType.Armor => BuildArmorStats(data, uid),
        _ => new List<(string, string)>(),
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

    private static List<(string, string)> BuildGunStats(ItemData data)
    {
        var list = new List<(string, string)>();
        var stat = GunStatTable.Instance.Get(data.Id);
        if (stat == null) return list;

        list.Add(("데미지", $"{stat.Damage:F0}"));
        if (stat.PelletCount > 1) list.Add(("탄자 수", $"{stat.PelletCount}"));
        list.Add(("연사", $"{stat.Rpm:F0} RPM"));
        list.Add(("탄창", $"{stat.MaxMagazine}"));
        list.Add(("장전", $"{stat.ReloadTime:F1}초"));
        list.Add(("사거리", $"{stat.BulletRange:F0}"));
        return list;
    }

    // 강화 수치는 WorldEquipmentManager가 실제 적용하는 값을 그대로 조회한다 (공식 중복 방지)
    private static List<(string, string)> BuildGunPartStats(ItemData data, int uid)
    {
        var list = new List<(string, string)>();
        var basePart = GunPartTable.Instance.Get(data.Id);
        if (basePart == null) return list;

        var part = WorldEquipmentManager.GetEnhancedGunPart(basePart, uid);
        AddMultiplier(list, "산탄 확산", part.SpreadMultiplier);
        AddMultiplier(list, "조준 속도", part.AimFovMultiplier);
        AddMultiplier(list, "재장전 속도", part.ReloadTimeMultiplier);
        AddMultiplier(list, "반동", part.RecoilMultiplier);
        AddMultiplier(list, "소음 범위", part.SoundRangeMultiplier);
        if (part.MaxMagazineBonus != 0) list.Add(("탄창 용량", $"+{part.MaxMagazineBonus}"));
        return list;
    }

    private static List<(string, string)> BuildArmorStats(ItemData data, int uid)
    {
        var list = new List<(string, string)>();
        var baseStat = ArmorStatTable.Instance.Get(data.Id);
        if (baseStat == null) return list;

        var stat = WorldEquipmentManager.GetEnhancedArmorStat(uid, baseStat);
        if (stat.DefenseRate > 0f) list.Add(("방어율", $"{stat.DefenseRate * 100f:F0}%"));
        if (stat.MaxHpBonus > 0f) list.Add(("HP 보너스", $"+{stat.MaxHpBonus:F0}"));
        AddMultiplier(list, "이동속도", stat.MoveSpeedMultiplier);
        return list;
    }

    // 효과가 없는(배율 1) 스탯은 행을 만들지 않는다
    private static void AddMultiplier(List<(string, string)> list, string label, float value)
    {
        if (Mathf.Approximately(value, 1f)) return;

        int pct = Mathf.RoundToInt((value - 1f) * 100f);
        list.Add((label, $"{(pct >= 0 ? "+" : "")}{pct}%"));
    }
}
