using UnityEngine;
using UnityEngine.EventSystems;

public class EquipDropHandler : ItemHolderDropHandler
{
    [SerializeField] private GameObject _itemVisual;
    [SerializeField] private int _slotIndex;
    private EquipableController _controller;

    public int SlotIndex => _slotIndex;
    public void SetController(EquipableController controller) => _controller = controller;

    // 무기 슬롯이면 장착된 총, 아니면 null (파츠 패널 진입용)
    public GunBase GetEquippedGun() => (_controller as WeaponController)?.GetGun(_slotIndex);

    private void OnEnable()
    {
        if (_controller == null) return;

        _controller.OnSlotUnequipped += OnSlotUnequipped;

        // UI가 닫혀 있는 동안(사망 등) 해제됐을 수 있으니, 열릴 때 실제 장착 상태로 동기화
        if (_isSetted && _controller.GetEquippedId(_slotIndex) == 0)
            OnSlotUnequipped(_slotIndex);
    }

    private void OnDisable()
    {
        if (_controller != null)
            _controller.OnSlotUnequipped -= OnSlotUnequipped;
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
