using System;
using UnityEngine;

// 플레이어 가방 장착 관리 (ArmorController와 동일한 구조)
// 장착 시 비주얼(BagMount) + 인벤토리 용량 확장, 용량 수치는 ItemData.effect_value 사용
public class BagController : EquipableController
{
    public static event Action<int, ItemData> OnBagChanged;

    private BagMount _mount;
    private Inventory _inventory;

    private int _appliedCapacity;
    private int _equippedSlotIndex = -1;

    private void Awake()
    {
        _mount = GetComponent<BagMount>();
        _inventory = GetComponent<Inventory>();
    }

    // slotIndex는 가방 UI의 EquipDropHandler._slotIndex (무기 0~1, 방어구 2~3에 이어서 4 사용)
    public override void Equip(ItemData data, int slotIndex, int uid)
    {
        if (_mount == null) return;
        if (!_mount.ApplyEquip(data.id, uid)) return;

        // 기존 가방의 용량 보너스 제거 후 새 가방 적용
        if (_appliedCapacity > 0)
        {
            _inventory.ReduceCapacity(_appliedCapacity);
            _inventory.ReduceMaxWeight(_appliedCapacity);
        }

        _appliedCapacity = data.EffectValue;
        _inventory.ExpandCapacity(_appliedCapacity);
        _inventory.ExpandMaxWeight(_appliedCapacity);

        _equippedSlotIndex = slotIndex;
        OnBagChanged?.Invoke(slotIndex, data);
        RoomSync.Equip(data.id, slotIndex, uid);
    }

    // 벗으면 줄어들 용량에 현재 아이템 + 반환될 가방(1칸)이 다 들어갈 때만 해제 허용
    public override bool CanUnequip(int slotIndex)
    {
        if (_appliedCapacity <= 0) return true;
        return _inventory.CanReduceCapacity(_appliedCapacity, 1);
    }

    public override void Unequip(int slotIndex)
    {
        if (_mount == null || _mount.GetEquippedItemId() == 0) return;

        _mount.ApplyUnequip();

        if (_appliedCapacity > 0)
        {
            _inventory.ReduceCapacity(_appliedCapacity);
            _inventory.ReduceMaxWeight(_appliedCapacity);
        }
        _appliedCapacity = 0;
        _equippedSlotIndex = -1;

        OnBagChanged?.Invoke(slotIndex, null);
        RaiseUnequipped(slotIndex);
        RoomSync.Equip(0, slotIndex, 0);
    }

    public override void UnequipAll()
    {
        if (_mount != null && _mount.GetEquippedItemId() != 0)
            Unequip(_equippedSlotIndex);
    }

    public override int GetEquippedId(int slotIndex) => _mount != null ? _mount.GetEquippedItemId() : 0;
    public override int GetEquippedUid(int slotIndex) => _mount != null ? _mount.GetEquippedUid() : 0;
}
