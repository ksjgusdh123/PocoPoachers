using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class EquipDropHandler : ItemHolderDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private GameObject _itemVisual;
    [SerializeField] private int _slotIndex;
    [SerializeField] private TextMeshProUGUI _enhancementText; // 장비/파츠 강화 단계 표시 (없으면 표시 안 함)
    private EquipableController _controller;
    private DescriptionUI _descriptionUI;

    public int SlotIndex => _slotIndex;
    public void SetController(EquipableController controller) => _controller = controller;

    // 저장/복원용 — 현재 이 슬롯에 장착된 아이템 정보
    public int EquippedItemId => _controller != null ? _controller.GetEquippedId(_slotIndex) : 0;
    public int EquippedUid => _controller != null ? _controller.GetEquippedUid(_slotIndex) : 0;

    // 무기 슬롯이면 장착된 총, 아니면 null (파츠 패널 진입용)
    public GunBase GetEquippedGun() => (_controller as WeaponController)?.GetGun(_slotIndex);

    protected override void Awake()
    {
        base.Awake();
        _descriptionUI = FindAnyObjectByType<DescriptionUI>(FindObjectsInactive.Include);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!_isSetted) return;
        _descriptionUI?.ShowDescription(DroppedItemData, GetUnequipUid());
    }

    public void OnPointerExit(PointerEventData eventData) => _descriptionUI?.HideDescription();

    private void OnEnable()
    {
        if (_controller == null) return;

        _controller.OnSlotUnequipped += OnSlotUnequipped;

        // 열릴 때 실제 장착 상태로 동기화 (닫혀 있는 동안 해제됐거나, 씬 전환으로 재장착됐을 수 있음)
        SyncDisplayToEquipped();
    }

    // 드래그드롭이 아닌 경로(씬 전환 복원 등)로 장비가 바뀌었을 때 슬롯 표시를 실제 장착 상태에 맞춘다.
    public void SyncDisplayToEquipped()
    {
        if (_controller == null) return;

        int itemId = _controller.GetEquippedId(_slotIndex);
        var data = itemId != 0 ? ItemTable.Instance.Get(itemId) : null;

        // 이 슬롯 타입과 맞는 아이템만 표시한다. 방어구 컨트롤러는 헬멧/갑옷을 단일로 취급해
        // 두 슬롯 모두 같은 id를 반환하므로, 타입이 다른 슬롯엔 표시하지 않아 중복 노출을 막는다.
        if (data == null || data.ItemType != _itemType)
        {
            if (_isSetted) OnSlotUnequipped(_slotIndex);
            return;
        }

        if (_isSetted && DroppedItemData != null && DroppedItemData.id == itemId)
        {
            UpdateEnhancementText(); // 같은 아이템이어도 강화 레벨이 바뀌었을 수 있어 갱신
            return;
        }

        SetDisplay(data, 1);
        UpdateEnhancementText();
        if (_itemVisual != null)
            _itemVisual.SetActive(true);
    }

    // 장비/파츠면 현재 강화 단계를 "+N"으로 표시, 그 외 타입이거나 미강화(0)면 숨김
    private void UpdateEnhancementText()
    {
        if (_enhancementText == null) return;

        if (!_isSetted || DroppedItemData == null || !IsEnhanceable(DroppedItemData.ItemType))
        {
            _enhancementText.text = string.Empty;
            return;
        }

        int level = WorldEquipmentManager.GetEnhancementLevel(GetUnequipUid(), DroppedItemData.id);
        _enhancementText.text = $"+{level}";
    }

    private static bool IsEnhanceable(ItemType type) =>
        type == ItemType.Armor ||
        type == ItemType.Helmet ||
        type == ItemType.Weapon ||
        type == ItemType.GunPart;

    protected override void ClearDisplay()
    {
        base.ClearDisplay();
        if (_enhancementText != null)
            _enhancementText.text = string.Empty;
    }

    private void OnDisable()
    {
        if (_controller != null)
            _controller.OnSlotUnequipped -= OnSlotUnequipped;
        _descriptionUI?.HideDescription();
    }

    // 외부(사망 등)에서 장비가 해제되면 UI 표시를 정리한다 (인벤토리 반납 없이)
    private void OnSlotUnequipped(int slotIndex)
    {
        if (slotIndex != _slotIndex) return;

        ClearDisplay();
        if (_itemVisual != null)
            _itemVisual.SetActive(false);
    }


    protected override bool OnItemDropped(ItemData data, int amount, int uid)
    {
        if (!base.OnItemDropped(data, amount, uid)) return false;

        if (_controller == null)
        {
            Debug.LogWarning($"[EquipDropHandler] {gameObject.name}에 Controller가 할당되지 않았습니다.");
            return false;
        }

        _controller.Equip(data, _slotIndex, uid);
        UpdateEnhancementText();

        if (_itemVisual != null)
            _itemVisual.SetActive(true);
        return true;
    }

    protected override int GetUnequipUid() => _controller?.GetEquippedUid(_slotIndex) ?? 0;

    public override void Unequip()
    {
        // 컨트롤러가 해제를 거부하면(예: 가방 해제 시 인벤토리 공간 부족) 중단
        if (_controller != null && !_controller.CanUnequip(_slotIndex)) return;

        base.Unequip();
        _controller?.Unequip(_slotIndex);
        if (_itemVisual != null)
            _itemVisual.SetActive(false);
    }
}
