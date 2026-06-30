using UnityEngine;

// 총기 파츠를 드래그&드롭으로 장착하는 슬롯 핸들러. 슬롯 하나(=SlotType)당 1개.
// 인스펙터에서 _itemType = GunPart, _slotType 지정. 대상 총은 패널이 SetGun으로 주입.
public class GunPartDropHandler : ItemHolderDropHandler
{
    [SerializeField] private SlotType _slotType;

    private GunBase _gun;

    public SlotType SlotType => _slotType;
    public void SetGun(GunBase gun) => _gun = gun;

    protected override bool OnItemDropped(ItemData data, int amount, int uid)
    {
        // 표시(SetDisplay) 부작용 전에 먼저 검증 — 슬롯/호환이 안 맞으면 거부
        GunPartData part = GunPartTable.Instance.Get(data.id);
        if (part == null || part.slot_type != _slotType)
            return false;
        if (_gun == null || !GunPartUtil.IsCompatible(part, _gun.Stat.GunType))
            return false;

        // ItemType(GunPart) 검사 + 아이콘 표시는 base가 처리
        if (!base.OnItemDropped(data, amount, uid))
            return false;

        _gun.EquipPart(part);
        return true;
    }

    public override void Unequip()
    {
        base.Unequip();              // 인벤토리로 반납
        _gun?.UnequipPart(_slotType); // 총에서 제거 + 스탯 재계산
    }
}
