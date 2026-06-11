using UnityEngine;
using UnityEngine.EventSystems;

public class EquipDropHandler : ItemHolderDropHandler
{
    [SerializeField] private GameObject _itemVisual;
    [SerializeField] private int _slotIndex;
    [SerializeField] private EquipableController _controller;


    protected override bool OnItemDropped(ItemData data, int amount)
    {
        if (!base.OnItemDropped(data, amount)) return false;

        if (_controller == null)
        {
            Debug.LogWarning($"[EquipDropHandler] {gameObject.name}에 Controller가 할당되지 않았습니다.");
            return false;
        }

        _controller.Equip(data, _slotIndex);

        if (_itemVisual != null)
            _itemVisual.SetActive(true);
        return true;
    }

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
