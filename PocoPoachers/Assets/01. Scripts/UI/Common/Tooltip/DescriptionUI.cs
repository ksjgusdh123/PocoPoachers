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
    [SerializeField] private Vector3 _offset;

    // 내구도가 있는 장비(무기/방어구)를 호버했을 때만 활성화되는 영역
    [SerializeField] private GameObject _durabilityRoot;
    [SerializeField] private Slider _durabilitySlider;
    [SerializeField] private TextMeshProUGUI _durabilityText;

    // 호버 중 실시간 갱신을 위해 구독해둔 대상 (다른 슬롯으로 옮기거나 닫을 때 해제)
    private EquippableItemBase _durabilityTarget;

    public void ShowDescription(ItemSlotUI slot)
    {
        if (!slot.IsSettedItem) return;
        // 아직 공개되지 않은(리빌 진행 중) 박스 슬롯은 설명 미표시
        if (slot.InventoryUI != null && slot.InventoryUI.IsSlotUnrevealed(slot.SlotIndex)) return;
        Show(slot.SlotItemData, slot.SlotUid, slot.transform.position);
    }

    // 장비 슬롯(EquipDropHandler) 등 ItemSlotUI가 아닌 곳에서 호버할 때 사용
    public void ShowDescription(ItemData data, int uid, Vector3 anchorPosition)
    {
        if (data == null) return;
        Show(data, uid, anchorPosition);
    }

    private void Show(ItemData data, int uid, Vector3 anchorPosition)
    {
        if (!gameObject.activeSelf) gameObject.SetActive(true);
        transform.position = anchorPosition + _offset;
        _name.text = LocalizationManager.GetInstance().GetString(data.ItemName);
        _description.text = LocalizationManager.GetInstance().GetString(data.Description);
        if (_icon != null) _icon.sprite = ResourceManager.Instance.LoadSprite(data.icon);
        BindDurability(data, uid);
    }

    public void HideDescription()
    {
        _name.text = "";
        _description.text = "";
        if (_icon != null) _icon.sprite = null;
        UnbindDurability();
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
}
